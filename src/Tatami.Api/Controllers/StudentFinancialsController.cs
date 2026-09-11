using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatami.Api.Extensions;
using Tatami.Api.Filters;
using Tatami.Application.Financials;
using Tatami.Domain.Enums;

namespace Tatami.Api.Controllers;

[ApiController]
[Route("api/students/me/financials")]
[Authorize(Roles = UserRole.Aluno)]
[FinancialExceptionFilter]
public class StudentFinancialsController : ControllerBase
{
    private readonly StudentFinancialService _financials;

    public StudentFinancialsController(StudentFinancialService financials)
    {
        _financials = financials;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await _financials.ListMineAsync(User.GetUserId(), cancellationToken));

    [HttpGet("{id:guid}/pix")]
    public async Task<IActionResult> GetPix(Guid id, CancellationToken cancellationToken) =>
        Ok(await _financials.GetPixAsync(User.GetUserId(), id, cancellationToken));

    [HttpPost("{id:guid}/aguardando")]
    public async Task<IActionResult> MarkAwaiting(Guid id, CancellationToken cancellationToken)
    {
        await _financials.MarkAwaitingConfirmationAsync(User.GetUserId(), id, cancellationToken);
        return NoContent();
    }
}