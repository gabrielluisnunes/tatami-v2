using Microsoft.AspNetCore.Mvc;

namespace Tatami.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            service = "tatami-api",
            version = "2.0.0"
        });
    }
}
