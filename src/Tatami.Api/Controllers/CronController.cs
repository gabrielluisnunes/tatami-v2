using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatami.Api.Filters;
using Tatami.Application.Financials;

namespace Tatami.Api.Controllers;

[ApiController]
[Route("api/cron")]
[AllowAnonymous]
[CronAuthorize]
public class CronController : ControllerBase
{
    private readonly FinancialJobs _jobs;

    public CronController(FinancialJobs jobs)
    {
        _jobs = jobs;
    }

    [HttpPost("generate-monthly-charges")]
    public async Task<IActionResult> GenerateMonthlyCharges(CancellationToken cancellationToken) =>
        Ok(await _jobs.GenerateMonthlyChargesAsync(cancellationToken));

    [HttpPost("update-overdue")]
    public async Task<IActionResult> UpdateOverdue(CancellationToken cancellationToken) =>
        Ok(await _jobs.UpdateOverdueAsync(cancellationToken));
}