using Azure;
using Azure.Data.Tables;

namespace CoffeeNChillGateway.Models
{
    /// <summary>
    /// A user profile stored in the Users table.
    /// PartitionKey is the role and RowKey is the email.
    /// </summary>
    public class UserEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
