using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Tatami.Application.Auth;
using Tatami.Application.Billing;
using Tatami.Application.Storage;
using Tatami.Application.Students;
using Tatami.Domain.Enums;
using Tatami.Domain.Repositories;
using Tatami.Infrastructure.Auth;
using Tatami.Infrastructure.Identity;
using Tatami.Infrastructure.Integrations.Email;
using Tatami.Infrastructure.Integrations.Storage;
using Tatami.Infrastructure.Integrations.Stripe;
using Tatami.Infrastructure.Persistence;
using Tatami.Infrastructure.Persistence.Repositories;

namespace Tatami.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured.");
        }

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();
        services.AddSingleton(dataSource);

        services.AddDbContext<TatamiDbContext>(options =>
            options.UseNpgsql(dataSource));

        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<TatamiDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));
        services.Configure<MinioOptions>(configuration.GetSection(MinioOptions.SectionName));

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration is missing.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Secret) || jwtOptions.Secret.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Secret must be at least 32 characters.");
        }

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    RoleClaimType = "role",
                };
            });

        services.AddAuthorization();

        services.AddScoped<JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAcademyRepository, AcademyRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOnboardingRepository, OnboardingRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IStudentIdentityService, StudentIdentityService>();
        services.AddScoped<IWelcomeEmailSender, StubWelcomeEmailSender>();
        services.AddScoped<IStripeGateway, StripeGateway>();

        RegisterMinio(services, configuration);

        return services;
    }

    private static void RegisterMinio(IServiceCollection services, IConfiguration configuration)
    {
        var minio = configuration.GetSection(MinioOptions.SectionName).Get<MinioOptions>()
            ?? new MinioOptions();

        var dataClient = MinioS3Factory.CreateClient(minio, minio.Endpoint);
        var publicEndpoint = MinioS3Factory.ResolvePublicEndpoint(minio);
        var presignClient = MinioS3Factory.SameEndpoint(minio.Endpoint, publicEndpoint)
            ? dataClient
            : MinioS3Factory.CreateClient(minio, publicEndpoint);

        services.AddSingleton<IAmazonS3>(dataClient);
        services.AddSingleton(
            new MinioPresignClient(presignClient, MinioS3Factory.ProtocolOf(publicEndpoint)));

        services.AddScoped<IObjectStorage, MinioObjectStorage>();
        services.AddScoped<IStudentPhotoStorage, StudentPhotoStorage>();
        services.AddHostedService<MinioBucketBootstrapHostedService>();
    }

    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var role in UserRole.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = role,
                    NormalizedName = role.ToUpperInvariant(),
                });
            }
        }
    }
}
