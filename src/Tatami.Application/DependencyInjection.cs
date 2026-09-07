using Microsoft.Extensions.DependencyInjection;
using Tatami.Application.Academies;
using Tatami.Application.Billing;
using Tatami.Application.Students;

namespace Tatami.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddScoped<IAcademyService, AcademyService>();
        services.AddScoped<IStripeBillingService, StripeBillingService>();
        services.AddScoped<IStudentService, StudentService>();

        return services;
    }
}
