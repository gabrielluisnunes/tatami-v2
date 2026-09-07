namespace Tatami.Application.Students;

public interface IStudentIdentityService
{
    Task<(Guid UserId, string TemporaryPassword)> CreateAlunoAsync(
        string email,
        string fullName,
        Guid academyId,
        CancellationToken cancellationToken = default);

    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}
