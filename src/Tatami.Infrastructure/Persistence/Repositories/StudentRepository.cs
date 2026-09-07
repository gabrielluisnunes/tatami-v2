using Microsoft.EntityFrameworkCore;
using Tatami.Domain.Entities;
using Tatami.Domain.Repositories;

namespace Tatami.Infrastructure.Persistence.Repositories;

public class StudentRepository : IStudentRepository
{
    private readonly TatamiDbContext _dbContext;

    public StudentRepository(TatamiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Student>> ListByAcademyAsync(
        Guid academyId,
        string? search,
        bool? active,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Students
            .AsNoTracking()
            .Include(student => student.Sports)
            .Where(student => student.AcademyId == academyId);

        if (active.HasValue)
        {
            query = query.Where(student => student.IsActive == active.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(student =>
                student.FullName.ToLower().Contains(term)
                || student.Email.ToLower().Contains(term));
        }

        return await query
            .OrderBy(student => student.FullName)
            .ToListAsync(cancellationToken);
    }

    public Task<Student?> GetByIdAsync(
        Guid id,
        Guid academyId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Students
            .Include(student => student.Sports)
            .FirstOrDefaultAsync(
                student => student.Id == id && student.AcademyId == academyId,
                cancellationToken);

    public Task<Student?> GetByEmailAsync(
        Guid academyId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _dbContext.Students
            .Include(student => student.Sports)
            .FirstOrDefaultAsync(
                student => student.AcademyId == academyId
                    && student.Email.ToLower() == normalized,
                cancellationToken);
    }

    public Task<int> CountActiveByAcademyAsync(
        Guid academyId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Students.CountAsync(
            student => student.AcademyId == academyId && student.IsActive,
            cancellationToken);

    public async Task<Student> CreateWithSportsAsync(
        Student student,
        IReadOnlyList<StudentSport> sports,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _dbContext.Students.Add(student);
            _dbContext.StudentSports.AddRange(sports);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return student;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Student> UpdateWithSportsAsync(
        Student student,
        IReadOnlyList<StudentSport> sports,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var existingSports = await _dbContext.StudentSports
                .Where(sport => sport.StudentId == student.Id)
                .ToListAsync(cancellationToken);

            _dbContext.StudentSports.RemoveRange(existingSports);
            _dbContext.StudentSports.AddRange(sports);
            _dbContext.Students.Update(student);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            student.Sports = sports.ToList();
            return student;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdateAsync(Student student, CancellationToken cancellationToken = default)
    {
        _dbContext.Students.Update(student);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
