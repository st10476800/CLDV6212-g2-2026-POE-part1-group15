using System.Text;
using System.Text.Json;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using CoffeeNChillGateway.Models;

namespace CoffeeNChillGateway.Services
{
    // Everything to do with the order-processing-queue lives here.
    public class QueueStorageService
    {
        public const string OrderQueueName = "order-processing-queue";
        public const string OrderPoisonQueueName = "order-processing-queue-poison";

        private QueueClient _queueClient;

        public QueueStorageService(string connectionString)
        {
            // No network call here on purpose. If Azurite is down, the error happens inside
            // SendOrderAsync, where the function can catch it and return a proper error response.
            _queueClient = new QueueClient(connectionString, OrderQueueName);
        }

        // Serializes the order to JSON, Base64 encodes it, and pushes it onto the queue.
        // The Functions host expects Base64 queue messages by default, and decodes them
        // again before ProcessOrderQueue receives them.
        public async Task<QueueSendResult> SendOrderAsync(OrderMessage order)
        {
            await _queueClient.CreateIfNotExistsAsync();

            string json = JsonSerializer.Serialize(order);
            string base64 = EncodeToBase64(json);

            Response<SendReceipt> response = await _queueClient.SendMessageAsync(base64);

            QueueSendResult result = new QueueSendResult();
            result.MessageId = response.Value.MessageId;
            result.JsonMessage = json;
            result.Base64Message = base64;
            return result;
        }

        public static string EncodeToBase64(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(bytes);
        }

        // The host normally hands the function plain JSON already. If a message was encoded
        // twice (for example added by hand in Storage Explorer), this decodes it one more time.
        public static string DecodeMessageIfEncoded(string message)
        {
            if (message == null)
            {
                return null;
            }

            string trimmed = message.Trim();
            if (trimmed.StartsWith("{"))
            {
                return trimmed;
            }

            try
            {
                byte[] bytes = Convert.FromBase64String(trimmed);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                // Not Base64 either. Return it as it is and let the JSON parser report the problem.
                return trimmed;
            }
        }
    }
}
