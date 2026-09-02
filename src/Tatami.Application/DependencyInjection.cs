using Microsoft.Extensions.DependencyInjection;
using Tatami.Application.Academies;

namespace Tatami.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddScoped<IAcademyService, AcademyService>();

        return services;
    }
}
