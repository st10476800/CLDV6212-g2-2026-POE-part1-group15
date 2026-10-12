namespace CLDV6212POE_ST10488555_ST10476800.Models
{
    public class FailedOrderDto
    {
        public string MessageId { get; set; }
        public string OrderId { get; set; }
        public string RawMessage { get; set; }
        public string Reason { get; set; }
        public DateTime FailedAt { get; set; }

        public static FailedOrderDto FromEntity(FailedOrderEntity entity)
        {
            FailedOrderDto dto = new FailedOrderDto();
            dto.MessageId = entity.RowKey;
            dto.OrderId = entity.OrderId;
            dto.RawMessage = entity.RawMessage;
            dto.Reason = entity.Reason;
            dto.FailedAt = entity.FailedAt;
            return dto;
        }
    }
}