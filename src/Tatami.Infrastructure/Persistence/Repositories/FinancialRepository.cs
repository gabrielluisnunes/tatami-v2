using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tatami.Domain.Entities;
using Tatami.Domain.Enums;
using Tatami.Domain.Repositories;

namespace Tatami.Infrastructure.Persistence.Repositories;

public class FinancialRepository : IFinancialRepository
{
    private readonly TatamiDbContext _dbContext;

    public FinancialRepository(TatamiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Financial>> ListMonthAsync(
        Guid academyId, DateOnly referenceMonth, CancellationToken cancellationToken = default) =>
        await _dbContext.Financials.AsNoTracking()
            .Where(financial => financial.AcademyId == academyId && financial.ReferenceMonth == referenceMonth)
            .OrderBy(financial => financial.DueDate).ThenBy(financial => financial.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Financial>> ListStudentAsync(
        Guid academyId, Guid studentId, CancellationToken cancellationToken = default) =>
        await _dbContext.Financials.AsNoTracking()
            .Where(financial => financial.AcademyId == academyId && financial.StudentId == studentId)
            .OrderByDescending(financial => financial.ReferenceMonth).ThenBy(financial => financial.Id)
            .ToListAsync(cancellationToken);

    public Task<Financial?> GetAsync(Guid academyId, Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Financials.AsNoTracking()
            .FirstOrDefaultAsync(financial => financial.AcademyId == academyId && financial.Id == id, cancellationToken);

    public Task<Financial?> GetStudentMonthAsync(
        Guid academyId, Guid studentId, DateOnly referenceMonth, CancellationToken cancellationToken = default) =>
        _dbContext.Financials.AsNoTracking()
            .FirstOrDefaultAsync(financial => financial.AcademyId == academyId
                && financial.StudentId == studentId && financial.ReferenceMonth == referenceMonth, cancellationToken);

    public async Task<bool> TryCreateAsync(Financial financial, CancellationToken cancellationToken = default)
    {
        _dbContext.Financials.Add(financial);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_financials_StudentId_ReferenceMonth",
            })
        {
            _dbContext.Entry(financial).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<bool> TrySetStatusAsync(
        Guid academyId, Guid id, FinancialStatus expectedStatus, FinancialStatus status,
        DateTime? paidAt, decimal? amount, DateTime updatedAt, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Financials
            .Where(financial => financial.AcademyId == academyId && financial.Id == id && financial.Status == expectedStatus);

        var affected = amount.HasValue
            ? await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(financial => financial.Status, status)
                .SetProperty(financial => financial.PaidAt, paidAt)
                .SetProperty(financial => financial.Amount, amount.Value)
                .SetProperty(financial => financial.UpdatedAt, updatedAt), cancellationToken)
            : await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(financial => financial.Status, status)
                .SetProperty(financial => financial.PaidAt, paidAt)
                .SetProperty(financial => financial.UpdatedAt, updatedAt), cancellationToken);

        return affected == 1;
    }

    public async Task<IReadOnlyList<MonthlyChargeCandidate>> ListChargeCandidatesAsync(
        CancellationToken cancellationToken = default) =>
        await (from student in _dbContext.Students.AsNoTracking()
               join academy in _dbContext.Academies.AsNoTracking() on student.AcademyId equals academy.Id
               where student.IsActive && student.PaymentDueDay >= 1 && student.PaymentDueDay <= 31
                   && academy.MonthlyPrice > 0
               orderby student.Id
               select new MonthlyChargeCandidate(student.Id, student.AcademyId,
                   academy.MonthlyPrice, student.PaymentDueDay!.Value))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Financial>> ListPendingBeforeAsync(
        DateOnly today, CancellationToken cancellationToken = default) =>
        await _dbContext.Financials.AsNoTracking()
            .Where(financial => financial.Status == FinancialStatus.Pending && financial.DueDate < today)
            .OrderBy(financial => financial.DueDate).ThenBy(financial => financial.Id)
            .ToListAsync(cancellationToken);
}