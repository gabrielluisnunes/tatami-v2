namespace Tatami.Application.Academies;

public record CreateOnboardingRequest(
    string AcademyName,
    string Sport,
    decimal MonthlyPrice);

public record UpdateAcademyRequest(
    string Name,
    string Sport,
    decimal MonthlyPrice,
    string? PixKey = null,
    string? PixKeyType = null);

public record AcademyResponse(
    Guid Id,
    string Name,
    string Sport,
    decimal MonthlyPrice,
    string SubscriptionStatus,
    Guid OwnerId,
    string? Plan,
    string? StripeCustomerId,
    DateTime? TrialEndsAt,
    string? PixKey = null,
    string? PixKeyType = null);

public record OnboardingResponse(
    AcademyResponse Academy,
    Auth.AuthResponse Auth);
