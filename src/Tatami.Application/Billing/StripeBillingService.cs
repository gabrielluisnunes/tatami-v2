using Tatami.Domain.Constants;
using Tatami.Domain.Enums;
using Tatami.Domain.Repositories;

namespace Tatami.Application.Billing;

public class StripeBillingService : IStripeBillingService
{
    private readonly IUserRepository _userRepository;
    private readonly IAcademyRepository _academyRepository;
    private readonly IStripeGateway _stripeGateway;

    public StripeBillingService(
        IUserRepository userRepository,
        IAcademyRepository academyRepository,
        IStripeGateway stripeGateway)
    {
        _userRepository = userRepository;
        _academyRepository = academyRepository;
        _stripeGateway = stripeGateway;
    }

    public async Task<StripeSessionResponse> CreateCheckoutSessionAsync(
        Guid userId,
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new BillingException("Usuário não encontrado.");

        if (!user.Roles.Contains(UserRole.Admin))
        {
            throw new BillingException("Acesso negado.");
        }

        if (!user.AcademyId.HasValue || user.AcademyId.Value != request.AcademyId)
        {
            throw new BillingException("Academia não pertence ao usuário.");
        }

        var planKey = _stripeGateway.GetPlanKeyByPriceId(request.PriceId)
            ?? throw new BillingException("Plano inválido.");

        var academy = await _academyRepository.GetByIdAsync(request.AcademyId, cancellationToken)
            ?? throw new BillingException("Academia não encontrada.");

        var customerId = await _stripeGateway.EnsureCustomerAsync(
            user.Email,
            academy.Name,
            academy.Id,
            academy.StripeCustomerId,
            cancellationToken);

        if (!string.Equals(academy.StripeCustomerId, customerId, StringComparison.Ordinal))
        {
            academy.StripeCustomerId = customerId;
            academy.UpdatedAt = DateTime.UtcNow;
            await _academyRepository.UpdateAsync(academy, cancellationToken);
        }

        var url = await _stripeGateway.CreateCheckoutSessionUrlAsync(
            customerId,
            request.PriceId,
            academy.Id,
            planKey,
            cancellationToken);

        return new StripeSessionResponse(url);
    }

    public async Task<StripeSessionResponse> CreatePortalSessionAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new BillingException("Usuário não encontrado.");

        if (!user.Roles.Contains(UserRole.Admin) || !user.AcademyId.HasValue)
        {
            throw new BillingException("Acesso negado.");
        }

        var academy = await _academyRepository.GetByIdAsync(user.AcademyId.Value, cancellationToken)
            ?? throw new BillingException("Academia não encontrada.");

        if (string.IsNullOrWhiteSpace(academy.StripeCustomerId))
        {
            throw new BillingException("Você ainda não possui uma assinatura vinculada.");
        }

        var url = await _stripeGateway.CreatePortalSessionUrlAsync(
            academy.StripeCustomerId,
            cancellationToken);

        return new StripeSessionResponse(url);
    }

    public async Task HandleWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default)
    {
        var stripeEvent = _stripeGateway.ConstructEvent(payload, signature);

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                await HandleCheckoutCompletedAsync(stripeEvent, cancellationToken);
                break;
            case "customer.subscription.updated":
                await HandleSubscriptionUpdatedAsync(stripeEvent, cancellationToken);
                break;
            case "customer.subscription.deleted":
                await HandleSubscriptionDeletedAsync(stripeEvent, cancellationToken);
                break;
            case "invoice.payment_succeeded":
                await HandleInvoicePaymentSucceededAsync(stripeEvent, cancellationToken);
                break;
            case "invoice.payment_failed":
                await HandleInvoicePaymentFailedAsync(stripeEvent, cancellationToken);
                break;
        }
    }

    private async Task HandleCheckoutCompletedAsync(
        ParsedStripeEvent stripeEvent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(stripeEvent.AcademyId)
            || string.IsNullOrWhiteSpace(stripeEvent.SubscriptionId)
            || !Guid.TryParse(stripeEvent.AcademyId, out var academyId))
        {
            return;
        }

        var academy = await _academyRepository.GetByIdAsync(academyId, cancellationToken);
        if (academy is null)
        {
            return;
        }

        var subscription = await _stripeGateway.RetrieveSubscriptionAsync(
            stripeEvent.SubscriptionId,
            cancellationToken);

        academy.StripeCustomerId = stripeEvent.CustomerId ?? academy.StripeCustomerId;
        academy.StripeSubscriptionId = subscription.Id;
        academy.SubscriptionStatus = subscription.Status;
        academy.Plan = stripeEvent.PlanType ?? academy.Plan;
        academy.TrialEndsAt = subscription.TrialEndsAt;
        academy.UpdatedAt = DateTime.UtcNow;

        await _academyRepository.UpdateAsync(academy, cancellationToken);
    }

    private async Task HandleSubscriptionUpdatedAsync(
        ParsedStripeEvent stripeEvent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(stripeEvent.SubscriptionId))
        {
            return;
        }

        var academy = await _academyRepository.GetByStripeSubscriptionIdAsync(
            stripeEvent.SubscriptionId,
            cancellationToken);
        if (academy is null)
        {
            return;
        }

        academy.SubscriptionStatus = stripeEvent.Status ?? academy.SubscriptionStatus;
        academy.TrialEndsAt = stripeEvent.TrialEndsAt;

        if (!string.IsNullOrWhiteSpace(stripeEvent.PriceId))
        {
            var planKey = _stripeGateway.GetPlanKeyByPriceId(stripeEvent.PriceId);
            if (planKey is not null)
            {
                academy.Plan = planKey;
            }
        }

        academy.UpdatedAt = DateTime.UtcNow;
        await _academyRepository.UpdateAsync(academy, cancellationToken);
    }

    private async Task HandleSubscriptionDeletedAsync(
        ParsedStripeEvent stripeEvent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(stripeEvent.SubscriptionId))
        {
            return;
        }

        var academy = await _academyRepository.GetByStripeSubscriptionIdAsync(
            stripeEvent.SubscriptionId,
            cancellationToken);
        if (academy is null)
        {
            return;
        }

        academy.SubscriptionStatus = SubscriptionStatus.Canceled;
        academy.StripeSubscriptionId = null;
        academy.Plan = null;
        academy.TrialEndsAt = null;
        academy.UpdatedAt = DateTime.UtcNow;

        await _academyRepository.UpdateAsync(academy, cancellationToken);
    }

    private async Task HandleInvoicePaymentSucceededAsync(
        ParsedStripeEvent stripeEvent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(stripeEvent.SubscriptionId))
        {
            return;
        }

        var academy = await _academyRepository.GetByStripeSubscriptionIdAsync(
            stripeEvent.SubscriptionId,
            cancellationToken);
        if (academy is null)
        {
            return;
        }

        academy.SubscriptionStatus = SubscriptionStatus.Active;
        academy.UpdatedAt = DateTime.UtcNow;
        await _academyRepository.UpdateAsync(academy, cancellationToken);
    }

    private async Task HandleInvoicePaymentFailedAsync(
        ParsedStripeEvent stripeEvent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(stripeEvent.SubscriptionId))
        {
            return;
        }

        var academy = await _academyRepository.GetByStripeSubscriptionIdAsync(
            stripeEvent.SubscriptionId,
            cancellationToken);
        if (academy is null)
        {
            return;
        }

        academy.SubscriptionStatus = SubscriptionStatus.PastDue;
        academy.UpdatedAt = DateTime.UtcNow;
        await _academyRepository.UpdateAsync(academy, cancellationToken);
    }
}
