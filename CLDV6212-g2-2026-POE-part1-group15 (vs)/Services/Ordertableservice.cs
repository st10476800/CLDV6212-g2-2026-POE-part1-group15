using Azure;
using Azure.Data.Tables;
using CLDV6212POE_ST10488555_ST10476800.Models;

namespace CLDV6212POE_ST10488555_ST10476800.Services
{
    public class OrderTableService
    {
        public const string OrdersTableName = "Orders";
        public const string FailedOrdersTableName = "FailedOrders";

        private TableClient _ordersTable;
        private TableClient _failedOrdersTable;

        public OrderTableService(string connectionString)
        {
            TableServiceClient serviceClient = new TableServiceClient(connectionString);
            _ordersTable = serviceClient.GetTableClient(OrdersTableName);
            _failedOrdersTable = serviceClient.GetTableClient(FailedOrdersTableName);
        }

        public async Task EnsureTablesExistAsync()
        {
            await _ordersTable.CreateIfNotExistsAsync();
            await _failedOrdersTable.CreateIfNotExistsAsync();
        }

        public OrderEntity GetOrder(string orderId)
        {
            string filter = "RowKey eq '" + EscapeForFilter(orderId) + "'";
            Pageable<OrderEntity> results = _ordersTable.Query<OrderEntity>(filter);

            foreach (OrderEntity entity in results)
            {
                return entity;
            }
            return null;
        }

        public List<OrderEntity> GetAllOrders()
        {
            List<OrderEntity> orders = new List<OrderEntity>();
            Pageable<OrderEntity> results = _ordersTable.Query<OrderEntity>();

            foreach (OrderEntity entity in results)
            {
                orders.Add(entity);
            }
            return orders;
        }

        public async Task AddOrderAsync(OrderEntity order)
        {
            Response response = await _ordersTable.AddEntityAsync(order);
            order.ETag = response.Headers.ETag.GetValueOrDefault();
        }

        public async Task UpdateOrderStatusAsync(OrderEntity order, string newStatus)
        {
            order.Status = newStatus;
            order.StatusUpdatedAt = DateTime.UtcNow;

            ETag ifMatch = order.ETag;
            if (ifMatch == default(ETag))
            {
                ifMatch = ETag.All;
            }

            Response response = await _ordersTable.UpdateEntityAsync(order, ifMatch, TableUpdateMode.Replace);
            order.ETag = response.Headers.ETag.GetValueOrDefault();
        }

        public async Task AddFailedOrderAsync(FailedOrderEntity failedOrder)
        {
            await _failedOrdersTable.UpsertEntityAsync(failedOrder, TableUpdateMode.Replace);
        }

        public List<FailedOrderEntity> GetFailedOrders()
        {
            List<FailedOrderEntity> failedOrders = new List<FailedOrderEntity>();
            Pageable<FailedOrderEntity> results = _failedOrdersTable.Query<FailedOrderEntity>();

            foreach (FailedOrderEntity entity in results)
            {
                failedOrders.Add(entity);
            }
            return failedOrders;
        }

        private static string EscapeForFilter(string value)
        {
            return value.Replace("'", "''");
        }
    }
}