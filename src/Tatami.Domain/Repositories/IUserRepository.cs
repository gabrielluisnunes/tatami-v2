using Tatami.Domain.Entities;

namespace Tatami.Domain.Repositories;

public interface IUserRepository
{
    Task<ApplicationUserInfo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task UpdateAsync(ApplicationUserInfo user, CancellationToken cancellationToken = default);
}

public record ApplicationUserInfo(
    Guid Id,
    string Email,
    string FullName,
    Guid? AcademyId,
    IReadOnlyList<string> Roles);
