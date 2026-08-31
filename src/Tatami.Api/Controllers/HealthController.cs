using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tatami.Infrastructure.Persistence;

namespace Tatami.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly TatamiDbContext _dbContext;

    public HealthController(TatamiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var databaseHealthy = await _dbContext.Database.CanConnectAsync(cancellationToken);

        var response = new
        {
            status = databaseHealthy ? "ok" : "degraded",
            service = "tatami-api",
            version = "2.0.0",
            checks = new
            {
                database = databaseHealthy ? "ok" : "unavailable"
            }
        };

        return databaseHealthy ? Ok(response) : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}
