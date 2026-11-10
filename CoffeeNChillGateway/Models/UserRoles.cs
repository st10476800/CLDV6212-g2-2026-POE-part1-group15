namespace CoffeeNChillGateway.Models
{
    /// <summary>
    /// The three roles a user can have. The role is also the PartitionKey in the Users table.
    /// </summary>
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Staff = "Staff";
        public const string Customer = "Customer";

        /// <summary>
        /// Checks a role the client sent and returns it with the correct spelling.
        /// Returns null if it is empty or not a real role.
        /// </summary>
        public static string Normalise(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return null;
            }

            string cleanRole = role.Trim();

            if (string.Equals(cleanRole, Admin, StringComparison.OrdinalIgnoreCase))
            {
                return Admin;
            }
            else if (string.Equals(cleanRole, Staff, StringComparison.OrdinalIgnoreCase))
            {
                return Staff;
            }
            else if (string.Equals(cleanRole, Customer, StringComparison.OrdinalIgnoreCase))
            {
                return Customer;
            }
            else
            {
                return null;
            }
        }
    }
}
