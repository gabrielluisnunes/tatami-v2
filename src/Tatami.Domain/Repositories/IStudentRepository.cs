using Tatami.Domain.Entities;

namespace Tatami.Domain.Repositories;

public interface IStudentRepository
{
    Task<IReadOnlyList<Student>> ListByAcademyAsync(
        Guid academyId,
        string? search,
        bool? active,
        CancellationToken cancellationToken = default);

    Task<Student?> GetByIdAsync(
        Guid id,
        Guid academyId,
        CancellationToken cancellationToken = default);

    Task<Student?> GetByEmailAsync(
        Guid academyId,
        string email,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveByAcademyAsync(
        Guid academyId,
        CancellationToken cancellationToken = default);

    Task<Student> CreateWithSportsAsync(
        Student student,
        IReadOnlyList<StudentSport> sports,
        CancellationToken cancellationToken = default);

    Task<Student> UpdateWithSportsAsync(
        Student student,
        IReadOnlyList<StudentSport> sports,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Student student, CancellationToken cancellationToken = default);
}
