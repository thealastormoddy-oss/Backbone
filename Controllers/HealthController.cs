using Microsoft.AspNetCore.Mvc;

namespace LabSyncBackbone.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;

        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var userIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            _logger.LogInformation("[HealthController.{Method}] Health check called from {RemoteIp}.", nameof(Get), userIp);
            return Ok(new
            {
                status = "Healthy",
                app = "LabSyncBackbone",
                time = DateTime.UtcNow
            });
        }
    }
}