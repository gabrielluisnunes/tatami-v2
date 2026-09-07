namespace Tatami.Application.Students;

public interface IWelcomeEmailSender
{
    Task<bool> SendAsync(
        string email,
        string fullName,
        string temporaryPassword,
        CancellationToken cancellationToken = default);
}
