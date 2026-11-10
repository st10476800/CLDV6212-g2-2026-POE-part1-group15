using System.ComponentModel.DataAnnotations;

namespace CoffeeNChillGateway.Models
{
    /// <summary>
    /// The details a user sends to create an account.
    /// </summary>
    public class RegisterDto
    {
        /// <summary>The user's full name, for example "Thandi Nkosi".</summary>
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        /// <summary>The user's email. This is also their username.</summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        /// <summary>The password. It must be 8 to 72 characters long (BCrypt only uses the first 72).</summary>
        [Required]
        [StringLength(72, MinimumLength = 8)]
        public string Password { get; set; }

        /// <summary>"Customer", "Staff" or "Admin". Leave it out to register as a Customer.</summary>
        public string Role { get; set; }

        /// <summary>The secret staff code. It is only needed when the role is Staff or Admin.</summary>
        public string StaffCode { get; set; }
    }
}
