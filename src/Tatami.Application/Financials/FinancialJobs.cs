using Tatami.Domain.Entities;
using Tatami.Domain.Enums;
using Tatami.Domain.Repositories;

namespace Tatami.Application.Financials;

public class FinancialJobs
{
    private readonly IFinancialRepository _financials;
    private readonly IFinancialNotificationSender _notifications;
    private readonly TimeProvider _timeProvider;

    public FinancialJobs(IFinancialRepository financials, IFinancialNotificationSender notifications, TimeProvider timeProvider)
    {
        _financials = financials;
        _notifications = notifications;
        _timeProvider = timeProvider;
    }

    public async Task<FinancialJobResult> GenerateMonthlyChargesAsync(CancellationToken cancellationToken = default)
    {
        var today = FinancialMonth.Today(_timeProvider);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var referenceMonth = FinancialMonth.First(today.Year, today.Month);
        var candidates = await _financials.ListChargeCandidatesAsync(cancellationToken);
        var processed = 0;
        foreach (var candidate in candidates)
        {
            if (candidate.PaymentDueDay is < 1 or > 31 || candidate.PaymentDueDay > today.Day || candidate.Amount <= 0)
                continue;
            var charge = new Financial
            {
                Id = Guid.NewGuid(), AcademyId = candidate.AcademyId, StudentId = candidate.StudentId,
                Amount = candidate.Amount, ReferenceMonth = referenceMonth,
                DueDate = FinancialMonth.DueDateForMonth(today.Year, today.Month, candidate.PaymentDueDay),
                CreatedAt = now, UpdatedAt = now,
            };
            if (!await _financials.TryCreateAsync(charge, cancellationToken)) continue;
            processed++;
            if (charge.DueDate == today)
                await _notifications.SendDueTodayAsync(charge, cancellationToken);
        }
        return new FinancialJobResult(processed);
    }

    public async Task<FinancialJobResult> UpdateOverdueAsync(CancellationToken cancellationToken = default)
    {
        var today = FinancialMonth.Today(_timeProvider);
        var pending = await _financials.ListPendingBeforeAsync(today, cancellationToken);
        var processed = 0;
        foreach (var charge in pending)
        {
            if (!await _financials.TrySetStatusAsync(charge.AcademyId, charge.Id, FinancialStatus.Pending,
                    FinancialStatus.Overdue, null, null, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken)) continue;
            charge.Status = FinancialStatus.Overdue;
            processed++;
            await _notifications.SendOverdueAsync(charge, cancellationToken);
        }
        return new FinancialJobResult(processed);
    }
}