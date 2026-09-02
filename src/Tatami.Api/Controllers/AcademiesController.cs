using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatami.Api.Extensions;
using Tatami.Application.Academies;

namespace Tatami.Api.Controllers;

[ApiController]
[Route("api/academies")]
[Authorize]
public class AcademiesController : ControllerBase
{
    private readonly IAcademyService _academyService;

    public AcademiesController(IAcademyService academyService)
    {
        _academyService = academyService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyAcademy(CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();
            var academy = await _academyService.GetMyAcademyAsync(userId, cancellationToken);
            return Ok(academy);
        }
        catch (AcademyException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyAcademy(
        UpdateAcademyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();
            var academy = await _academyService.UpdateMyAcademyAsync(
                userId,
                request,
                cancellationToken);

            return Ok(academy);
        }
        catch (AcademyException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
