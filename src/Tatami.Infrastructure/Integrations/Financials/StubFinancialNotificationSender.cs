using Microsoft.Extensions.Logging;
using Tatami.Application.Financials;
using Tatami.Domain.Entities;

namespace Tatami.Infrastructure.Integrations.Financials;

public class StubFinancialNotificationSender : IFinancialNotificationSender
{
    private readonly ILogger<StubFinancialNotificationSender> _logger;

    public StubFinancialNotificationSender(ILogger<StubFinancialNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task SendDueTodayAsync(Financial financial, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Financial due-today notification stub. Notification not sent yet.");
        return Task.CompletedTask;
    }

    public Task SendOverdueAsync(Financial financial, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Financial overdue notification stub. Notification not sent yet.");
        return Task.CompletedTask;
    }
}