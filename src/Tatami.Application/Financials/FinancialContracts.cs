using Tatami.Domain.Entities;
using Tatami.Domain.Enums;

namespace Tatami.Application.Financials;

public interface IPixBrCodeGenerator
{
    string Build(string pixKey, PixKeyType keyType, string merchantName, decimal amount, string description);
}

public interface IFinancialNotificationSender
{
    Task SendDueTodayAsync(Financial financial, CancellationToken cancellationToken = default);
    Task SendOverdueAsync(Financial financial, CancellationToken cancellationToken = default);
}

public interface IFinancialExportWriter
{
    byte[] WriteOverdue(string month, IReadOnlyList<FinancialRow> rows);
}

public record FinancialRow(Guid StudentId, string StudentName, string Email, bool IsActive,
    int? PaymentDueDay, Guid? FinancialId, decimal? Amount, DateOnly? DueDate,
    DateOnly ReferenceMonth, string Status, DateTime? PaidAt);

public record FinancialOverview(string Month, decimal Received, decimal OverdueAmount,
    int OverdueCount, int ChargeCount, int AwaitingCount, IReadOnlyList<FinancialRow> Students);

public record StudentFinancialResponse(Guid Id, decimal Amount, DateOnly DueDate,
    DateOnly ReferenceMonth, string Status, DateTime? PaidAt);

public record PixResponse(Guid FinancialId, decimal Amount, string Payload);
public record MarkPaidRequest(Guid FinancialId);
public record ManualPaymentRequest(Guid StudentId, decimal Amount, DateTimeOffset PaidAt);
public record FinancialJobResult(int Processed);

public class FinancialException : Exception
{
    public string Code { get; }

    public FinancialException(string code, string message) : base(message)
    {
        Code = code;
    }
}

public static class FinancialStatusNames
{
    public static string ToApi(FinancialStatus status) => status switch
    {
        FinancialStatus.Pending => "pending",
        FinancialStatus.Paid => "paid",
        FinancialStatus.Overdue => "overdue",
        FinancialStatus.AguardandoConfirmacao => "aguardando_confirmacao",
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };
}