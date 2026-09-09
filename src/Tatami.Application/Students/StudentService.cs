using Tatami.Application.Storage;
using Tatami.Domain.Constants;
using Tatami.Domain.Entities;
using Tatami.Domain.Enums;
using Tatami.Domain.Repositories;

namespace Tatami.Application.Students;

public class StudentService : IStudentService
{
    private static readonly TimeSpan PhotoUrlTtl = TimeSpan.FromHours(1);

    private readonly IUserRepository _userRepository;
    private readonly IAcademyRepository _academyRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IStudentIdentityService _identityService;
    private readonly IWelcomeEmailSender _welcomeEmailSender;
    private readonly IStudentPhotoStorage _photoStorage;

    public StudentService(
        IUserRepository userRepository,
        IAcademyRepository academyRepository,
        IStudentRepository studentRepository,
        IStudentIdentityService identityService,
        IWelcomeEmailSender welcomeEmailSender,
        IStudentPhotoStorage photoStorage)
    {
        _userRepository = userRepository;
        _academyRepository = academyRepository;
        _studentRepository = studentRepository;
        _identityService = identityService;
        _welcomeEmailSender = welcomeEmailSender;
        _photoStorage = photoStorage;
    }

    public async Task<IReadOnlyList<StudentResponse>> ListAsync(
        Guid adminUserId,
        string? search,
        bool? active,
        CancellationToken cancellationToken = default)
    {
        var academyId = await RequireAdminAcademyIdAsync(adminUserId, cancellationToken);
        var students = await _studentRepository.ListByAcademyAsync(
            academyId,
            search,
            active,
            cancellationToken);

        var result = new List<StudentResponse>(students.Count);
        foreach (var student in students)
        {
            result.Add(await MapStudentAsync(student, cancellationToken));
        }

        return result;
    }

    public async Task<StudentResponse> GetByIdAsync(
        Guid adminUserId,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        var academyId = await RequireAdminAcademyIdAsync(adminUserId, cancellationToken);
        var student = await _studentRepository.GetByIdAsync(studentId, academyId, cancellationToken)
            ?? throw new StudentException("Aluno não encontrado.");

        return await MapStudentAsync(student, cancellationToken);
    }

    public async Task<EnrollStudentResponse> EnrollAsync(
        Guid adminUserId,
        EnrollStudentRequest request,
        CancellationToken cancellationToken = default)
    {
        var academyId = await RequireAdminAcademyIdAsync(adminUserId, cancellationToken);
        var academy = await _academyRepository.GetByIdAsync(academyId, cancellationToken)
            ?? throw new StudentException("Academia não encontrada.");

        var fullName = ValidateFullName(request.FullName);
        var email = ValidateEmail(request.Email);
        var sports = BuildSports(academyId, request.Sports);

        if (string.Equals(academy.Plan, SaaSPlans.Starter, StringComparison.OrdinalIgnoreCase))
        {
            var activeCount = await _studentRepository.CountActiveByAcademyAsync(academyId, cancellationToken);
            if (activeCount >= SaaSPlans.StarterMaxActiveStudents)
            {
                throw new StudentException(
                    $"Plano Starter permite até {SaaSPlans.StarterMaxActiveStudents} alunos ativos.",
                    "PLAN_LIMIT_REACHED");
            }
        }

        if (await _identityService.EmailExistsAsync(email, cancellationToken))
        {
            throw new StudentException("E-mail já cadastrado.", "EMAIL_ALREADY_REGISTERED");
        }

        var existingStudent = await _studentRepository.GetByEmailAsync(academyId, email, cancellationToken);
        if (existingStudent is not null && existingStudent.IsActive)
        {
            throw new StudentException("E-mail já cadastrado nesta academia.", "EMAIL_ALREADY_REGISTERED");
        }

        Guid? createdUserId = null;
        try
        {
            var (userId, temporaryPassword) = await _identityService.CreateAlunoAsync(
                email,
                fullName,
                academyId,
                cancellationToken);
            createdUserId = userId;

            var now = DateTime.UtcNow;
            var student = new Student
            {
                Id = Guid.NewGuid(),
                AcademyId = academyId,
                UserId = userId,
                FullName = fullName,
                Email = email,
                Phone = TrimOrNull(request.Phone),
                EmergencyPhone = TrimOrNull(request.EmergencyPhone),
                BirthDate = request.BirthDate,
                Cep = TrimOrNull(request.Cep),
                Address = TrimOrNull(request.Address),
                Neighborhood = TrimOrNull(request.Neighborhood),
                City = TrimOrNull(request.City),
                State = TrimOrNull(request.State)?.ToUpperInvariant(),
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            };

            foreach (var sport in sports)
            {
                sport.StudentId = student.Id;
            }

            student = await _studentRepository.CreateWithSportsAsync(student, sports, cancellationToken);

            var emailSent = await _welcomeEmailSender.SendAsync(
                email,
                fullName,
                temporaryPassword,
                cancellationToken);

            return new EnrollStudentResponse(
                await MapStudentAsync(student, cancellationToken),
                emailSent,
                emailSent ? null : temporaryPassword);
        }
        catch
        {
            if (createdUserId.HasValue)
            {
                await _identityService.DeleteUserAsync(createdUserId.Value, cancellationToken);
            }

            throw;
        }
    }

