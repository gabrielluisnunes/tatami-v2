using Microsoft.Extensions.DependencyInjection;
using Tatami.Application.Academies;
using Tatami.Application.Billing;
using Tatami.Application.Financials;
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
        services.AddScoped<IStudentProfileService, StudentProfileService>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<FinancialService>();
        services.AddScoped<StudentFinancialService>();
        services.AddScoped<FinancialJobs>();

        return services;
    }
}
