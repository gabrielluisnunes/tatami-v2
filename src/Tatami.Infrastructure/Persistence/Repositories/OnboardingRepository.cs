using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tatami.Domain.Entities;
using Tatami.Domain.Repositories;
using Tatami.Infrastructure.Identity;

namespace Tatami.Infrastructure.Persistence.Repositories;

public class OnboardingRepository : IOnboardingRepository
{
    private readonly TatamiDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public OnboardingRepository(
        TatamiDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task CompleteAsync(
        Academy academy,
        ApplicationUserInfo userInfo,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _dbContext.Academies.Add(academy);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var user = await _userManager.FindByIdAsync(userInfo.Id.ToString());
            if (user is null)
            {
                throw new InvalidOperationException("Usuário não encontrado.");
            }

            user.AcademyId = academy.Id;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var message = string.Join(" ", result.Errors.Select(error => error.Description));
                throw new InvalidOperationException(message);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
