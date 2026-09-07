using Tatami.Domain.Common;
using Tatami.Domain.Enums;

namespace Tatami.Domain.Entities;

public class StudentSport : BaseEntity
{
    public Guid StudentId { get; set; }

    public Guid AcademyId { get; set; }

    public SportType Sport { get; set; }

    public string? Belt { get; set; }

    public int Degree { get; set; }

    public DateTime BeltUpdatedAt { get; set; }

    public Student Student { get; set; } = null!;
}
