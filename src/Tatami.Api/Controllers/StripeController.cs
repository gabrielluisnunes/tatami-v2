using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatami.Api.Extensions;
using Tatami.Application.Billing;

namespace Tatami.Api.Controllers;

[ApiController]
[Route("api/stripe")]
[Authorize]
public class StripeController : ControllerBase
{
    private readonly IStripeBillingService _billingService;

    public StripeController(IStripeBillingService billingService)
    {
        _billingService = billingService;
    }

    [HttpPost("checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession(
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();
            var response = await _billingService.CreateCheckoutSessionAsync(
                userId,
                request,
                cancellationToken);

            return Ok(response);
        }
        catch (BillingException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("portal-session")]
    public async Task<IActionResult> CreatePortalSession(CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();
            var response = await _billingService.CreatePortalSessionAsync(userId, cancellationToken);
            return Ok(response);
        }
        catch (BillingException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
