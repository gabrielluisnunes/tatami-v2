namespace Tatami.Application.Students;

public interface IStudentService
{
    Task<IReadOnlyList<StudentResponse>> ListAsync(
        Guid adminUserId,
        string? search,
        bool? active,
        CancellationToken cancellationToken = default);

    Task<StudentResponse> GetByIdAsync(
        Guid adminUserId,
        Guid studentId,
        CancellationToken cancellationToken = default);

    Task<EnrollStudentResponse> EnrollAsync(
        Guid adminUserId,
        EnrollStudentRequest request,
        CancellationToken cancellationToken = default);

    Task<StudentResponse> UpdateAsync(
        Guid adminUserId,
        Guid studentId,
        UpdateStudentRequest request,
        CancellationToken cancellationToken = default);

    Task<StudentResponse> DeactivateAsync(
        Guid adminUserId,
        Guid studentId,
        CancellationToken cancellationToken = default);

    Task<StudentResponse> ActivateAsync(
        Guid adminUserId,
        Guid studentId,
        CancellationToken cancellationToken = default);
}
