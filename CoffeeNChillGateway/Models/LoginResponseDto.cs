namespace CoffeeNChillGateway.Models
{
    /// <summary>
    /// What the gateway sends back after a successful login.
    /// </summary>
    public class LoginResponseDto
    {
        /// <summary>The JWT. Send it in the header as: Authorization: Bearer (token).</summary>
        public string Token { get; set; }

        /// <summary>The user's email.</summary>
        public string Email { get; set; }

        /// <summary>The user's full name.</summary>
        public string FullName { get; set; }

        /// <summary>The user's role.</summary>
        public string Role { get; set; }

        /// <summary>The time the token stops working (UTC).</summary>
        public DateTime ExpiresAt { get; set; }
    }
}
