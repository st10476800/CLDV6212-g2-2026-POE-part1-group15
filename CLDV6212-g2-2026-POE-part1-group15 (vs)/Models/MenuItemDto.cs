using System;
using System.Collections.Generic;
using System.Text;

namespace CLDV6212POE_ST10488555_ST10476800.Models
{
    public class MenuItemDto
    {
        public string Category { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public bool IsAvailable { get; set; }

        public static MenuItemDto FromEntity(MenuItemEntity entity)
        {
            return new MenuItemDto
            {
                Category = entity.PartitionKey,
                Id = entity.RowKey,
                Name = entity.Name,
                Description = entity.Description,
                Price = entity.Price,
                IsAvailable = entity.IsAvailable
            };
        }

        public MenuItemEntity ToEntity()
        {
            return new MenuItemEntity
            {
                PartitionKey = Category,
                RowKey = Id ?? Guid.NewGuid().ToString(),
                Name = Name,
                Description = Description,
                Price = Price,
                IsAvailable = IsAvailable
            };
        }
    }
}