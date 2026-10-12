using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using CLDV6212POE_ST10488555_ST10476800.Models;
using CLDV6212POE_ST10488555_ST10476800.Services;

namespace CLDV6212POE_ST10488555_ST10476800.Functions
{
    public class ProcessOrderQueue
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private OrderTableService _orderService;
        private ILogger _logger;
        private int _stepDelaySeconds;

        public ProcessOrderQueue(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ProcessOrderQueue>();
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            _orderService = new OrderTableService(connectionString);
            _stepDelaySeconds = 10;
            string delaySetting = Environment.GetEnvironmentVariable("ORDER_STEP_DELAY_SECONDS");
            int parsedDelay;
            if (int.TryParse(delaySetting, out parsedDelay) && parsedDelay >= 0)
            {
                _stepDelaySeconds = parsedDelay;
            }
        }

        [Function("ProcessOrderQueue")]
        public async Task Run(
            [QueueTrigger(QueueStorageService.OrderQueueName, Connection = "AzureWebJobsStorage")] string queueMessage,
            FunctionContext context)
        {
            string messageId = GetBindingValue(context, "Id");
            string dequeueCount = GetBindingValue(context, "DequeueCount");

            _logger.LogInformation("Queue message {MessageId} received from {QueueName} (attempt {DequeueCount}).",
                messageId, QueueStorageService.OrderQueueName, dequeueCount);

            try
            {
                // The host has already decoded the Base64. This only decodes again if a message was encoded twice.
                string json = QueueStorageService.DecodeMessageIfEncoded(queueMessage);

                OrderMessage order = JsonSerializer.Deserialize<OrderMessage>(json, JsonOptions);
                if (order == null)
                {
                    throw new InvalidOperationException("The queue message was empty.");
                }

                List<string> errors = order.Validate();
                if (errors.Count > 0)
                {
                    throw new InvalidOperationException("The order failed validation: " + string.Join(" ", errors));
                }

                await _orderService.EnsureTablesExistAsync();

                OrderEntity entity = _orderService.GetOrder(order.OrderId);
                if (entity == null)
                {
                    entity = OrderEntity.FromMessage(order);
                    await _orderService.AddOrderAsync(entity);
                    _logger.LogInformation("Order {OrderId} saved to the Orders table (PartitionKey {OrderDate}) with status {Status}.",
                        entity.RowKey, entity.PartitionKey, entity.Status);
                }
                else
                {
                    _logger.LogWarning("Order {OrderId} is already in the Orders table with status {Status}. Carrying on from there.",
                        entity.RowKey, entity.Status);
                }

                if (entity.Status == OrderStatus.Received)
                {
                    await WaitForBarista();
                    await _orderService.UpdateOrderStatusAsync(entity, OrderStatus.Preparing);
                    _logger.LogInformation("Order {OrderId}: {OldStatus} -> {NewStatus}.", entity.RowKey, OrderStatus.Received, OrderStatus.Preparing);
                }

                if (entity.Status == OrderStatus.Preparing)
                {
                    await WaitForBarista();
                    await _orderService.UpdateOrderStatusAsync(entity, OrderStatus.Ready);
                    _logger.LogInformation("Order {OrderId}: {OldStatus} -> {NewStatus}. Waiting for the student to collect it.",
                        entity.RowKey, OrderStatus.Preparing, OrderStatus.Ready);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not process queue message {MessageId} (attempt {DequeueCount}). It will be retried, and moved to {PoisonQueue} if it keeps failing.",
                    messageId, dequeueCount, QueueStorageService.OrderPoisonQueueName);

                throw;
            }
        }

        [Function("ProcessPoisonOrder")]
        public async Task RunPoison(
            [QueueTrigger(QueueStorageService.OrderPoisonQueueName, Connection = "AzureWebJobsStorage")] string poisonMessage,
            FunctionContext context)
        {
            string messageId = GetBindingValue(context, "Id");
            string json = QueueStorageService.DecodeMessageIfEncoded(poisonMessage);
            string orderId = TryReadOrderId(json);

            _logger.LogError("POISON MESSAGE {MessageId} for order {OrderId} failed every retry on {QueueName}. Full message: {Payload}",
                messageId, orderId, QueueStorageService.OrderQueueName, json);

            FailedOrderEntity failedOrder = new FailedOrderEntity();
            failedOrder.PartitionKey = DateTime.UtcNow.ToString("yyyy-MM-dd");
            failedOrder.RowKey = messageId;
            if (messageId == "unknown")
            {
                failedOrder.RowKey = Guid.NewGuid().ToString();
            }
            failedOrder.OrderId = orderId;
            failedOrder.RawMessage = Shorten(json, 30000);
            failedOrder.Reason = "Failed every retry on " + QueueStorageService.OrderQueueName + " and was moved to the poison queue. Check the function logs for the exact error.";
            failedOrder.FailedAt = DateTime.UtcNow;

            try
            {
                await _orderService.EnsureTablesExistAsync();
                await _orderService.AddFailedOrderAsync(failedOrder);
                _logger.LogInformation("Poison message {MessageId} saved to the FailedOrders table.", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not save poison message {MessageId} to the FailedOrders table.", messageId);
            }
        }

        private async Task WaitForBarista()
        {
            if (_stepDelaySeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(_stepDelaySeconds));
            }
        }

        private static string GetBindingValue(FunctionContext context, string key)
        {
            object value;
            if (context.BindingContext.BindingData.TryGetValue(key, out value) && value != null)
            {
                return value.ToString().Trim('"');
            }
            return "unknown";
        }

        private static string TryReadOrderId(string json)
        {
            try
            {
                OrderMessage order = JsonSerializer.Deserialize<OrderMessage>(json, JsonOptions);
                if (order != null && !string.IsNullOrWhiteSpace(order.OrderId))
                {
                    return order.OrderId;
                }
            }
            catch (JsonException)
            {
            }
            return "unknown";
        }

        private static string Shorten(string text, int maxLength)
        {
            if (text == null)
            {
                return "";
            }
            if (text.Length <= maxLength)
            {
                return text;
            }
            return text.Substring(0, maxLength);
        }
    }
}