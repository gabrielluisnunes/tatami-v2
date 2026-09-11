using Moq;
using Tatami.Application.Financials;
using Tatami.Domain.Entities;
using Tatami.Domain.Enums;
using Tatami.Domain.Repositories;

namespace Tatami.Tests;

public class FinancialServiceTests
{
    private readonly Mock<IAcademyRepository> _academies = new();
    private readonly Mock<IStudentRepository> _students = new();
    private readonly Mock<IFinancialRepository> _financials = new();
    private readonly Mock<IFinancialExportWriter> _export = new();
    private readonly Mock<IPixBrCodeGenerator> _pix = new();
    private readonly Mock<IFinancialNotificationSender> _notifications = new();
    private readonly FixedTimeProvider _clock = new("2026-09-11T02:00:00Z");
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Academy _academy = new() { Id = Guid.NewGuid(), Name = "Academia", PixKey = "pix@example.com", PixKeyType = PixKeyType.Email };
    private readonly Student _student;
    private readonly Financial _charge;

    public FinancialServiceTests()
    {
        _student = new Student { Id = Guid.NewGuid(), AcademyId = _academy.Id, UserId = Guid.NewGuid(), FullName = "Aluno", PaymentDueDay = 10 };
        _charge = new Financial { Id = Guid.NewGuid(), AcademyId = _academy.Id, StudentId = _student.Id,
            Amount = 150, DueDate = new DateOnly(2026, 9, 10), ReferenceMonth = new DateOnly(2026, 9, 1) };
        _academies.Setup(repository => repository.GetByOwnerIdAsync(_adminId, default)).ReturnsAsync(_academy);
        _academies.Setup(repository => repository.GetByIdAsync(_academy.Id, default)).ReturnsAsync(_academy);
        _students.Setup(repository => repository.GetByUserIdAsync(_student.UserId, default)).ReturnsAsync(_student);
        _students.Setup(repository => repository.GetByIdAsync(_student.Id, _academy.Id, default)).ReturnsAsync(_student);
        _financials.Setup(repository => repository.GetAsync(_academy.Id, _charge.Id, default)).ReturnsAsync(_charge);
    }

    private FinancialService Admin() => new(_academies.Object, _students.Object, _financials.Object, _export.Object, _clock);
    private StudentFinancialService StudentService() => new(_students.Object, _academies.Object, _financials.Object, _pix.Object, _clock);

    [Theory]
    [InlineData(2025, 2, 31, 28)]
    [InlineData(2024, 2, 31, 29)]
    [InlineData(2026, 4, 31, 30)]
    [InlineData(2026, 1, 31, 31)]
    [InlineData(2026, 1, 0, 1)]
    public void DueDateClampsLikeV1(int year, int month, int dueDay, int expectedDay) =>
        Assert.Equal(new DateOnly(year, month, expectedDay), FinancialMonth.DueDateForMonth(year, month, dueDay));

    [Fact]
    public void MonthUsesBrasiliaAtUtcBoundary()
    {
        var clock = new FixedTimeProvider("2026-10-01T02:59:59Z");
        Assert.Equal(new DateOnly(2026, 9, 30), FinancialMonth.Today(clock));
        Assert.Equal(new DateOnly(2026, 9, 1), FinancialMonth.Parse(null, clock));
        Assert.Equal(new DateOnly(2026, 10, 1), FinancialMonth.Today(new FixedTimeProvider("2026-10-01T03:00:00Z")));
    }

    [Theory]
    [InlineData("2026-13")]
    [InlineData("2026-9")]
    [InlineData("0000-01")]
    [InlineData("")]
    public void InvalidMonthIsRejected(string month) => Assert.Throws<FinancialException>(() => FinancialMonth.Parse(month, _clock));

    [Fact]
    public async Task OverviewIncludesActiveWithoutChargeAndHistoricalInactive()
    {
        _student.IsActive = false;
        var active = new Student { Id = Guid.NewGuid(), FullName = "Sem cobrança" };
        var inactive = new Student { Id = Guid.NewGuid(), IsActive = false };
        _charge.Status = FinancialStatus.Overdue;
        _students.Setup(repository => repository.ListByAcademyAsync(_academy.Id, null, null, default)).ReturnsAsync(new[] { _student, active, inactive });
        _financials.Setup(repository => repository.ListMonthAsync(_academy.Id, _charge.ReferenceMonth, default)).ReturnsAsync(new[] { _charge });
        var overview = await Admin().GetMonthOverviewAsync(_adminId, 2026, 9);
        Assert.Equal(150, overview.OverdueAmount);
        Assert.Equal(1, overview.OverdueCount);
        Assert.Equal(1, overview.ChargeCount);
        Assert.Equal(2, overview.Students.Count);
        Assert.Contains(overview.Students, row => row.StudentId == active.Id && row.Status == "sem_cobranca" && row.FinancialId == null);
    }

