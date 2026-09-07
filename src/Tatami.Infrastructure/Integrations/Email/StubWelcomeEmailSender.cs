using Microsoft.Extensions.Logging;
using Tatami.Application.Students;

namespace Tatami.Infrastructure.Integrations.Email;

public class StubWelcomeEmailSender : IWelcomeEmailSender
{
    private readonly ILogger<StubWelcomeEmailSender> _logger;

    public StubWelcomeEmailSender(ILogger<StubWelcomeEmailSender> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendAsync(
        string email,
        string fullName,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Welcome email stub for {Email} ({FullName}). Password not sent by email yet.",
            email,
            fullName);
        return Task.FromResult(false);
    }
}
