using Microsoft.AspNetCore.Mvc;

namespace Entity.RatingService.Api;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new { status = "ok" });
    }
}
