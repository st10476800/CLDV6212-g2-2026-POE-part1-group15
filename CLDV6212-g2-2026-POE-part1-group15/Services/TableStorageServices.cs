using Azure;
using Azure.Data.Tables;
using CLDV6212POE_ST10488555_ST10476800.Models;

namespace CLDV6212POE_ST10488555_ST10476800.Services
{
    public class TableStorageService
    {
        private TableClient _tableClient;

        public TableStorageService(string connectionString)
        {
            _tableClient = new TableClient(connectionString, "MenuItems");
            _tableClient.CreateIfNotExists();
        }

        public void AddItem(MenuItemEntity item)
        {
            _tableClient.AddEntity(item);
        }

        public List<MenuItemEntity> GetAllItems()
        {
            List<MenuItemEntity> results = new List<MenuItemEntity>();
            var items = _tableClient.Query<MenuItemEntity>();
            foreach (var item in items)
            {
                results.Add(item);
            }
            return results;
        }

        public List<MenuItemEntity> GetItemsByCategory(string category)
        {
            List<MenuItemEntity> results = new List<MenuItemEntity>();
            var items = _tableClient.Query<MenuItemEntity>(x => x.PartitionKey == category);
            foreach (var item in items)
            {
                results.Add(item);
            }
            return results;
        }

        public MenuItemEntity GetItem(string category, string id)
        {
            try
            {
                var response = _tableClient.GetEntity<MenuItemEntity>(category, id);
                return response.Value;
            }
            catch (RequestFailedException)
            {
                return null;
            }
        }

        public void UpdateItem(MenuItemEntity item)
        {
            _tableClient.UpdateEntity(item, item.ETag, TableUpdateMode.Replace);
        }

        public void DeleteItem(string category, string id)
        {
            _tableClient.DeleteEntity(category, id);
        }
    }
}