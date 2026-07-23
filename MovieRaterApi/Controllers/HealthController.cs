using Microsoft.AspNetCore.Mvc;

namespace MovieRaterApi.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        _logger.LogInformation("Health check requested");
        return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
    }
}