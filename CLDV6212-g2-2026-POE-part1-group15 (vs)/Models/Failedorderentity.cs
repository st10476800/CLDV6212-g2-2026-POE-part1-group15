using Azure;
using Azure.Data.Tables;

namespace CLDV6212POE_ST10488555_ST10476800.Models
{
    public class FailedOrderEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string OrderId { get; set; }
        public string RawMessage { get; set; }
        public string Reason { get; set; }
        public DateTime FailedAt { get; set; }
    }
}