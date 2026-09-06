using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using Tatami.Application.Billing;
using Tatami.Domain.Constants;

namespace Tatami.Infrastructure.Integrations.Stripe;

public class StripeGateway : IStripeGateway
{
    private readonly StripeOptions _options;
    private readonly Dictionary<string, string> _priceIdToPlanKey;

    public StripeGateway(IOptions<StripeOptions> options)
    {
        _options = options.Value;

        if (!string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            StripeConfiguration.ApiKey = _options.SecretKey;
        }

        _priceIdToPlanKey = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [_options.PriceIds.Starter] = SaaSPlans.Starter,
            [_options.PriceIds.Pro] = SaaSPlans.Pro,
            [_options.PriceIds.MultiUnit] = SaaSPlans.MultiUnit,
        };
    }

    private void EnsureApiKey()
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            throw new BillingException("Stripe:SecretKey is not configured.");
        }

        StripeConfiguration.ApiKey = _options.SecretKey;
    }

    public int TrialDays => _options.TrialDays;

    public string FrontendBaseUrl => _options.FrontendBaseUrl.TrimEnd('/');

    public string? GetPlanKeyByPriceId(string priceId) =>
        _priceIdToPlanKey.TryGetValue(priceId, out var planKey) ? planKey : null;

    public async Task<string> EnsureCustomerAsync(
        string email,
        string name,
        Guid academyId,
        string? existingCustomerId,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(existingCustomerId))
        {
            return existingCustomerId;
        }

        EnsureApiKey();
        var customerService = new CustomerService();
        var customer = await customerService.CreateAsync(
            new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Metadata = new Dictionary<string, string>
                {
                    ["academy_id"] = academyId.ToString(),
                },
            },
            cancellationToken: cancellationToken);

        return customer.Id;
    }

    public async Task<string> CreateCheckoutSessionUrlAsync(
        string customerId,
        string priceId,
        Guid academyId,
        string planKey,
        CancellationToken cancellationToken = default)
    {
        EnsureApiKey();
        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(
            new SessionCreateOptions
            {
                Mode = "subscription",
                Customer = customerId,
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1,
                    },
                ],
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    TrialPeriodDays = TrialDays,
                },
                Metadata = new Dictionary<string, string>
                {
                    ["academy_id"] = academyId.ToString(),
                    ["plan_type"] = planKey,
                },
                SuccessUrl = $"{FrontendBaseUrl}/dashboard",
                CancelUrl = $"{FrontendBaseUrl}/dashboard/assinatura",
            },
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(session.Url))
        {
            throw new InvalidOperationException("Stripe Checkout session URL was empty.");
        }

        return session.Url;
    }

    public async Task<string> CreatePortalSessionUrlAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        EnsureApiKey();
        var portalService = new global::Stripe.BillingPortal.SessionService();
        var session = await portalService.CreateAsync(
            new global::Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = customerId,
                ReturnUrl = $"{FrontendBaseUrl}/dashboard/assinatura",
            },
            cancellationToken: cancellationToken);

        return session.Url;
    }

    public ParsedStripeEvent ConstructEvent(string payload, string signature)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            throw new BillingException("Stripe webhook secret is not configured.");
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signature, _options.WebhookSecret);
        }
        catch (Exception ex)
        {
            throw new BillingException($"Invalid Stripe webhook signature: {ex.Message}");
        }

        return stripeEvent.Type switch
        {
            "checkout.session.completed" => ParseCheckoutCompleted(stripeEvent),
            "customer.subscription.updated" => ParseSubscription(stripeEvent),
            "customer.subscription.deleted" => ParseSubscription(stripeEvent),
            "invoice.payment_succeeded" => ParseInvoice(stripeEvent),
            "invoice.payment_failed" => ParseInvoice(stripeEvent),
            _ => new ParsedStripeEvent(stripeEvent.Type, null, null, null, null, null, null, null),
        };
    }

    public async Task<StripeSubscriptionSnapshot> RetrieveSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        EnsureApiKey();
        var subscriptionService = new SubscriptionService();
        var subscription = await subscriptionService.GetAsync(
            subscriptionId,
            cancellationToken: cancellationToken);

        return new StripeSubscriptionSnapshot(
            subscription.Id,
            subscription.Status,
            ToUtc(subscription.TrialEnd),
            subscription.Items.Data.FirstOrDefault()?.Price?.Id);
    }

    private static ParsedStripeEvent ParseCheckoutCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Session
            ?? throw new BillingException("Invalid checkout.session.completed payload.");

        session.Metadata.TryGetValue("academy_id", out var academyId);
        session.Metadata.TryGetValue("plan_type", out var planType);

        return new ParsedStripeEvent(
            stripeEvent.Type,
            academyId,
            planType,
            session.CustomerId,
            session.SubscriptionId,
            null,
            null,
            null);
    }

    private static ParsedStripeEvent ParseSubscription(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription
            ?? throw new BillingException("Invalid subscription webhook payload.");

        return new ParsedStripeEvent(
            stripeEvent.Type,
            null,
            null,
            subscription.CustomerId,
            subscription.Id,
            subscription.Status,
            ToUtc(subscription.TrialEnd),
            subscription.Items.Data.FirstOrDefault()?.Price?.Id);
    }

    private static ParsedStripeEvent ParseInvoice(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice
            ?? throw new BillingException("Invalid invoice webhook payload.");

        var subscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;

        return new ParsedStripeEvent(
            stripeEvent.Type,
            null,
            null,
            invoice.CustomerId,
            subscriptionId,
            null,
            null,
            null);
    }

    private static DateTime? ToUtc(DateTime? value) =>
        value.HasValue
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : null;
}
