using Tatami.Domain.Entities;

namespace Tatami.Domain.Repositories;

public interface IAcademyRepository
{
    Task<Academy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Academy?> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);

    Task<Academy> CreateAsync(Academy academy, CancellationToken cancellationToken = default);

    Task<Academy> UpdateAsync(Academy academy, CancellationToken cancellationToken = default);
}