    public async Task<StudentResponse> UpdateAsync(
        Guid adminUserId,
        Guid studentId,
        UpdateStudentRequest request,
        CancellationToken cancellationToken = default)
    {
        var academyId = await RequireAdminAcademyIdAsync(adminUserId, cancellationToken);
        var student = await _studentRepository.GetByIdAsync(studentId, academyId, cancellationToken)
            ?? throw new StudentException("Aluno não encontrado.");

        var sports = BuildSports(academyId, request.Sports);
        foreach (var sport in sports)
        {
            sport.StudentId = student.Id;
        }

        student.FullName = ValidateFullName(request.FullName);
        student.Phone = TrimOrNull(request.Phone);
        student.EmergencyPhone = TrimOrNull(request.EmergencyPhone);
        student.BirthDate = request.BirthDate;
        student.Cep = TrimOrNull(request.Cep);
        student.Address = TrimOrNull(request.Address);
        student.Neighborhood = TrimOrNull(request.Neighborhood);
        student.City = TrimOrNull(request.City);
        student.State = TrimOrNull(request.State)?.ToUpperInvariant();
        student.UpdatedAt = DateTime.UtcNow;

        student = await _studentRepository.UpdateWithSportsAsync(student, sports, cancellationToken);
        return await MapStudentAsync(student, cancellationToken);
    }

    public async Task<StudentResponse> DeactivateAsync(
        Guid adminUserId,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        var academyId = await RequireAdminAcademyIdAsync(adminUserId, cancellationToken);
        var student = await _studentRepository.GetByIdAsync(studentId, academyId, cancellationToken)
            ?? throw new StudentException("Aluno não encontrado.");

        student.IsActive = false;
        student.UpdatedAt = DateTime.UtcNow;
        await _studentRepository.UpdateAsync(student, cancellationToken);
        return await MapStudentAsync(student, cancellationToken);
    }

    public async Task<StudentResponse> ActivateAsync(
        Guid adminUserId,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        var academyId = await RequireAdminAcademyIdAsync(adminUserId, cancellationToken);
        var student = await _studentRepository.GetByIdAsync(studentId, academyId, cancellationToken)
            ?? throw new StudentException("Aluno não encontrado.");

        var conflict = await _studentRepository.GetByEmailAsync(academyId, student.Email, cancellationToken);
        if (conflict is not null && conflict.IsActive && conflict.Id != student.Id)
        {
            throw new StudentException(
                "Já existe um aluno ativo com este e-mail nesta academia.",
                "EMAIL_ALREADY_REGISTERED");
        }

        if (string.Equals(
                (await _academyRepository.GetByIdAsync(academyId, cancellationToken))?.Plan,
                SaaSPlans.Starter,
                StringComparison.OrdinalIgnoreCase))
        {
            var activeCount = await _studentRepository.CountActiveByAcademyAsync(academyId, cancellationToken);
            if (!student.IsActive && activeCount >= SaaSPlans.StarterMaxActiveStudents)
            {
                throw new StudentException(
                    $"Plano Starter permite até {SaaSPlans.StarterMaxActiveStudents} alunos ativos.",
                    "PLAN_LIMIT_REACHED");
            }
        }

        student.IsActive = true;
        student.UpdatedAt = DateTime.UtcNow;
        await _studentRepository.UpdateAsync(student, cancellationToken);
        return await MapStudentAsync(student, cancellationToken);
    }

