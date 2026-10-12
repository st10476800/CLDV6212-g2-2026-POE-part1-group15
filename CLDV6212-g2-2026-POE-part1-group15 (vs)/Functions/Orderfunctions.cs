using System.Net;
using System.Text.Json;
using Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using CLDV6212POE_ST10488555_ST10476800.Models;
using CLDV6212POE_ST10488555_ST10476800.Services;

namespace CLDV6212POE_ST10488555_ST10476800.Functions
{
    public class OrderFunctions
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private QueueStorageService _queueService;
        private OrderTableService _orderService;
        private ILogger _logger;

        public OrderFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<OrderFunctions>();
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            _queueService = new QueueStorageService(connectionString);
            _orderService = new OrderTableService(connectionString);
        }

        [Function("QueueOrder")]
        public async Task<HttpResponseData> QueueOrder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders/queue")] HttpRequestData req)
        {
            string body = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return await CreateMessageResponse(req, HttpStatusCode.BadRequest, "Request body is empty. Send the order as JSON.");
            }

            OrderMessage order;
            try
            {
                order = JsonSerializer.Deserialize<OrderMessage>(body, JsonOptions);
            }
            catch (JsonException ex)
            {
                return await CreateMessageResponse(req, HttpStatusCode.BadRequest, "The order is not valid JSON. " + ex.Message);
            }

            if (order == null)
            {
                return await CreateMessageResponse(req, HttpStatusCode.BadRequest, "The order is empty.");
            }

            order.ApplyDefaults();
            List<string> errors = order.Validate();
            if (errors.Count > 0)
            {
                HttpResponseData badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { Message = "Order validation failed.", Errors = errors });
                return badResponse;
            }

            try
            {
                await _orderService.EnsureTablesExistAsync();

                OrderEntity existingOrder = _orderService.GetOrder(order.OrderId);
                if (existingOrder != null)
                {
                    return await CreateMessageResponse(req, HttpStatusCode.Conflict,
                        "An order with OrderId " + order.OrderId + " already exists. Its status is " + existingOrder.Status + ".");
                }

                QueueSendResult result = await _queueService.SendOrderAsync(order);

                _logger.LogInformation("Order {OrderId} for {CustomerName} placed on {QueueName} as message {MessageId}.",
                    order.OrderId, order.CustomerName, QueueStorageService.OrderQueueName, result.MessageId);

                HttpResponseData response = req.CreateResponse(HttpStatusCode.Accepted);
                await response.WriteAsJsonAsync(new
                {
                    Message = "Order accepted and placed on the queue.",
                    OrderId = order.OrderId,
                    QueueName = QueueStorageService.OrderQueueName,
                    MessageId = result.MessageId,
                    Status = "Queued",
                    Order = order,
                    Base64Message = result.Base64Message
                });
                return response;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Storage error while queueing order {OrderId}.", order.OrderId);
                return await CreateMessageResponse(req, HttpStatusCode.ServiceUnavailable,
                    "Could not reach Azure Storage (Azurite), so the order was not queued. " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while queueing order {OrderId}.", order.OrderId);
                return await CreateMessageResponse(req, HttpStatusCode.InternalServerError,
                    "Something went wrong while queueing the order. " + ex.Message);
            }
        }

        [Function("GetAllOrders")]
        public async Task<HttpResponseData> GetAllOrders(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders")] HttpRequestData req)
        {
            try
            {
                await _orderService.EnsureTablesExistAsync();
                List<OrderEntity> orders = _orderService.GetAllOrders();

                List<OrderDto> dtoList = new List<OrderDto>();
                for (int i = 0; i < orders.Count; i++)
                {
                    dtoList.Add(OrderDto.FromEntity(orders[i]));
                }

                HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(dtoList);
                return response;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Storage error while reading orders.");
                return await CreateMessageResponse(req, HttpStatusCode.ServiceUnavailable, "Could not read orders from Azure Storage. " + ex.Message);
            }
        }

        [Function("GetOrderById")]
        public async Task<HttpResponseData> GetOrderById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{orderId}")] HttpRequestData req,
            string orderId)
        {
            try
            {
                await _orderService.EnsureTablesExistAsync();
                OrderEntity order = _orderService.GetOrder(orderId);

                if (order == null)
                {
                    return await CreateMessageResponse(req, HttpStatusCode.NotFound,
                        "Order " + orderId + " was not found. If you only just placed it, the queue may not have processed it yet.");
                }

                HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(OrderDto.FromEntity(order));
                return response;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Storage error while reading order {OrderId}.", orderId);
                return await CreateMessageResponse(req, HttpStatusCode.ServiceUnavailable, "Could not read the order from Azure Storage. " + ex.Message);
            }
        }

        [Function("CollectOrder")]
        public async Task<HttpResponseData> CollectOrder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "orders/{orderId}/collect")] HttpRequestData req,
            string orderId)
        {
            try
            {
                await _orderService.EnsureTablesExistAsync();
                OrderEntity order = _orderService.GetOrder(orderId);

                if (order == null)
                {
                    return await CreateMessageResponse(req, HttpStatusCode.NotFound, "Order " + orderId + " was not found.");
                }

                if (order.Status == OrderStatus.Collected)
                {
                    return await CreateMessageResponse(req, HttpStatusCode.Conflict, "Order " + orderId + " has already been collected.");
                }

                if (order.Status != OrderStatus.Ready)
                {
                    return await CreateMessageResponse(req, HttpStatusCode.Conflict,
                        "Order " + orderId + " is still " + order.Status + ". It can only be collected once it is Ready.");
                }

                string previousStatus = order.Status;
                await _orderService.UpdateOrderStatusAsync(order, OrderStatus.Collected);
                _logger.LogInformation("Order {OrderId} moved from {OldStatus} to {NewStatus}.", orderId, previousStatus, OrderStatus.Collected);

                HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(OrderDto.FromEntity(order));
                return response;
            }
            catch (RequestFailedException ex)
            {
                if (ex.Status == 412)
                {
                    return await CreateMessageResponse(req, HttpStatusCode.Conflict,
                        "Order " + orderId + " changed while it was being updated. Please try again.");
                }

                _logger.LogError(ex, "Azure Storage error while collecting order {OrderId}.", orderId);
                return await CreateMessageResponse(req, HttpStatusCode.ServiceUnavailable, "Could not update the order in Azure Storage. " + ex.Message);
            }
        }

        [Function("GetFailedOrders")]
        public async Task<HttpResponseData> GetFailedOrders(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "failed-orders")] HttpRequestData req)
        {
            try
            {
                await _orderService.EnsureTablesExistAsync();
                List<FailedOrderEntity> failedOrders = _orderService.GetFailedOrders();

                List<FailedOrderDto> dtoList = new List<FailedOrderDto>();
                for (int i = 0; i < failedOrders.Count; i++)
                {
                    dtoList.Add(FailedOrderDto.FromEntity(failedOrders[i]));
                }

                HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(dtoList);
                return response;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Storage error while reading failed orders.");
                return await CreateMessageResponse(req, HttpStatusCode.ServiceUnavailable, "Could not read failed orders from Azure Storage. " + ex.Message);
            }
        }

        private static async Task<HttpResponseData> CreateMessageResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
        {
            HttpResponseData response = req.CreateResponse(statusCode);
            await response.WriteAsJsonAsync(new { Message = message });
            return response;
        }
    }
}