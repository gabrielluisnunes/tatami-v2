namespace Tatami.Domain.Constants;

public static class SaaSPlans
{
    public const string Starter = "starter";
    public const string Pro = "pro";
    public const string MultiUnit = "multi-unit";

    public static readonly IReadOnlyList<string> All =
    [
        Starter,
        Pro,
        MultiUnit,
    ];

    public static readonly IReadOnlyList<string> Visible =
    [
        Starter,
        Pro,
    ];

    public static string DisplayName(string planKey) => planKey switch
    {
        Starter => "Starter",
        Pro => "Pro",
        MultiUnit => "Multi-unit",
        _ => planKey,
    };

    public static decimal DisplayPrice(string planKey) => planKey switch
    {
        Starter => 79m,
        Pro => 175m,
        MultiUnit => 299m,
        _ => 0m,
    };
}
