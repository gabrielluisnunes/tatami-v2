using System.Text.RegularExpressions;
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
            academy.OwnerId,
            academy.Plan,
            academy.StripeCustomerId,
            academy.TrialEndsAt,
            academy.PixKey,
            academy.PixKeyType?.ToSlug());
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

        var (pixKey, pixKeyType) = ValidatePix(request.PixKey, request.PixKeyType);

        academy.Name = request.Name.Trim();
        academy.Sport = SportTypeExtensions.FromSlug(request.Sport);
        academy.MonthlyPrice = request.MonthlyPrice;
        academy.PixKey = pixKey;
        academy.PixKeyType = pixKeyType;
        academy.UpdatedAt = DateTime.UtcNow;

        await _academyRepository.UpdateAsync(academy, cancellationToken);

        return OnboardingService.MapAcademy(academy);
    }

    private static (string? Key, PixKeyType? Type) ValidatePix(string? key, string? type)
    {
        key = string.IsNullOrWhiteSpace(key) ? null : key.Trim();
        type = string.IsNullOrWhiteSpace(type) ? null : type.Trim().ToLowerInvariant();

        if (key is null && type is null)
        {
            return (null, null);
        }

        if (key is null || type is null)
        {
            throw new AcademyException("Informe a chave PIX e seu tipo, ou deixe ambos vazios.");
        }

        if (!PixKeyTypeExtensions.AllSlugs.Contains(type))
        {
            throw new AcademyException("Tipo de chave PIX inválido.");
        }

        var pixKeyType = PixKeyTypeExtensions.FromSlug(type);
        var pattern = pixKeyType switch
        {
            PixKeyType.Celular => @"\A\+[1-9][0-9]{9,14}\z",
            PixKeyType.Email => @"\A[^\s@]+@[^\s@]+\.[^\s@]+\z",
            PixKeyType.Cpf => @"\A[0-9]{11}\z",
            PixKeyType.Cnpj => @"\A[0-9]{14}\z",
            PixKeyType.Aleatoria => @"\A[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\z",
            _ => throw new AcademyException("Tipo de chave PIX inválido."),
        };

        if (key.Length > 254 || !Regex.IsMatch(key, pattern, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)))
        {
            throw new AcademyException("Chave PIX inválida para o tipo informado.");
        }

        return (key, pixKeyType);
    }
}

public class AcademyException : Exception
{
    public AcademyException(string message) : base(message)
    {
    }
}
