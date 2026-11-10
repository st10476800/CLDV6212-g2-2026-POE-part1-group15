using Azure;
using Azure.Data.Tables;
using CoffeeNChillGateway.Models;

namespace CoffeeNChillGateway.Services
{
    /// <summary>
    /// Saves and finds user profiles in the Users table.
    /// </summary>
    public class UserTableService
    {
        private const string TableName = "Users";

        private TableClient _tableClient;
        private bool _tableChecked = false;

        public UserTableService(string connectionString)
        {
            TableServiceClient serviceClient = new TableServiceClient(connectionString);
            _tableClient = serviceClient.GetTableClient(TableName);
        }

        /// <summary>
        /// Creates the Users table the first time we need it. We do this here and not in the
        /// constructor, so the gateway can still start if the storage is not ready yet.
        /// </summary>
        private async Task EnsureTableExistsAsync()
        {
            if (_tableChecked == false)
            {
                await _tableClient.CreateIfNotExistsAsync();
                _tableChecked = true;
            }
        }

        /// <summary>
        /// Saves a new user. Throws a RequestFailedException with status 409
        /// if the same Role and Email are already in the table.
        /// </summary>
        public async Task AddUserAsync(UserEntity user)
        {
            await EnsureTableExistsAsync();
            await _tableClient.AddEntityAsync(user);
        }

        /// <summary>
        /// Finds a user by email. The PartitionKey is the role, so we search all
        /// partitions for the RowKey. Returns null if there is no user with that email.
        /// </summary>
        public async Task<UserEntity> GetUserByEmailAsync(string email)
        {
            await EnsureTableExistsAsync();

            // Emails are saved in lower case. A single quote must be doubled in a table filter.
            string cleanEmail = email.Trim().ToLower().Replace("'", "''");
            string filter = "RowKey eq '" + cleanEmail + "'";

            AsyncPageable<UserEntity> results = _tableClient.QueryAsync<UserEntity>(filter);

            await foreach (UserEntity user in results)
            {
                return user;
            }

            return null;
        }
    }
}
