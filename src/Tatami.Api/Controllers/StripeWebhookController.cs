using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatami.Application.Billing;

namespace Tatami.Api.Controllers;

[ApiController]
[Route("api/webhooks/stripe")]
[AllowAnonymous]
public class StripeWebhookController : ControllerBase
{
    private readonly IStripeBillingService _billingService;

    public StripeWebhookController(IStripeBillingService billingService)
    {
        _billingService = billingService;
    }

    [HttpPost]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        if (string.IsNullOrWhiteSpace(signature))
        {
            return BadRequest(new { error = "Missing Stripe-Signature header." });
        }

        try
        {
            await _billingService.HandleWebhookAsync(payload, signature, cancellationToken);
            return Ok(new { received = true });
        }
        catch (BillingException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
