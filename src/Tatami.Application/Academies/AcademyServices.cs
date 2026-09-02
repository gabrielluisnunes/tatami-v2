using Tatami.Application.Auth;
using Tatami.Domain.Constants;
using Tatami.Domain.Entities;
using Tatami.Domain.Enums;
using Tatami.Domain.Repositories;

namespace Tatami.Application.Academies;

public class OnboardingService : IOnboardingService
{
    private readonly IUserRepository _userRepository;
    private readonly IOnboardingRepository _onboardingRepository;
    private readonly IAuthService _authService;

    public OnboardingService(
        IUserRepository userRepository,
        IOnboardingRepository onboardingRepository,
        IAuthService authService)
    {
        _userRepository = userRepository;
        _onboardingRepository = onboardingRepository;
        _authService = authService;
    }

    public async Task<OnboardingResponse> CompleteOnboardingAsync(
        Guid userId,
        CreateOnboardingRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new AcademyException("Usuário não encontrado.");
        }

        if (!user.Roles.Contains(UserRole.Admin))
        {
            throw new AcademyException("Apenas administradores podem criar uma academia.");
        }

        if (user.AcademyId.HasValue)
        {
            throw new AcademyException("Academia já configurada.");
        }

        if (!SportTypeExtensions.AllSlugs.Contains(request.Sport))
        {
            throw new AcademyException("Esporte inválido.");
        }

        if (request.MonthlyPrice < 0)
        {
            throw new AcademyException("Preço mensal inválido.");
        }

        var now = DateTime.UtcNow;
        var academy = new Academy
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            Name = request.AcademyName.Trim(),
            Sport = SportTypeExtensions.FromSlug(request.Sport),
            MonthlyPrice = request.MonthlyPrice,
            SubscriptionStatus = SubscriptionStatus.Trial,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _onboardingRepository.CompleteAsync(academy, user, cancellationToken);

        var auth = await _authService.IssueAuthResponseForUserAsync(userId, cancellationToken);

        return new OnboardingResponse(MapAcademy(academy), auth);
    }

    internal static AcademyResponse MapAcademy(Academy academy) =>
        new(
            academy.Id,
            academy.Name,
            academy.Sport.ToSlug(),
            academy.MonthlyPrice,
            academy.SubscriptionStatus,
            academy.OwnerId);
}

public class AcademyService : IAcademyService
{
    private readonly IUserRepository _userRepository;
    private readonly IAcademyRepository _academyRepository;

    public AcademyService(
        IUserRepository userRepository,
        IAcademyRepository academyRepository)
    {
        _userRepository = userRepository;
        _academyRepository = academyRepository;
    }

    public async Task<AcademyResponse> GetMyAcademyAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.AcademyId.HasValue)
        {
            throw new AcademyException("Academia não encontrada.");
        }

        var academy = await _academyRepository.GetByIdAsync(user.AcademyId.Value, cancellationToken);
        if (academy is null)
        {
            throw new AcademyException("Academia não encontrada.");
        }

        return OnboardingService.MapAcademy(academy);
    }

    public async Task<AcademyResponse> UpdateMyAcademyAsync(
        Guid userId,
        UpdateAcademyRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.Roles.Contains(UserRole.Admin))
        {
            throw new AcademyException("Acesso negado.");
        }

        if (!user.AcademyId.HasValue)
        {
            throw new AcademyException("Academia não configurada.");
        }

        if (!SportTypeExtensions.AllSlugs.Contains(request.Sport))
        {
            throw new AcademyException("Esporte inválido.");
        }

        if (request.MonthlyPrice < 0)
        {
            throw new AcademyException("Preço mensal inválido.");
        }

        var academy = await _academyRepository.GetByIdAsync(user.AcademyId.Value, cancellationToken);
        if (academy is null)
        {
            throw new AcademyException("Academia não encontrada.");
        }

        academy.Name = request.Name.Trim();
        academy.Sport = SportTypeExtensions.FromSlug(request.Sport);
        academy.MonthlyPrice = request.MonthlyPrice;
        academy.UpdatedAt = DateTime.UtcNow;

        await _academyRepository.UpdateAsync(academy, cancellationToken);

        return OnboardingService.MapAcademy(academy);
    }
}

public class AcademyException : Exception
{
    public AcademyException(string message) : base(message)
    {
    }
}
