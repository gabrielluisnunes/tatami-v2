using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tatami.Domain.Repositories;
using Tatami.Infrastructure.Identity;

namespace Tatami.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserRepository(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<ApplicationUserInfo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new ApplicationUserInfo(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            user.AcademyId,
            roles.ToList());
    }

    public async Task UpdateAsync(ApplicationUserInfo userInfo, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userInfo.Id.ToString());
        if (user is null)
        {
            throw new InvalidOperationException("Usuário não encontrado.");
        }

        user.FullName = userInfo.FullName;
        user.AcademyId = userInfo.AcademyId;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var message = string.Join(" ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException(message);
        }
    }
}
