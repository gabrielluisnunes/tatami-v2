using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatami.Api.Extensions;
using Tatami.Application.Academies;

namespace Tatami.Api.Controllers;

[ApiController]
[Route("api/onboarding")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly IOnboardingService _onboardingService;

    public OnboardingController(IOnboardingService onboardingService)
    {
        _onboardingService = onboardingService;
    }

    [HttpPost]
    public async Task<IActionResult> Complete(
        CreateOnboardingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();
            var response = await _onboardingService.CompleteOnboardingAsync(
                userId,
                request,
                cancellationToken);

            return Ok(response);
        }
        catch (AcademyException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
