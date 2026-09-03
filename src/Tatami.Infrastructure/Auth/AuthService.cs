using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tatami.Application.Auth;
using Tatami.Domain.Enums;
using Tatami.Infrastructure.Identity;
using Tatami.Infrastructure.Persistence;

namespace Tatami.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TatamiDbContext _dbContext;
    private readonly JwtTokenService _jwtTokenService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        TatamiDbContext dbContext,
        JwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!UserRole.All.Contains(request.Role))
        {
            throw new AuthException("Role inválida. Use: admin, professor ou aluno.");
        }

        if (!string.Equals(request.Role, UserRole.Admin, StringComparison.OrdinalIgnoreCase))
        {
            throw new AuthException(
                "Cadastro público disponível apenas para administradores de academia.");
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            throw new AuthException("E-mail já cadastrado.");
        }

        var now = DateTime.UtcNow;
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName.Trim(),
            EmailConfirmed = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var message = string.Join(" ", result.Errors.Select(error => error.Description));
            throw new AuthException(message);
        }

        await _userManager.AddToRoleAsync(user, request.Role);

        return await BuildAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new AuthException("E-mail ou senha inválidos.");
        }

        return await BuildAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = await _dbContext.RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.Token == request.RefreshToken, cancellationToken);

        if (refreshToken is null || !refreshToken.IsActive)
        {
            throw new AuthException("Refresh token inválido ou expirado.");
        }

        refreshToken.IsRevoked = true;

        var response = await BuildAuthResponseAsync(refreshToken.User, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task LogoutAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.Token == request.RefreshToken, cancellationToken);

        if (refreshToken is null)
        {
            return;
        }

        refreshToken.IsRevoked = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthResponse> IssueAuthResponseForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new AuthException("Usuário não encontrado.");
        }

        return await BuildAuthResponseAsync(user, cancellationToken);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? UserRole.Aluno;

        await SanitizeAcademyReferenceAsync(user, cancellationToken);

        var (accessToken, expiresAt) = _jwtTokenService.GenerateAccessToken(user, roles);

        var refreshTokenValue = JwtTokenService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = _jwtTokenService.GetRefreshTokenExpiration(),
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            refreshTokenValue,
            expiresAt,
            new AuthUserResponse(user.Id, user.Email ?? string.Empty, user.FullName, role, user.AcademyId));
    }

    private async Task SanitizeAcademyReferenceAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        if (!user.AcademyId.HasValue)
        {
            return;
        }

        var academyExists = await _dbContext.Academies
            .AsNoTracking()
            .AnyAsync(academy => academy.Id == user.AcademyId.Value, cancellationToken);

        if (academyExists)
        {
            return;
        }

        user.AcademyId = null;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var message = string.Join(" ", result.Errors.Select(error => error.Description));
            throw new AuthException(message);
        }
    }
}

public class AuthException : Exception
{
    public AuthException(string message) : base(message)
    {
    }
}
