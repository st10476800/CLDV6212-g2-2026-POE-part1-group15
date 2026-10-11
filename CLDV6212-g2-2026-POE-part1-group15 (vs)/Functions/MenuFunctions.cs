using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using CLDV6212POE_ST10488555_ST10476800.Models;
using CLDV6212POE_ST10488555_ST10476800.Services;

namespace CLDV6212POE_ST10488555_ST10476800.Functions
{
    public class MenuFunctions
    {
        private TableStorageService _service;
        private ILogger _logger;

        public MenuFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MenuFunctions>();
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            _service = new TableStorageService(connectionString);
        }

        [Function("CreateMenuItem")]
        public async Task<HttpResponseData> CreateMenuItem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu")] HttpRequestData req)
        {
            string body = await new StreamReader(req.Body).ReadToEndAsync();
            MenuItemEntity item = JsonSerializer.Deserialize<MenuItemEntity>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (item == null || item.PartitionKey == null || item.RowKey == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("PartitionKey and RowKey are required.");
                return badResponse;
            }

            _service.AddItem(item);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(item);
            return response;
        }

        [Function("GetAllMenuItems")]
        public async Task<HttpResponseData> GetAllMenuItems(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu")] HttpRequestData req)
        {
            var items = _service.GetAllItems();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(items);
            return response;
        }

        [Function("GetMenuItemsByCategory")]
        public async Task<HttpResponseData> GetMenuItemsByCategory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu/category/{category}")] HttpRequestData req,
            string category)
        {
            var items = _service.GetItemsByCategory(category);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(items);
            return response;
        }

        [Function("UpdateMenuItem")]
        public async Task<HttpResponseData> UpdateMenuItem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "menu/{category}/{id}")] HttpRequestData req,
            string category, string id)
        {
            MenuItemEntity existingItem = _service.GetItem(category, id);

            if (existingItem == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("Menu item not found.");
                return notFoundResponse;
            }

            string body = await new StreamReader(req.Body).ReadToEndAsync();
            MenuItemEntity updatedItem = JsonSerializer.Deserialize<MenuItemEntity>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (updatedItem == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid request body.");
                return badResponse;
            }

            existingItem.Name = updatedItem.Name;
            existingItem.Description = updatedItem.Description;
            existingItem.Price = updatedItem.Price;
            existingItem.IsAvailable = updatedItem.IsAvailable;

            _service.UpdateItem(existingItem);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(existingItem);
            return response;
        }

        [Function("DeleteMenuItem")]
        public async Task<HttpResponseData> DeleteMenuItem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "menu/{category}/{id}")] HttpRequestData req,
            string category, string id)
        {
            MenuItemEntity existingItem = _service.GetItem(category, id);

            if (existingItem == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("Menu item not found.");
                return notFoundResponse;
            }

            _service.DeleteItem(category, id);

            var response = req.CreateResponse(HttpStatusCode.NoContent);
            return response;
        }
    }
}