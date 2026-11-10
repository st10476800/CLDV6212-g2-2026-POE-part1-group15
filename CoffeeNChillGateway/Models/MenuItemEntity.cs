using Azure;
using Azure.Data.Tables;

namespace CoffeeNChillGateway.Models
{
    /// <summary>
    /// One menu item in the MenuItems table. PartitionKey is the category and RowKey is the item id.
    /// </summary>
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
