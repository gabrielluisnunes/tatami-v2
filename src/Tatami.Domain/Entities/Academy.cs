using Tatami.Domain.Common;
using Tatami.Domain.Enums;

namespace Tatami.Domain.Entities;

public class Academy : BaseEntity
{
    public Guid OwnerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public SportType Sport { get; set; }

    public decimal MonthlyPrice { get; set; }

    public string SubscriptionStatus { get; set; } = Constants.SubscriptionStatus.Trial;
}
