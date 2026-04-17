
namespace Entity.PlayerService.Api;

using Microsoft.AspNetCore.Mvc;

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
