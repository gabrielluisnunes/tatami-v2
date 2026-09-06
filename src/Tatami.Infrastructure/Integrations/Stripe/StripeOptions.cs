namespace Tatami.Infrastructure.Integrations.Stripe;

public class StripeOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;

    public int TrialDays { get; set; } = 5;

    public string FrontendBaseUrl { get; set; } = "http://localhost:4200";

    public StripePriceIds PriceIds { get; set; } = new();
}

public class StripePriceIds
{
    public string Starter { get; set; } = "price_1TnTDWJbC64QkQGS8OwQNBJM";

    public string Pro { get; set; } = "price_1TnTDnJbC64QkQGSlGmIdkcC";

    public string MultiUnit { get; set; } = "price_1TkFfOJFm0PQ5umUgHBYJDvM";
}
