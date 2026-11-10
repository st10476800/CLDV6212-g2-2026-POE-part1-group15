using Azure;
using Azure.Data.Tables;

namespace CoffeeNChillGateway.Models
{
    // One row in the Orders Azure Table.
    // PartitionKey = the order date (yyyy-MM-dd), RowKey = the OrderId.
    public class OrderEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string CustomerName { get; set; }

        // Azure Tables cannot store a list, so the SKUs are saved as one comma separated string.
        public string SelectedItemSKUs { get; set; }

        public double TotalPrice { get; set; }
        public DateTime OrderTimestamp { get; set; }
        public string Status { get; set; }
        public DateTime StatusUpdatedAt { get; set; }

        // Turns a queue message into a brand new Orders table row with the status "Received".
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
