using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatami.Api.Extensions;
using Tatami.Application.Students;
using Tatami.Domain.Enums;

namespace Tatami.Api.Controllers;

[ApiController]
[Route("api/students/me")]
[Authorize(Roles = UserRole.Aluno)]
public class StudentProfileController : ControllerBase
{
    private readonly IStudentProfileService _profileService;

    public StudentProfileController(IStudentProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();
            var profile = await _profileService.GetMyProfileAsync(userId, cancellationToken);
            return Ok(profile);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Usuário não autenticado." });
        }
        catch (StudentException ex)
        {
            return NotFound(new { error = ex.Message, code = ex.Code });
        }
    }

    [HttpPost("complete-profile")]
    public async Task<IActionResult> CompleteProfile(
        [FromBody] CompleteStudentProfileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();
            var profile = await _profileService.CompleteProfileAsync(userId, request, cancellationToken);
            return Ok(profile);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Usuário não autenticado." });
        }
        catch (StudentException ex)
        {
            return BadRequest(new { error = ex.Message, code = ex.Code });
        }
    }
}
