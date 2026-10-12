namespace CLDV6212POE_ST10488555_ST10476800.Models
{
   
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