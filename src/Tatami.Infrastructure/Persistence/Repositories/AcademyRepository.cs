using Microsoft.EntityFrameworkCore;
using Tatami.Domain.Entities;
using Tatami.Domain.Repositories;

namespace Tatami.Infrastructure.Persistence.Repositories;

public class AcademyRepository : IAcademyRepository
{
    private readonly TatamiDbContext _dbContext;

    public AcademyRepository(TatamiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Academy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Academies.FirstOrDefaultAsync(academy => academy.Id == id, cancellationToken);

    public Task<Academy?> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default) =>
        _dbContext.Academies.FirstOrDefaultAsync(academy => academy.OwnerId == ownerId, cancellationToken);

    public async Task<Academy> CreateAsync(Academy academy, CancellationToken cancellationToken = default)
    {
        _dbContext.Academies.Add(academy);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return academy;
    }

    public async Task<Academy> UpdateAsync(Academy academy, CancellationToken cancellationToken = default)
    {
        _dbContext.Academies.Update(academy);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return academy;
    }
}
