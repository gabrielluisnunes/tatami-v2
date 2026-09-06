namespace Tatami.Application.Billing;

public record CreateCheckoutSessionRequest(string PriceId, Guid AcademyId);

public record StripeSessionResponse(string Url);

public record StripeSubscriptionSnapshot(
    string Id,
    string Status,
    DateTime? TrialEndsAt,
    string? PriceId);

public record ParsedStripeEvent(
    string Type,
    string? AcademyId,
    string? PlanType,
    string? CustomerId,
    string? SubscriptionId,
    string? Status,
    DateTime? TrialEndsAt,
    string? PriceId);

public interface IStripeGateway
{
    int TrialDays { get; }

    string FrontendBaseUrl { get; }

    string? GetPlanKeyByPriceId(string priceId);

    Task<string> EnsureCustomerAsync(
        string email,
        string name,
        Guid academyId,
        string? existingCustomerId,
        CancellationToken cancellationToken = default);

    Task<string> CreateCheckoutSessionUrlAsync(
        string customerId,
        string priceId,
        Guid academyId,
        string planKey,
        CancellationToken cancellationToken = default);

    Task<string> CreatePortalSessionUrlAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    ParsedStripeEvent ConstructEvent(string payload, string signature);

    Task<StripeSubscriptionSnapshot> RetrieveSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default);
}

public interface IStripeBillingService
{
    Task<StripeSessionResponse> CreateCheckoutSessionAsync(
        Guid userId,
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<StripeSessionResponse> CreatePortalSessionAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task HandleWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default);
}

public class BillingException : Exception
{
    public BillingException(string message) : base(message)
    {
    }
}
