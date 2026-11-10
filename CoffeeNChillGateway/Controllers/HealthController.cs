using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChillGateway.Controllers
{
    /// <summary>
    /// Used to check that the gateway is running.
    /// </summary>
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// Returns a message if the gateway is running.
        /// </summary>
        /// <response code="200">The gateway is running.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetHealth()
        {
            return Ok("The CoffeeNChill gateway is running.");
        }
    }
}