    private async Task<Guid> RequireAdminAcademyIdAsync(
        Guid adminUserId,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(adminUserId, cancellationToken)
            ?? throw new StudentException("Usuário não encontrado.");

        if (!user.Roles.Contains(UserRole.Admin))
        {
            throw new StudentException("Acesso negado.");
        }

        if (!user.AcademyId.HasValue)
        {
            throw new StudentException("Academia não configurada.");
        }

        return user.AcademyId.Value;
    }

    private static string ValidateFullName(string fullName)
    {
        var value = fullName?.Trim() ?? string.Empty;
        if (value.Length < 2)
        {
            throw new StudentException("Nome deve ter pelo menos 2 caracteres.");
        }

        return value;
    }

    private static string ValidateEmail(string email)
    {
        var value = email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
        {
            throw new StudentException("E-mail inválido.");
        }

        return value;
    }

    private static IReadOnlyList<StudentSport> BuildSports(
        Guid academyId,
        IReadOnlyList<StudentSportInput> inputs)
    {
        if (inputs is null || inputs.Count == 0)
        {
            throw new StudentException("Informe pelo menos um esporte.");
        }

        var now = DateTime.UtcNow;
        var sports = new List<StudentSport>();
        var seen = new HashSet<SportType>();

        foreach (var input in inputs)
        {
            SportType sportType;
            try
            {
                sportType = SportTypeExtensions.FromSlug(input.Sport);
            }
            catch (ArgumentException)
            {
                throw new StudentException($"Esporte inválido: {input.Sport}");
            }

            if (!sportType.IsStudentSport())
            {
                throw new StudentException("Esporte inválido para aluno.");
            }

            if (!seen.Add(sportType))
            {
                throw new StudentException("Não é permitido repetir o mesmo esporte.");
            }

            var degree = Math.Clamp(input.Degree, 0, 4);
            string? belt = TrimOrNull(input.Belt);

            if (sportType == SportType.Boxe)
            {
                belt = null;
                degree = 0;
            }

            sports.Add(new StudentSport
            {
                Id = Guid.NewGuid(),
                AcademyId = academyId,
                Sport = sportType,
                Belt = belt,
                Degree = degree,
                BeltUpdatedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        return sports;
    }

    private static string? TrimOrNull(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private async Task<StudentResponse> MapStudentAsync(
        Student student,
        CancellationToken cancellationToken)
    {
        var photoUrl = await ResolvePhotoUrlAsync(student.PhotoUrl, cancellationToken);
        return new StudentResponse(
            student.Id,
            student.AcademyId,
            student.UserId,
            student.FullName,
            student.Email,
            student.Phone,
            student.EmergencyPhone,
            student.BirthDate,
            student.Cep,
            student.Address,
            student.Neighborhood,
            student.City,
            student.State,
            student.PaymentDueDay,
            photoUrl,
            student.IsActive,
            student.IsProfileComplete,
            student.Sports
                .OrderBy(sport => sport.Sport.ToSlug())
                .Select(sport => new StudentSportResponse(
                    sport.Sport.ToSlug(),
                    sport.Belt,
                    sport.Degree,
                    sport.BeltUpdatedAt))
                .ToList(),
            student.CreatedAt);
    }

    private async Task<string?> ResolvePhotoUrlAsync(
        string? photoPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(photoPath))
        {
            return null;
        }

        if (photoPath.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            || photoPath.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return photoPath;
        }

        try
        {
            return await _photoStorage.GetSignedUrlAsync(photoPath, PhotoUrlTtl, cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}
