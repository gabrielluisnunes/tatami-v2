using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatami.Api.Extensions;
using Tatami.Api.Filters;
using Tatami.Application.Financials;
using Tatami.Domain.Enums;

namespace Tatami.Api.Controllers;

[ApiController]
[Route("api/financials")]
[Authorize(Roles = UserRole.Admin)]
[FinancialExceptionFilter]
public class FinancialsController : ControllerBase
{
    private readonly FinancialService _financials;
    private readonly TimeProvider _timeProvider;

    public FinancialsController(FinancialService financials, TimeProvider timeProvider)
    {
        _financials = financials;
        _timeProvider = timeProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetMonth([FromQuery] string? month, CancellationToken cancellationToken)
    {
        var reference = FinancialMonth.Parse(month, _timeProvider);
        return Ok(await _financials.GetMonthOverviewAsync(User.GetUserId(), reference.Year, reference.Month, cancellationToken));
    }

    [HttpPost("mark-paid")]
    public async Task<IActionResult> MarkPaid(MarkPaidRequest request, CancellationToken cancellationToken)
    {
        await _financials.MarkPaidAsync(User.GetUserId(), request.FinancialId, cancellationToken);
        return NoContent();
    }

    [HttpPost("manual-payment")]
    public async Task<IActionResult> ManualPayment(ManualPaymentRequest request, CancellationToken cancellationToken)
    {
        await _financials.ManualPaymentAsync(User.GetUserId(), request.StudentId, request.Amount, request.PaidAt, cancellationToken);
        return NoContent();
    }

    [HttpGet("export-overdue")]
    public async Task<IActionResult> ExportOverdue([FromQuery] string? month, CancellationToken cancellationToken)
    {
        var reference = FinancialMonth.Parse(month, _timeProvider);
        var file = await _financials.ExportOverdueAsync(User.GetUserId(), reference.Year, reference.Month, cancellationToken);
        return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"inadimplentes-{FinancialMonth.Key(reference)}.xlsx");
    }
}