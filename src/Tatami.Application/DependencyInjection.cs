using Microsoft.Extensions.DependencyInjection;
using Tatami.Application.Academies;
using Tatami.Application.Billing;

namespace Tatami.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddScoped<IAcademyService, AcademyService>();
        services.AddScoped<IStripeBillingService, StripeBillingService>();

        return services;
    }
}
