namespace CoffeeNChillGateway.Models
{
    /// <summary>
    /// A menu item as the client sends it and receives it.
    /// </summary>
    public class MenuItemDto
    {
        /// <summary>The category, for example "Hot Drinks". It is the PartitionKey in the table.</summary>
        public string Category { get; set; }

        /// <summary>The item id, for example "COF-001". Leave it out and one is made for you.</summary>
        public string Id { get; set; }

        /// <summary>The name of the item.</summary>
        public string Name { get; set; }

        /// <summary>A short description.</summary>
        public string Description { get; set; }

        /// <summary>The price in rand.</summary>
        public double Price { get; set; }

        /// <summary>True if the item can be ordered right now.</summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Turns a MenuItemEntity from the table into a MenuItemDto.
        /// </summary>
        public static MenuItemDto FromEntity(MenuItemEntity entity)
        {
            MenuItemDto dto = new MenuItemDto();
            dto.Category = entity.PartitionKey;
            dto.Id = entity.RowKey;
            dto.Name = entity.Name;
            dto.Description = entity.Description;
            dto.Price = entity.Price;
            dto.IsAvailable = entity.IsAvailable;
            return dto;
        }

        /// <summary>
        /// Turns this DTO into a MenuItemEntity that can be saved in the table.
        /// </summary>
        public MenuItemEntity ToEntity()
        {
            MenuItemEntity entity = new MenuItemEntity();
            entity.PartitionKey = Category.Trim();

            if (string.IsNullOrWhiteSpace(Id))
            {
                entity.RowKey = Guid.NewGuid().ToString();
            }
            else
            {
                entity.RowKey = Id.Trim();
            }

            entity.Name = Name.Trim();
            entity.Description = Description;
            entity.Price = Price;
            entity.IsAvailable = IsAvailable;
            return entity;
        }
    }
}
