using Tatami.Domain.Entities;
using Tatami.Domain.Enums;

namespace Tatami.Domain.Repositories;

public record MonthlyChargeCandidate(Guid StudentId, Guid AcademyId, decimal Amount, int PaymentDueDay);

public interface IFinancialRepository
{
    Task<IReadOnlyList<Financial>> ListMonthAsync(Guid academyId, DateOnly referenceMonth, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Financial>> ListStudentAsync(Guid academyId, Guid studentId, CancellationToken cancellationToken = default);
    Task<Financial?> GetAsync(Guid academyId, Guid id, CancellationToken cancellationToken = default);
    Task<Financial?> GetStudentMonthAsync(Guid academyId, Guid studentId, DateOnly referenceMonth, CancellationToken cancellationToken = default);
    Task<bool> TryCreateAsync(Financial financial, CancellationToken cancellationToken = default);
    Task<bool> TrySetStatusAsync(Guid academyId, Guid id, FinancialStatus expectedStatus, FinancialStatus status, DateTime? paidAt, decimal? amount, DateTime updatedAt, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonthlyChargeCandidate>> ListChargeCandidatesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Financial>> ListPendingBeforeAsync(DateOnly today, CancellationToken cancellationToken = default);
}