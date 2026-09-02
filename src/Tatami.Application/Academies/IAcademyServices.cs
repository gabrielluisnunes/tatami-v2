using Tatami.Application.Academies;

namespace Tatami.Application.Academies;

public interface IOnboardingService
{
    Task<OnboardingResponse> CompleteOnboardingAsync(
        Guid userId,
        CreateOnboardingRequest request,
        CancellationToken cancellationToken = default);
}

public interface IAcademyService
{
    Task<AcademyResponse> GetMyAcademyAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<AcademyResponse> UpdateMyAcademyAsync(
        Guid userId,
        UpdateAcademyRequest request,
        CancellationToken cancellationToken = default);
}
