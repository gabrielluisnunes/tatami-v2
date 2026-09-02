using Tatami.Domain.Entities;

namespace Tatami.Domain.Repositories;

public interface IOnboardingRepository
{
    Task CompleteAsync(
        Academy academy,
        ApplicationUserInfo user,
        CancellationToken cancellationToken = default);
}
