using Tatami.Domain.Common;
using Tatami.Domain.Enums;

namespace Tatami.Domain.Entities;

public class Financial : BaseEntity
{
    public Guid StudentId { get; set; }
    public Guid AcademyId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly ReferenceMonth { get; set; }
    public FinancialStatus Status { get; set; } = FinancialStatus.Pending;
    public DateTime? PaidAt { get; set; }
}