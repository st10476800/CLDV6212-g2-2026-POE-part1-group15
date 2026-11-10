using Azure;
using CoffeeNChillGateway.Models;
using CoffeeNChillGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChillGateway.Controllers
{
    /// <summary>
    /// The CoffeeNChill menu. Anyone can view it. Only Admin and Staff can change it.
    /// </summary>
    [ApiController]
    [Route("api/menu")]
    [Produces("application/json")]
    public class MenuController : ControllerBase
    {
        private MenuTableService _menuService;
        private ILogger<MenuController> _logger;

        public MenuController(MenuTableService menuService, ILogger<MenuController> logger)
        {
            _menuService = menuService;
            _logger = logger;
        }

        /// <summary>
        /// Gets every item on the menu. No login needed.
        /// </summary>
        /// <response code="200">The list of menu items.</response>
        /// <response code="503">The storage is not reachable.</response>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<MenuItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GetAllMenuItems()
        {
            try
            {
                List<MenuItemEntity> items = await _menuService.GetAllItemsAsync();
                return Ok(ConvertToDtoList(items));
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Table Storage error while reading the menu.");
                return StorageUnavailable();
            }
        }

        /// <summary>
        /// Gets the menu items in one category, for example "Hot Drinks". No login needed.
        /// </summary>
        /// <param name="category">The category name.</param>
        /// <response code="200">The items in that category. The list is empty if there are none.</response>
        /// <response code="503">The storage is not reachable.</response>
        [HttpGet("category/{category}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<MenuItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GetMenuItemsByCategory(string category)
        {
            try
            {
                List<MenuItemEntity> items = await _menuService.GetItemsByCategoryAsync(category);
                return Ok(ConvertToDtoList(items));
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Table Storage error while reading category {Category}.", category);
                return StorageUnavailable();
            }
        }

        /// <summary>
        /// Adds a new menu item. Admin and Staff only.
        /// </summary>
        /// <param name="dto">The new item. Category and Name are required.</param>
        /// <response code="201">The item was created.</response>
        /// <response code="400">The details are not valid.</response>
        /// <response code="409">An item with this Category and Id already exists.</response>
        /// <response code="503">The storage is not reachable.</response>
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(typeof(MenuItemDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> CreateMenuItem([FromBody] MenuItemDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Category) || string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { Message = "Category and Name are required." });
            }

            if (dto.Price < 0)
            {
                return BadRequest(new { Message = "Price cannot be negative." });
            }

            MenuItemEntity item = dto.ToEntity();

            try
            {
                await _menuService.AddItemAsync(item);
            }
            catch (RequestFailedException ex)
            {
                if (ex.Status == 409)
                {
                    return Conflict(new { Message = "A menu item with Category '" + item.PartitionKey + "' and Id '" + item.RowKey + "' already exists." });
                }
                if (ex.Status == 400)
                {
                    return BadRequest(new { Message = "The Category or Id has a character that is not allowed (/, \\, # or ?)." });
                }

                _logger.LogError(ex, "Table Storage error while creating a menu item.");
                return StorageUnavailable();
            }

            return StatusCode(StatusCodes.Status201Created, MenuItemDto.FromEntity(item));
        }

        /// <summary>
        /// Changes an existing menu item. Admin and Staff only.
        /// </summary>
        /// <param name="category">The category of the item.</param>
        /// <param name="id">The id of the item.</param>
        /// <param name="dto">The new name, description, price and availability.</param>
        /// <response code="200">The item was updated.</response>
        /// <response code="404">There is no item with this category and id.</response>
        /// <response code="503">The storage is not reachable.</response>
        [HttpPut("{category}/{id}")]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(typeof(MenuItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> UpdateMenuItem(string category, string id, [FromBody] MenuItemUpdateDto dto)
        {
            try
            {
                MenuItemEntity item = await _menuService.GetItemAsync(category, id);
                if (item == null)
                {
                    return NotFound(new { Message = "Menu item not found." });
                }

                item.Name = dto.Name.Trim();
                item.Description = dto.Description;
                item.Price = dto.Price;
                item.IsAvailable = dto.IsAvailable;

                await _menuService.UpdateItemAsync(item);

                return Ok(MenuItemDto.FromEntity(item));
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Table Storage error while updating {Category}/{Id}.", category, id);
                return StorageUnavailable();
            }
        }

        /// <summary>
        /// Deletes a menu item. Admin and Staff only.
        /// </summary>
        /// <param name="category">The category of the item.</param>
        /// <param name="id">The id of the item.</param>
        /// <response code="204">The item was deleted.</response>
        /// <response code="404">There is no item with this category and id.</response>
        /// <response code="503">The storage is not reachable.</response>
        [HttpDelete("{category}/{id}")]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> DeleteMenuItem(string category, string id)
        {
            try
            {
                MenuItemEntity item = await _menuService.GetItemAsync(category, id);
                if (item == null)
                {
                    return NotFound(new { Message = "Menu item not found." });
                }

                await _menuService.DeleteItemAsync(category, id);
                return NoContent();
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Table Storage error while deleting {Category}/{Id}.", category, id);
                return StorageUnavailable();
            }
        }

        /// <summary>
        /// Turns a list of table entities into a list of DTOs.
        /// </summary>
        private List<MenuItemDto> ConvertToDtoList(List<MenuItemEntity> items)
        {
            List<MenuItemDto> dtoList = new List<MenuItemDto>();
            for (int i = 0; i < items.Count; i++)
            {
                dtoList.Add(MenuItemDto.FromEntity(items[i]));
            }
            return dtoList;
        }

        private IActionResult StorageUnavailable()
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { Message = "Could not reach the storage. Please try again." });
        }
    }
}
