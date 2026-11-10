using System.ComponentModel.DataAnnotations;

namespace CoffeeNChillGateway.Models
{
    /// <summary>
    /// The new details for a menu item. The category and id come from the address, so they are not here.
    /// </summary>
    public class MenuItemUpdateDto
    {
        /// <summary>The name of the item.</summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>A short description.</summary>
        public string Description { get; set; }

        /// <summary>The new price in rand. It cannot be negative.</summary>
        [Range(0, 10000)]
        public double Price { get; set; }

        /// <summary>True if the item can be ordered right now.</summary>
        public bool IsAvailable { get; set; }
    }
}
