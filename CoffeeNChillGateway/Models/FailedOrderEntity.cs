using Azure;
using Azure.Data.Tables;

namespace CoffeeNChillGateway.Models
{
    // One row in the FailedOrders table.
    // Every message that ends up on the poison queue is recorded here, so nothing is lost silently.
    public class FailedOrderEntity : ITableEntity
    {
        public string PartitionKey { get; set; }   // date the message failed (yyyy-MM-dd)
        public string RowKey { get; set; }         // the queue message id
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string OrderId { get; set; }
        public string RawMessage { get; set; }
        public string Reason { get; set; }
        public DateTime FailedAt { get; set; }
    }
}
