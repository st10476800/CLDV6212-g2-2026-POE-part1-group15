using System.Security.Claims;
using System.Text;
using CoffeeNChillGateway.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CoffeeNChillGateway.Services
{
    /// <summary>
    /// Creates the signed JWT Bearer tokens that users get when they log in.
    /// </summary>
    public class TokenService
    {
        private string _key;
        private string _issuer;
        private string _audience;
        private int _expiryMinutes;

        public TokenService(IConfiguration configuration)
        {
            _key = configuration["Jwt:Key"];
            _issuer = configuration["Jwt:Issuer"];
            _audience = configuration["Jwt:Audience"];
            _expiryMinutes = int.Parse(configuration["Jwt:ExpiryMinutes"]);
        }

        /// <summary>
        /// Makes a JWT for a user. The token has three claims: Name, Email and Role.
        /// </summary>
        public LoginResponseDto CreateLoginResponse(UserEntity user)
        {
            DateTime expiresAt = DateTime.UtcNow.AddMinutes(_expiryMinutes);

            List<Claim> claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Name, user.FullName));
            claims.Add(new Claim(ClaimTypes.Email, user.RowKey));
            claims.Add(new Claim(ClaimTypes.Role, user.PartitionKey));

            SymmetricSecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
            SigningCredentials credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor();
            descriptor.Subject = new ClaimsIdentity(claims);
            descriptor.Issuer = _issuer;
            descriptor.Audience = _audience;
            descriptor.Expires = expiresAt;
            descriptor.SigningCredentials = credentials;

            JsonWebTokenHandler handler = new JsonWebTokenHandler();
            string token = handler.CreateToken(descriptor);

            LoginResponseDto response = new LoginResponseDto();
            response.Token = token;
            response.Email = user.RowKey;
            response.FullName = user.FullName;
            response.Role = user.PartitionKey;
            response.ExpiresAt = expiresAt;
            return response;
        }
    }
}