    [Fact]
    public async Task ExportUsesRequestedMonthNotCurrentMonthAndOnlyOverdue()
    {
        var selectedMonth = new DateOnly(2026, 7, 1);
        _charge.ReferenceMonth = selectedMonth;
        _charge.Status = FinancialStatus.Overdue;
        var paidStudent = new Student { Id = Guid.NewGuid(), FullName = "Pago" };
        var paid = new Financial { Id = Guid.NewGuid(), StudentId = paidStudent.Id, Status = FinancialStatus.Paid, Amount = 99, ReferenceMonth = selectedMonth };
        _students.Setup(repository => repository.ListByAcademyAsync(_academy.Id, null, null, default)).ReturnsAsync(new[] { _student, paidStudent });
        _financials.Setup(repository => repository.ListMonthAsync(_academy.Id, selectedMonth, default)).ReturnsAsync(new[] { _charge, paid });
        _export.Setup(writer => writer.WriteOverdue("2026-07", It.Is<IReadOnlyList<FinancialRow>>(rows => rows.Count == 1 && rows[0].FinancialId == _charge.Id)))
            .Returns(new byte[] { 1, 2, 3 });
        Assert.Equal(new byte[] { 1, 2, 3 }, await Admin().ExportOverdueAsync(_adminId, 2026, 7));
        _financials.Verify(repository => repository.ListMonthAsync(_academy.Id, new DateOnly(2026, 9, 1), default), Times.Never);
    }

