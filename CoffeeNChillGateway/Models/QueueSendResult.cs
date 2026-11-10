namespace CoffeeNChillGateway.Models
{
    // What QueueStorageService hands back after it puts an order on the queue.
    public class QueueSendResult
    {
        public string MessageId { get; set; }
        public string JsonMessage { get; set; }
        public string Base64Message { get; set; }
    }
}
