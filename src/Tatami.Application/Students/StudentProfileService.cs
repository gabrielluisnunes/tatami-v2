using System.Text.RegularExpressions;
using Tatami.Application.Storage;
using Tatami.Domain.Repositories;

namespace Tatami.Application.Students;

public interface IStudentProfileService
{
    Task<StudentMeResponse> GetMyProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<StudentMeResponse> CompleteProfileAsync(
        Guid userId,
        CompleteStudentProfileRequest request,
        CancellationToken cancellationToken = default);
}

public class StudentProfileService : IStudentProfileService
{
    private static readonly TimeSpan PhotoUrlTtl = TimeSpan.FromHours(1);
    private static readonly Regex DataUrlRegex = new(
        @"^data:(?<mime>image/(?:jpeg|jpg|png|webp));base64,(?<data>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private const int MaxPhotoBytes = 5 * 1024 * 1024;
    private const int FaceDescriptorLength = 128;

    private readonly IStudentRepository _studentRepository;
    private readonly IStudentPhotoStorage _photoStorage;

    public StudentProfileService(
        IStudentRepository studentRepository,
        IStudentPhotoStorage photoStorage)
    {
        _studentRepository = studentRepository;
        _photoStorage = photoStorage;
    }

    public async Task<StudentMeResponse> GetMyProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var student = await _studentRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new StudentException("Perfil de aluno não encontrado.");

        return await MapMeAsync(student, cancellationToken);
    }

    public async Task<StudentMeResponse> CompleteProfileAsync(
        Guid userId,
        CompleteStudentProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PaymentDueDay is < 1 or > 31)
        {
            throw new StudentException("Dia de vencimento deve ser entre 1 e 31.");
        }

        if (request.FaceDescriptor is null || request.FaceDescriptor.Count != FaceDescriptorLength)
        {
            throw new StudentException(
                $"Descriptor facial deve ter exatamente {FaceDescriptorLength} valores.");
        }

        var (contentType, bytes) = DecodePhoto(request.PhotoBase64);
        var extension = contentType switch
        {
            "image/png" => "png",
            "image/webp" => "webp",
            _ => "jpeg",
        };

        var student = await _studentRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new StudentException("Perfil de aluno não encontrado.");

        if (!student.IsActive)
        {
            throw new StudentException("Aluno inativo.");
        }

        var objectKey = $"{student.AcademyId}/{student.UserId}.{extension}";
        var previousPath = student.PhotoUrl;

        await using var stream = new MemoryStream(bytes);
        await _photoStorage.UploadAsync(objectKey, stream, contentType, cancellationToken);

        student.PhotoUrl = objectKey;
        student.FaceDescriptor = request.FaceDescriptor.ToArray();
        student.PaymentDueDay = request.PaymentDueDay;
        student.UpdatedAt = DateTime.UtcNow;

        await _studentRepository.UpdateAsync(student, cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousPath)
            && !string.Equals(previousPath, objectKey, StringComparison.Ordinal))
        {
            await _photoStorage.DeleteAsync(previousPath, cancellationToken);
        }

        return await MapMeAsync(student, cancellationToken);
    }

    private async Task<StudentMeResponse> MapMeAsync(
        Domain.Entities.Student student,
        CancellationToken cancellationToken)
    {
        var photoUrl = await ResolvePhotoUrlAsync(student.PhotoUrl, cancellationToken);
        return new StudentMeResponse(
            student.Id,
            student.AcademyId,
            student.UserId,
            student.FullName,
            student.Email,
            student.PaymentDueDay,
            photoUrl,
            student.IsProfileComplete);
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

        return await _photoStorage.GetSignedUrlAsync(photoPath, PhotoUrlTtl, cancellationToken);
    }

    private static (string ContentType, byte[] Bytes) DecodePhoto(string photoBase64)
    {
        if (string.IsNullOrWhiteSpace(photoBase64))
        {
            throw new StudentException("Foto é obrigatória.");
        }

        var match = DataUrlRegex.Match(photoBase64.Trim());
        if (!match.Success)
        {
            throw new StudentException(
                "Foto inválida. Use data URL JPEG, PNG ou WebP.");
        }

        var mime = match.Groups["mime"].Value.ToLowerInvariant();
        if (mime == "image/jpg")
        {
            mime = "image/jpeg";
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(match.Groups["data"].Value);
        }
        catch (FormatException)
        {
            throw new StudentException("Foto em base64 inválida.");
        }

        if (bytes.Length == 0 || bytes.Length > MaxPhotoBytes)
        {
            throw new StudentException("Foto deve ter no máximo 5MB.");
        }

        return (mime, bytes);
    }
}
