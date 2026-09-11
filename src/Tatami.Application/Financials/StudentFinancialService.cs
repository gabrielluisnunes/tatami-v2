using Tatami.Domain.Entities;
using Tatami.Domain.Enums;
using Tatami.Domain.Repositories;

namespace Tatami.Application.Financials;

public class StudentFinancialService
{
    private readonly IStudentRepository _students;
    private readonly IAcademyRepository _academies;
    private readonly IFinancialRepository _financials;
    private readonly IPixBrCodeGenerator _pix;
    private readonly TimeProvider _timeProvider;

    public StudentFinancialService(IStudentRepository students, IAcademyRepository academies,
        IFinancialRepository financials, IPixBrCodeGenerator pix, TimeProvider timeProvider)
    {
        _students = students;
        _academies = academies;
        _financials = financials;
        _pix = pix;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<StudentFinancialResponse>> ListMineAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        var student = await GetStudentAsync(userId, cancellationToken);
        var charges = await _financials.ListStudentAsync(student.AcademyId, student.Id, cancellationToken);
        return charges.Select(charge => new StudentFinancialResponse(charge.Id, charge.Amount, charge.DueDate,
            charge.ReferenceMonth, FinancialStatusNames.ToApi(charge.Status), charge.PaidAt)).ToList();
    }

    public async Task<PixResponse> GetPixAsync(Guid userId, Guid financialId, CancellationToken cancellationToken = default)
    {
        var charge = await GetOwnedChargeAsync(userId, financialId, cancellationToken);
        EnsurePayable(charge);
        var academy = await _academies.GetByIdAsync(charge.AcademyId, cancellationToken)
            ?? throw FinancialService.NotFound();
        if (string.IsNullOrWhiteSpace(academy.PixKey) || academy.PixKeyType is null)
            throw new FinancialException("PIX_NOT_CONFIGURED", "A academia ainda não configurou a chave PIX. Fale com a administração.");
        try
        {
            return new PixResponse(charge.Id, charge.Amount, _pix.Build(academy.PixKey, academy.PixKeyType.Value,
                academy.Name, charge.Amount, $"Mensalidade {FinancialMonth.Key(charge.ReferenceMonth)}"));
        }
        catch (ArgumentException)
        {
            throw new FinancialException("INVALID_PIX_CONFIGURATION", "A configuração PIX da academia não permite gerar o código. Fale com a administração.");
        }
    }

    public async Task MarkAwaitingConfirmationAsync(Guid userId, Guid financialId,
        CancellationToken cancellationToken = default)
    {
        var charge = await GetOwnedChargeAsync(userId, financialId, cancellationToken);
        if (charge.Status == FinancialStatus.AguardandoConfirmacao) return;
        EnsurePayable(charge);
        if (!await _financials.TrySetStatusAsync(charge.AcademyId, charge.Id, charge.Status,
                FinancialStatus.AguardandoConfirmacao, null, null, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken))
            throw FinancialService.Conflict();
    }

    private async Task<Student> GetStudentAsync(Guid userId, CancellationToken cancellationToken) =>
        await _students.GetByUserIdAsync(userId, cancellationToken)
        ?? throw new FinancialException("NOT_FOUND", "Aluno não encontrado.");

    private async Task<Financial> GetOwnedChargeAsync(Guid userId, Guid financialId, CancellationToken cancellationToken)
    {
        var student = await GetStudentAsync(userId, cancellationToken);
        var charge = await _financials.GetAsync(student.AcademyId, financialId, cancellationToken);
        if (charge is null || charge.StudentId != student.Id) throw FinancialService.NotFound();
        return charge;
    }

    private static void EnsurePayable(Financial charge)
    {
        if (charge.Status is not (FinancialStatus.Pending or FinancialStatus.Overdue))
            throw new FinancialException("INVALID_STATUS", "Esta cobrança não está disponível para pagamento.");
    }
}