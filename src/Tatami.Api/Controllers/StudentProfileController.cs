using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatami.Api.Extensions;
using Tatami.Application.Students;
using Tatami.Domain.Enums;

namespace Tatami.Api.Controllers;

[ApiController]
[Route("api/students/me")]
[Authorize]
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
            EnsureAluno();
            var userId = User.GetUserId();
            var profile = await _profileService.GetMyProfileAsync(userId, cancellationToken);
            return Ok(profile);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (StudentException ex)
        {
            return NotFound(new { error = ex.Message, code = ex.Code });
        }
    }

    [HttpPost("complete-profile")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> CompleteProfile(
        [FromBody] CompleteStudentProfileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            EnsureAluno();
            var userId = User.GetUserId();
            var profile = await _profileService.CompleteProfileAsync(userId, request, cancellationToken);
            return Ok(profile);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (StudentException ex)
        {
            return BadRequest(new { error = ex.Message, code = ex.Code });
        }
    }


    private void EnsureAluno()
    {
        if (User.IsInRole(UserRole.Aluno)
            || User.HasClaim("role", UserRole.Aluno)
            || User.HasClaim(ClaimTypes.Role, UserRole.Aluno))
        {
            return;
        }

        throw new UnauthorizedAccessException("Acesso permitido apenas para alunos.");
    }
}
