using System.Security.Claims;
using Azure;
using CoffeeNChillGateway.Models;
using CoffeeNChillGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChillGateway.Controllers
{
    /// <summary>
    /// Register a new account and log in to get a JWT token.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private UserTableService _userService;
        private PasswordService _passwordService;
        private TokenService _tokenService;
        private IConfiguration _configuration;
        private ILogger<AuthController> _logger;

        public AuthController(
            UserTableService userService,
            PasswordService passwordService,
            TokenService tokenService,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _userService = userService;
            _passwordService = passwordService;
            _tokenService = tokenService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new user account.
        /// </summary>
        /// <remarks>
        /// The password is hashed with BCrypt before it is saved in the Users table.
        /// If the role is left out, the user becomes a Customer. Staff and Admin accounts
        /// need the secret staff code.
        /// </remarks>
        /// <param name="dto">The new user's details.</param>
        /// <response code="201">The account was created.</response>
        /// <response code="400">The details are not valid.</response>
        /// <response code="403">The staff code is missing or wrong.</response>
        /// <response code="409">A user with this email already exists.</response>
        /// <response code="503">The storage is not reachable.</response>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // Work out the role. No role means Customer.
            string role = UserRoles.Customer;
            if (string.IsNullOrWhiteSpace(dto.Role) == false)
            {
                role = UserRoles.Normalise(dto.Role);
                if (role == null)
                {
                    return BadRequest(new { Message = "Role must be Customer, Staff or Admin." });
                }
            }

            // Staff and Admin accounts need the secret code
            if (role != UserRoles.Customer)
            {
                string realCode = _configuration["Auth:StaffRegistrationCode"];
                if (string.IsNullOrEmpty(dto.StaffCode) || dto.StaffCode != realCode)
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { Message = "A valid staff code is needed to register as Staff or Admin." });
                }
            }

            string email = dto.Email.Trim().ToLower();

            try
            {
                UserEntity existingUser = await _userService.GetUserByEmailAsync(email);
                if (existingUser != null)
                {
                    return Conflict(new { Message = "A user with this email already exists." });
                }

                UserEntity newUser = new UserEntity();
                newUser.PartitionKey = role;
                newUser.RowKey = email;
                newUser.PasswordHash = _passwordService.HashPassword(dto.Password);
                newUser.FullName = dto.FullName.Trim();
                newUser.CreatedDate = DateTime.UtcNow;

                await _userService.AddUserAsync(newUser);

                _logger.LogInformation("New {Role} registered: {Email}", role, email);

                UserDto userDto = UserDto.FromEntity(newUser);
                return StatusCode(StatusCodes.Status201Created, userDto);
            }
            catch (RequestFailedException ex)
            {
                if (ex.Status == 409)
                {
                    return Conflict(new { Message = "A user with this email already exists." });
                }

                _logger.LogError(ex, "Table Storage error while registering {Email}.", email);
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { Message = "Could not reach the storage. Please try again." });
            }
        }

        /// <summary>
        /// Logs a user in and returns a JWT Bearer token.
        /// </summary>
        /// <remarks>
        /// The token has the user's Name, Email and Role as claims. Send it in the header
        /// of later requests as: Authorization: Bearer (token)
        /// </remarks>
        /// <param name="dto">The email and password.</param>
        /// <response code="200">Login worked. The token is in the response.</response>
        /// <response code="400">The email or password is missing.</response>
        /// <response code="401">The email or password is wrong.</response>
        /// <response code="503">The storage is not reachable.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            UserEntity user = null;

            try
            {
                user = await _userService.GetUserByEmailAsync(dto.Email);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Table Storage error while logging in {Email}.", dto.Email);
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { Message = "Could not reach the storage. Please try again." });
            }

            // We give the same message for a wrong email and a wrong password
            if (user == null)
            {
                return Unauthorized(new { Message = "Invalid email or password." });
            }

            bool passwordIsCorrect = _passwordService.VerifyPassword(dto.Password, user.PasswordHash);
            if (passwordIsCorrect == false)
            {
                return Unauthorized(new { Message = "Invalid email or password." });
            }

            LoginResponseDto response = _tokenService.CreateLoginResponse(user);
            return Ok(response);
        }

        /// <summary>
        /// Shows who you are, using the claims inside your token. Good for testing a token.
        /// </summary>
        /// <response code="200">The name, email and role from the token.</response>
        /// <response code="401">There is no token, or it is not valid.</response>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetCurrentUser()
        {
            var result = new
            {
                Name = GetClaimValue(ClaimTypes.Name),
                Email = GetClaimValue(ClaimTypes.Email),
                Role = GetClaimValue(ClaimTypes.Role)
            };

            return Ok(result);
        }

        /// <summary>
        /// Reads one claim from the current user's token. Returns an empty string if it is not there.
        /// </summary>
        private string GetClaimValue(string claimType)
        {
            Claim claim = User.FindFirst(claimType);
            if (claim == null)
            {
                return "";
            }
            return claim.Value;
        }
    }
}