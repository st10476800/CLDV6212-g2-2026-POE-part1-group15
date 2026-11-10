namespace CoffeeNChillGateway.Services
{
    /// <summary>
    /// Hashes and checks passwords with BCrypt.
    /// We never save the real password, only the hash.
    /// </summary>
    public class PasswordService
    {
        /// <summary>
        /// Turns a password into a BCrypt hash. BCrypt adds a random salt, so the same
        /// password gives a different hash each time.
        /// </summary>
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Checks if a password matches a saved hash.
        /// </summary>
        public bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
    }
}
