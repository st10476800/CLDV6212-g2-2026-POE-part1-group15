namespace CoffeeNChillGateway.Models
{
    // What the API sends back to the client when it asks about an order.
    // Keeps the table details (PartitionKey, RowKey, ETag) out of the response.
    public class OrderDto
    {
        public string OrderId { get; set; }
        public string OrderDate { get; set; }
        public string CustomerName { get; set; }
        public List<string> SelectedItemSKUs { get; set; }
        public double TotalPrice { get; set; }
        public DateTime OrderTimestamp { get; set; }
        public string Status { get; set; }
        public DateTime StatusUpdatedAt { get; set; }

        public static OrderDto FromEntity(OrderEntity entity)
        {
            OrderDto dto = new OrderDto();
            dto.OrderId = entity.RowKey;
            dto.OrderDate = entity.PartitionKey;
            dto.CustomerName = entity.CustomerName;
            dto.SelectedItemSKUs = SplitSkus(entity.SelectedItemSKUs);
            dto.TotalPrice = entity.TotalPrice;
            dto.OrderTimestamp = entity.OrderTimestamp;
            dto.Status = entity.Status;
            dto.StatusUpdatedAt = entity.StatusUpdatedAt;
            return dto;
        }

        // Turns "COF-001,PAS-104" back into a proper list.
        private static List<string> SplitSkus(string skus)
        {
            List<string> result = new List<string>();
            if (string.IsNullOrWhiteSpace(skus))
            {
                return result;
            }

            string[] parts = skus.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string sku = parts[i].Trim();
                if (sku.Length > 0)
                {
                    result.Add(sku);
                }
            }
            return result;
        }
    }
}
