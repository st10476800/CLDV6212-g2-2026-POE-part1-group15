using System.Text;
using System.Text.Json;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using CLDV6212POE_ST10488555_ST10476800.Models;

namespace CLDV6212POE_ST10488555_ST10476800.Services
{
    public class QueueStorageService
    {
        public const string OrderQueueName = "order-processing-queue";
        public const string OrderPoisonQueueName = "order-processing-queue-poison";

        private QueueClient _queueClient;

        public QueueStorageService(string connectionString)
        {
            _queueClient = new QueueClient(connectionString, OrderQueueName);
        }

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
                return trimmed;
            }
        }
    }
}