    [Fact]
    public async Task AdminCannotAccessOtherAcademysCharge()
    {
        var error = await Assert.ThrowsAsync<FinancialException>(() => Admin().MarkPaidAsync(_adminId, Guid.NewGuid()));
        Assert.Equal("NOT_FOUND", error.Code);
        Assert.DoesNotContain(_financials.Invocations, invocation => invocation.Method.Name == nameof(IFinancialRepository.TrySetStatusAsync));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StudentCannotReadOrChangeAnotherStudentsCharge(bool awaiting)
    {
        _charge.StudentId = Guid.NewGuid();
        var error = await Assert.ThrowsAsync<FinancialException>(async () =>
        {
            if (awaiting) await StudentService().MarkAwaitingConfirmationAsync(_student.UserId, _charge.Id);
            else await StudentService().GetPixAsync(_student.UserId, _charge.Id);
        });
        Assert.Equal("NOT_FOUND", error.Code);
        _pix.VerifyNoOtherCalls();
        Assert.DoesNotContain(_financials.Invocations, invocation => invocation.Method.Name == nameof(IFinancialRepository.TrySetStatusAsync));
    }

    [Theory]
    [InlineData(FinancialStatus.Paid)]
    [InlineData(FinancialStatus.AguardandoConfirmacao)]
    public async Task PixOnlyForPendingOrOverdue(FinancialStatus status)
    {
        _charge.Status = status;
        Assert.Equal("INVALID_STATUS", (await Assert.ThrowsAsync<FinancialException>(() => StudentService().GetPixAsync(_student.UserId, _charge.Id))).Code);
    }

    [Fact]
    public async Task PixUsesPersistedAmountAndAcademyKey()
    {
        _pix.Setup(generator => generator.Build("pix@example.com", PixKeyType.Email, "Academia", 150, "Mensalidade 2026-09")).Returns("brcode");
        var result = await StudentService().GetPixAsync(_student.UserId, _charge.Id);
        Assert.Equal("brcode", result.Payload);
        Assert.Equal(150, result.Amount);
        _academy.PixKey = null;
        Assert.Equal("PIX_NOT_CONFIGURED", (await Assert.ThrowsAsync<FinancialException>(() => StudentService().GetPixAsync(_student.UserId, _charge.Id))).Code);
    }

    [Fact]
    public async Task AwaitingDoesNotMarkPaidAndRejectsConcurrentChange()
    {
        var error = await Assert.ThrowsAsync<FinancialException>(() => StudentService().MarkAwaitingConfirmationAsync(_student.UserId, _charge.Id));
        Assert.Equal("CONFLICT", error.Code);
        _financials.Verify(repository => repository.TrySetStatusAsync(_academy.Id, _charge.Id, FinancialStatus.Pending,
            FinancialStatus.AguardandoConfirmacao, null, null, _clock.GetUtcNow().UtcDateTime, default));
        _charge.Status = FinancialStatus.AguardandoConfirmacao;
        await StudentService().MarkAwaitingConfirmationAsync(_student.UserId, _charge.Id);
        _charge.Status = FinancialStatus.Paid;
        await Assert.ThrowsAsync<FinancialException>(() => StudentService().MarkAwaitingConfirmationAsync(_student.UserId, _charge.Id));
    }

    [Fact]
    public async Task MarkPaidIsIdempotent()
    {
        _charge.Status = FinancialStatus.Paid;
        await Admin().MarkPaidAsync(_adminId, _charge.Id);
        Assert.DoesNotContain(_financials.Invocations, invocation => invocation.Method.Name == nameof(IFinancialRepository.TrySetStatusAsync));
    }

    [Fact]
    public async Task ManualCreatesPaidCurrentMonthWithUtcTimestamp()
    {
        Financial? created = null;
        _financials.Setup(repository => repository.TryCreateAsync(It.IsAny<Financial>(), default))
            .Callback<Financial, CancellationToken>((charge, _) => created = charge).ReturnsAsync(true);
        var paidAt = DateTimeOffset.Parse("2026-08-31T22:00:00-03:00");
        await Admin().ManualPaymentAsync(_adminId, _student.Id, 125.50m, paidAt);
        Assert.NotNull(created);
        Assert.Equal(new DateOnly(2026, 9, 1), created.ReferenceMonth);
        Assert.Equal(FinancialStatus.Paid, created.Status);
        Assert.Equal(paidAt.UtcDateTime, created.PaidAt);
        Assert.Equal(125.50m, created.Amount);
    }

    [Fact]
    public async Task ManualUpdatesExistingOpenChargeAndCannotOverwritePaid()
    {
        _financials.Setup(repository => repository.GetStudentMonthAsync(_academy.Id, _student.Id, _charge.ReferenceMonth, default)).ReturnsAsync(_charge);
        var paidAt = _clock.GetUtcNow();
        _financials.Setup(repository => repository.TrySetStatusAsync(_academy.Id, _charge.Id, FinancialStatus.Pending,
            FinancialStatus.Paid, paidAt.UtcDateTime, 120m, paidAt.UtcDateTime, default)).ReturnsAsync(true);
        await Admin().ManualPaymentAsync(_adminId, _student.Id, 120m, paidAt);
        _financials.Verify(repository => repository.TryCreateAsync(It.IsAny<Financial>(), default), Times.Never);
        _charge.Status = FinancialStatus.Paid;
        Assert.Equal("ALREADY_PAID", (await Assert.ThrowsAsync<FinancialException>(() => Admin().ManualPaymentAsync(_adminId, _student.Id, 120m, paidAt))).Code);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.001")]
    [InlineData("100000000")]
    public async Task ManualRejectsInvalidAmount(string value) =>
        Assert.Equal("INVALID_AMOUNT", (await Assert.ThrowsAsync<FinancialException>(() => Admin().ManualPaymentAsync(_adminId,
            _student.Id, decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture), _clock.GetUtcNow()))).Code);

    [Fact]
    public async Task ManualRejectsFutureDate() =>
        Assert.Equal("INVALID_PAID_AT", (await Assert.ThrowsAsync<FinancialException>(() => Admin().ManualPaymentAsync(_adminId,
            _student.Id, 100, _clock.GetUtcNow().AddDays(1)))).Code);

    [Fact]
    public async Task GenerateUsesBrasiliaDaySkipsFutureAndNotifiesOnlyNewDueToday()
    {
        var tomorrow = new MonthlyChargeCandidate(Guid.NewGuid(), _academy.Id, 150, 11);
        var today = new MonthlyChargeCandidate(_student.Id, _academy.Id, 150, 10);
        var past = new MonthlyChargeCandidate(Guid.NewGuid(), _academy.Id, 150, 1);
        _financials.Setup(repository => repository.ListChargeCandidatesAsync(default)).ReturnsAsync(new[] { today, tomorrow, past });
        var keys = new HashSet<(Guid, DateOnly)>();
        _financials.Setup(repository => repository.TryCreateAsync(It.IsAny<Financial>(), default))
            .ReturnsAsync((Financial charge, CancellationToken _) => keys.Add((charge.StudentId, charge.ReferenceMonth)));
        var jobs = new FinancialJobs(_financials.Object, _notifications.Object, _clock);
        Assert.Equal(2, (await jobs.GenerateMonthlyChargesAsync()).Processed);
        Assert.Equal(0, (await jobs.GenerateMonthlyChargesAsync()).Processed);
        Assert.All(keys, key => Assert.Equal(new DateOnly(2026, 9, 1), key.Item2));
        _notifications.Verify(sender => sender.SendDueTodayAsync(It.Is<Financial>(charge => charge.StudentId == _student.Id && charge.Amount == 150), default), Times.Once);
    }

    [Fact]
    public async Task OverdueNotifiesOnlySuccessfulPendingTransition()
    {
        _financials.Setup(repository => repository.ListPendingBeforeAsync(new DateOnly(2026, 9, 10), default)).ReturnsAsync(new[] { _charge });
        _financials.SetupSequence(repository => repository.TrySetStatusAsync(_academy.Id, _charge.Id, FinancialStatus.Pending,
            FinancialStatus.Overdue, null, null, _clock.GetUtcNow().UtcDateTime, default)).ReturnsAsync(true).ReturnsAsync(false);
        var jobs = new FinancialJobs(_financials.Object, _notifications.Object, _clock);
        Assert.Equal(1, (await jobs.UpdateOverdueAsync()).Processed);
        Assert.Equal(0, (await jobs.UpdateOverdueAsync()).Processed);
        _notifications.Verify(sender => sender.SendOverdueAsync(_charge, default), Times.Once);
    }

    private sealed class FixedTimeProvider(string value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse(value);
    }
}