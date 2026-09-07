using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Tatami.Application.Students;
using Tatami.Domain.Enums;
using Tatami.Infrastructure.Identity;

namespace Tatami.Infrastructure.Identity;

public class StudentIdentityService : IStudentIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public StudentIdentityService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<(Guid UserId, string TemporaryPassword)> CreateAlunoAsync(
        string email,
        string fullName,
        Guid academyId,
        CancellationToken cancellationToken = default)
    {
        var temporaryPassword = GenerateTemporaryPassword();
        var now = DateTime.UtcNow;
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FullName = fullName,
            AcademyId = academyId,
            EmailConfirmed = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var result = await _userManager.CreateAsync(user, temporaryPassword);
        if (!result.Succeeded)
        {
            var message = string.Join(" ", result.Errors.Select(error => error.Description));
            throw new StudentException(message);
        }

        await _userManager.AddToRoleAsync(user, UserRole.Aluno);
        return (user.Id, temporaryPassword);
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return;
        }

        await _userManager.DeleteAsync(user);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is not null;
    }

    private static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        var chars = new char[12];
        chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];

        const string all = upper + lower + digits;
        for (var i = 3; i < chars.Length; i++)
        {
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        return new string(chars.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());
    }
}
