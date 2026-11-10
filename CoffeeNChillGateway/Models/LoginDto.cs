using System.ComponentModel.DataAnnotations;

namespace CoffeeNChillGateway.Models
{
    /// <summary>
    /// The details a user sends to log in.
    /// </summary>
    public class LoginDto
    {
        /// <summary>The email the user registered with.</summary>
        [Required]
        public string Email { get; set; }

        /// <summary>The user's password.</summary>
        [Required]
        public string Password { get; set; }
    }
}
