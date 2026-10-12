using Azure;
using Azure.Data.Tables;

namespace CLDV6212POE_ST10488555_ST10476800.Models
{
    public class OrderEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string CustomerName { get; set; }

        public string SelectedItemSKUs { get; set; }

        public double TotalPrice { get; set; }
        public DateTime OrderTimestamp { get; set; }
        public string Status { get; set; }
        public DateTime StatusUpdatedAt { get; set; }

        public static OrderEntity FromMessage(OrderMessage message)
        {
            DateTime orderTimestamp = message.OrderTimestamp.Value.ToUniversalTime();

            OrderEntity entity = new OrderEntity();
            entity.PartitionKey = orderTimestamp.ToString("yyyy-MM-dd");
            entity.RowKey = message.OrderId;
            entity.CustomerName = message.CustomerName;
            entity.SelectedItemSKUs = string.Join(",", message.SelectedItemSKUs);
            entity.TotalPrice = message.TotalPrice;
            entity.OrderTimestamp = orderTimestamp;
            entity.Status = OrderStatus.Received;
            entity.StatusUpdatedAt = DateTime.UtcNow;
            return entity;
        }
    }
}