using Azure;
using Azure.Data.Tables;
using CoffeeNChillGateway.Models;

namespace CoffeeNChillGateway.Services
{
    /// <summary>
    /// Reads and writes menu items in the MenuItems table.
    /// </summary>
    public class MenuTableService
    {
        private const string TableName = "MenuItems";

        private TableClient _tableClient;
        private bool _tableChecked = false;

        public MenuTableService(string connectionString)
        {
            TableServiceClient serviceClient = new TableServiceClient(connectionString);
            _tableClient = serviceClient.GetTableClient(TableName);
        }

        private async Task EnsureTableExistsAsync()
        {
            if (_tableChecked == false)
            {
                await _tableClient.CreateIfNotExistsAsync();
                _tableChecked = true;
            }
        }

        /// <summary>
        /// Saves a new item. Throws a RequestFailedException with status 409 if the
        /// same Category and Id already exist.
        /// </summary>
        public async Task AddItemAsync(MenuItemEntity item)
        {
            await EnsureTableExistsAsync();
            await _tableClient.AddEntityAsync(item);
        }

        public async Task<List<MenuItemEntity>> GetAllItemsAsync()
        {
            await EnsureTableExistsAsync();

            List<MenuItemEntity> items = new List<MenuItemEntity>();
            AsyncPageable<MenuItemEntity> results = _tableClient.QueryAsync<MenuItemEntity>();

            await foreach (MenuItemEntity item in results)
            {
                items.Add(item);
            }
            return items;
        }

        public async Task<List<MenuItemEntity>> GetItemsByCategoryAsync(string category)
        {
            await EnsureTableExistsAsync();

            string safeCategory = category.Replace("'", "''");
            string filter = "PartitionKey eq '" + safeCategory + "'";

            List<MenuItemEntity> items = new List<MenuItemEntity>();
            AsyncPageable<MenuItemEntity> results = _tableClient.QueryAsync<MenuItemEntity>(filter);

            await foreach (MenuItemEntity item in results)
            {
                items.Add(item);
            }
            return items;
        }

        /// <summary>
        /// Finds one item by category and id. Returns null if it does not exist.
        /// </summary>
        public async Task<MenuItemEntity> GetItemAsync(string category, string id)
        {
            await EnsureTableExistsAsync();

            NullableResponse<MenuItemEntity> response = await _tableClient.GetEntityIfExistsAsync<MenuItemEntity>(category, id);
            if (response.HasValue)
            {
                return response.Value;
            }
            return null;
        }

        public async Task UpdateItemAsync(MenuItemEntity item)
        {
            await EnsureTableExistsAsync();
            await _tableClient.UpdateEntityAsync(item, ETag.All, TableUpdateMode.Replace);
        }

        public async Task DeleteItemAsync(string category, string id)
        {
            await EnsureTableExistsAsync();
            await _tableClient.DeleteEntityAsync(category, id);
        }
    }
}
