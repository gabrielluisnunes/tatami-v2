namespace Tatami.Application.Auth;

public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string Role);

public record LoginRequest(
    string Email,
    string Password);

public record RefreshTokenRequest(
    string RefreshToken);

public record AuthUserResponse(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    Guid? AcademyId);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    AuthUserResponse User);
