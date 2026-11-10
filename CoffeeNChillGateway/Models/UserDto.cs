namespace CoffeeNChillGateway.Models
{
    /// <summary>
    /// A user as it is shown to the client. It never includes the password hash.
    /// </summary>
    public class UserDto
    {
        /// <summary>The user's full name.</summary>
        public string FullName { get; set; }

        /// <summary>The user's email.</summary>
        public string Email { get; set; }

        /// <summary>The user's role.</summary>
        public string Role { get; set; }

        /// <summary>The date the account was created (UTC).</summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Turns a UserEntity from the table into a UserDto.
        /// </summary>
        public static UserDto FromEntity(UserEntity entity)
        {
            UserDto dto = new UserDto();
            dto.FullName = entity.FullName;
            dto.Email = entity.RowKey;
            dto.Role = entity.PartitionKey;
            dto.CreatedDate = entity.CreatedDate;
            return dto;
        }
    }
}
