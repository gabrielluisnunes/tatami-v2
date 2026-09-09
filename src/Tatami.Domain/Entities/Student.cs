using Tatami.Domain.Common;

namespace Tatami.Domain.Entities;

public class Student : BaseEntity
{
    public Guid AcademyId { get; set; }

    public Guid UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? EmergencyPhone { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? Cep { get; set; }

    public string? Address { get; set; }

    public string? Neighborhood { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public int? PaymentDueDay { get; set; }

    /// <summary>Storage object path (e.g. academyId/userId.jpeg), never a public URL.</summary>
    public string? PhotoUrl { get; set; }

    /// <summary>Face-api descriptor: exactly 128 floats. Sensitive — never expose in list APIs.</summary>
    public double[]? FaceDescriptor { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<StudentSport> Sports { get; set; } = new List<StudentSport>();

    public bool IsProfileComplete =>
        PaymentDueDay is >= 1 and <= 31
        && FaceDescriptor is { Length: 128 }
        && !string.IsNullOrWhiteSpace(PhotoUrl);
}
