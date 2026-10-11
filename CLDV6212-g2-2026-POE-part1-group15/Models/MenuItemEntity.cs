using Azure;
using Azure.Data.Tables;

namespace CLDV6212POE_ST10488555_ST10476800.Models
{
    public class MenuItemEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public bool IsAvailable { get; set; }
    }
}