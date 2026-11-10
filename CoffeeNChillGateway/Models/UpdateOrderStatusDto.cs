using System.ComponentModel.DataAnnotations;

namespace CoffeeNChillGateway.Models
{
    /// <summary>
    /// Used by staff to move an order to its next status.
    /// </summary>
    public class UpdateOrderStatusDto
    {
        /// <summary>The order to change, for example "ORD-2026-8801".</summary>
        [Required]
        public string OrderId { get; set; }

        /// <summary>The new status: Received, Preparing, Ready or Collected.</summary>
        [Required]
        public string Status { get; set; }
    }
}
