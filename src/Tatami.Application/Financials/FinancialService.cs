using Tatami.Domain.Entities;
using Tatami.Domain.Enums;
using Tatami.Domain.Repositories;

namespace Tatami.Application.Financials;

public class FinancialService
{
    private readonly IAcademyRepository _academies;
    private readonly IStudentRepository _students;
    private readonly IFinancialRepository _financials;
    private readonly IFinancialExportWriter _exportWriter;
    private readonly TimeProvider _timeProvider;

    public FinancialService(IAcademyRepository academies, IStudentRepository students,
        IFinancialRepository financials, IFinancialExportWriter exportWriter, TimeProvider timeProvider)
    {
        _academies = academies;
        _students = students;
        _financials = financials;
        _exportWriter = exportWriter;
        _timeProvider = timeProvider;
    }

    public async Task<FinancialOverview> GetMonthOverviewAsync(Guid adminId, int year, int month,
        CancellationToken cancellationToken = default)
    {
        var academy = await GetAcademyAsync(adminId, cancellationToken);
        var referenceMonth = FinancialMonth.First(year, month);
        var charges = await _financials.ListMonthAsync(academy.Id, referenceMonth, cancellationToken);
        var students = await _students.ListByAcademyAsync(academy.Id, null, null, cancellationToken);
        var byStudent = charges.ToDictionary(charge => charge.StudentId);
        var rows = students.Where(student => student.IsActive || byStudent.ContainsKey(student.Id))
            .Select(student =>
            {
                byStudent.TryGetValue(student.Id, out var charge);
                return new FinancialRow(student.Id, student.FullName, student.Email, student.IsActive,
                    student.PaymentDueDay, charge?.Id, charge?.Amount, charge?.DueDate, referenceMonth,
                    charge is null ? "sem_cobranca" : FinancialStatusNames.ToApi(charge.Status), charge?.PaidAt);
            }).OrderBy(row => row.StudentName).ToList();
        return new FinancialOverview(FinancialMonth.Key(referenceMonth),
            charges.Where(charge => charge.Status == FinancialStatus.Paid).Sum(charge => charge.Amount),
            charges.Where(charge => charge.Status == FinancialStatus.Overdue).Sum(charge => charge.Amount),
            charges.Count(charge => charge.Status == FinancialStatus.Overdue), charges.Count,
            charges.Count(charge => charge.Status == FinancialStatus.AguardandoConfirmacao), rows);
    }

    public async Task MarkPaidAsync(Guid adminId, Guid financialId, CancellationToken cancellationToken = default)
    {
        var academy = await GetAcademyAsync(adminId, cancellationToken);
        var charge = await _financials.GetAsync(academy.Id, financialId, cancellationToken)
            ?? throw NotFound();
        if (charge.Status == FinancialStatus.Paid) return;
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (!await _financials.TrySetStatusAsync(academy.Id, charge.Id, charge.Status, FinancialStatus.Paid,
                now, null, now, cancellationToken))
            throw Conflict();
    }

    public async Task ManualPaymentAsync(Guid adminId, Guid studentId, decimal amount, DateTimeOffset paidAt,
        CancellationToken cancellationToken = default)
    {
        var academy = await GetAcademyAsync(adminId, cancellationToken);
        ValidateAmount(amount);
        var now = _timeProvider.GetUtcNow();
        if (paidAt == default || paidAt > now)
            throw new FinancialException("INVALID_PAID_AT", "Informe uma data de pagamento válida, não futura.");
        var student = await _students.GetByIdAsync(studentId, academy.Id, cancellationToken)
            ?? throw new FinancialException("NOT_FOUND", "Aluno não encontrado.");
        var today = FinancialMonth.Today(_timeProvider);
        var referenceMonth = FinancialMonth.First(today.Year, today.Month);
        var charge = await _financials.GetStudentMonthAsync(academy.Id, student.Id, referenceMonth, cancellationToken);
        if (charge is null)
        {
            var created = new Financial
            {
                Id = Guid.NewGuid(), AcademyId = academy.Id, StudentId = student.Id, Amount = amount,
                ReferenceMonth = referenceMonth,
                DueDate = FinancialMonth.DueDateForMonth(today.Year, today.Month, student.PaymentDueDay ?? today.Day),
                Status = FinancialStatus.Paid, PaidAt = paidAt.UtcDateTime,
                CreatedAt = now.UtcDateTime, UpdatedAt = now.UtcDateTime,
            };
            if (await _financials.TryCreateAsync(created, cancellationToken)) return;
            charge = await _financials.GetStudentMonthAsync(academy.Id, student.Id, referenceMonth, cancellationToken)
                ?? throw Conflict();
        }
        if (charge.Status == FinancialStatus.Paid)
            throw new FinancialException("ALREADY_PAID", "A mensalidade deste mês já está paga.");
        if (!await _financials.TrySetStatusAsync(academy.Id, charge.Id, charge.Status, FinancialStatus.Paid,
                paidAt.UtcDateTime, amount, now.UtcDateTime, cancellationToken))
            throw Conflict();
    }

    public async Task<byte[]> ExportOverdueAsync(Guid adminId, int year, int month,
        CancellationToken cancellationToken = default)
    {
        var overview = await GetMonthOverviewAsync(adminId, year, month, cancellationToken);
        return _exportWriter.WriteOverdue(overview.Month,
            overview.Students.Where(row => row.Status == "overdue").ToList());
    }

    private async Task<Academy> GetAcademyAsync(Guid adminId, CancellationToken cancellationToken) =>
        await _academies.GetByOwnerIdAsync(adminId, cancellationToken)
        ?? throw new FinancialException("ACADEMY_NOT_FOUND", "Academia não encontrada.");

    internal static void ValidateAmount(decimal amount)
    {
        if (amount <= 0 || amount > 99999999.99m || decimal.Round(amount, 2) != amount)
            throw new FinancialException("INVALID_AMOUNT", "Informe um valor positivo com até duas casas decimais.");
    }

    internal static FinancialException NotFound() => new("NOT_FOUND", "Cobrança não encontrada.");
    internal static FinancialException Conflict() => new("CONFLICT", "A cobrança foi alterada. Atualize a lista e tente novamente.");
}