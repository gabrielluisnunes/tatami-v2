namespace Tatami.Application.Students;

public record StudentSportInput(string Sport, string? Belt, int Degree);

public record EnrollStudentRequest(
    string FullName,
    string Email,
    string? Phone,
    string? EmergencyPhone,
    DateOnly? BirthDate,
    string? Cep,
    string? Address,
    string? Neighborhood,
    string? City,
    string? State,
    IReadOnlyList<StudentSportInput> Sports);

public record UpdateStudentRequest(
    string FullName,
    string? Phone,
    string? EmergencyPhone,
    DateOnly? BirthDate,
    string? Cep,
    string? Address,
    string? Neighborhood,
    string? City,
    string? State,
    IReadOnlyList<StudentSportInput> Sports);

public record StudentSportResponse(
    string Sport,
    string? Belt,
    int Degree,
    DateTime BeltUpdatedAt);

public record StudentResponse(
    Guid Id,
    Guid AcademyId,
    Guid UserId,
    string FullName,
    string Email,
    string? Phone,
    string? EmergencyPhone,
    DateOnly? BirthDate,
    string? Cep,
    string? Address,
    string? Neighborhood,
    string? City,
    string? State,
    int? PaymentDueDay,
    string? PhotoUrl,
    bool IsActive,
    IReadOnlyList<StudentSportResponse> Sports,
    DateTime CreatedAt);

public record EnrollStudentResponse(
    StudentResponse Student,
    bool EmailSent,
    string? TemporaryPassword);

public class StudentException : Exception
{
    public string? Code { get; }

    public StudentException(string message, string? code = null) : base(message)
    {
        Code = code;
    }
}
