using Tatami.Domain.Common;

namespace Tatami.Domain.Entities;

public class Student : BaseEntity
{
    public Guid AcademyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
