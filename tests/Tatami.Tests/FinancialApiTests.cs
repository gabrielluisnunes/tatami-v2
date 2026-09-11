using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Tatami.Api.Controllers;
using Tatami.Application;
using Tatami.Application.Financials;
using Tatami.Domain.Entities;
using Tatami.Domain.Repositories;

namespace Tatami.Tests;

public class FinancialApiTests
{
    private const string JwtSecret = "test-only-jwt-key-at-least-32-characters-long";
    private const string CronSecret = "test-only-cron-key";

    [Theory]
    [InlineData("/api/financials", null, HttpStatusCode.Unauthorized)]
    [InlineData("/api/financials", "aluno", HttpStatusCode.Forbidden)]
    [InlineData("/api/financials", "professor", HttpStatusCode.Forbidden)]
    [InlineData("/api/students/me/financials", null, HttpStatusCode.Unauthorized)]
    [InlineData("/api/students/me/financials", "admin", HttpStatusCode.Forbidden)]
    [InlineData("/api/students/me/financials", "professor", HttpStatusCode.Forbidden)]
    public async Task RoutesEnforceAuthenticationAndRoles(string route, string? role, HttpStatusCode expected)
    {
        using var server = Server();
        using var client = Client(server, role);
        Assert.Equal(expected, (await client.GetAsync(route)).StatusCode);
    }

    [Theory]
    [InlineData("/api/financials/mark-paid", "aluno")]
    [InlineData("/api/financials/manual-payment", "aluno")]
    [InlineData("/api/students/me/financials/11111111-1111-1111-1111-111111111111/aguardando", "admin")]
    public async Task MutationRoutesEnforceRoles(string route, string role)
    {
        using var server = Server();
        using var client = Client(server, role);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(route, new { })).StatusCode);
    }

    [Theory]
    [InlineData("generate-monthly-charges")]
    [InlineData("update-overdue")]
    public async Task CronAcceptsOnlyItsOwnBearerSecret(string job)
    {
        var repository = new Mock<IFinancialRepository>(MockBehavior.Strict);
        using var server = Server(repository: repository.Object);
        using var client = Client(server, "admin");
        var route = $"/api/cron/{job}";
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(route, null)).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(route, null)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", CronSecret);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(route, null)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "incorrect");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(route, null)).StatusCode);
        repository.VerifyNoOtherCalls();
        repository.Setup(value => value.ListChargeCandidatesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<MonthlyChargeCandidate>());
        repository.Setup(value => value.ListPendingBeforeAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Financial>());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CronSecret);
        var response = await client.PostAsync(route, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, (await response.Content.ReadFromJsonAsync<FinancialJobResult>())!.Processed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MissingCronSecretFailsClosed(string? secret)
    {
        var repository = new Mock<IFinancialRepository>(MockBehavior.Strict);
        using var server = Server(secret, repository.Object);
        using var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CronSecret);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsync("/api/cron/generate-monthly-charges", null)).StatusCode);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InvalidMonthReturnsBadRequestBeforeRepositoryAccess()
    {
        using var server = Server(repository: new Mock<IFinancialRepository>(MockBehavior.Strict).Object);
        using var client = Client(server, "admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/financials?month=2026-13")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/financials/export-overdue?month=invalid")).StatusCode);
    }

    [Fact]
    public async Task CronTokenCannotAccessStudentOrAdminRoutes()
    {
        using var server = Server();
        using var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CronSecret);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/financials")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/students/me/financials")).StatusCode);
    }

    private static TestServer Server(string? secret = CronSecret, IFinancialRepository? repository = null)
    {
        return new TestServer(new WebHostBuilder()
            .ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Cron:Secret"] = secret }))
            .ConfigureServices(services =>
            {
                services.AddApplication();
                services.AddSingleton(repository ?? Mock.Of<IFinancialRepository>());
                services.AddSingleton(Mock.Of<IAcademyRepository>());
                services.AddSingleton(Mock.Of<IStudentRepository>());
                services.AddSingleton(Mock.Of<IFinancialNotificationSender>());
                services.AddSingleton(Mock.Of<IFinancialExportWriter>());
                services.AddSingleton(Mock.Of<IPixBrCodeGenerator>());
                services.AddControllers().AddApplicationPart(typeof(FinancialsController).Assembly);
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true, ValidIssuer = "test", ValidateAudience = true, ValidAudience = "test",
                        ValidateLifetime = true, ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret)), RoleClaimType = "role",
                    };
                });
                services.AddAuthorization();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            }));
    }

    private static HttpClient Client(TestServer server, string? role)
    {
        var client = server.CreateClient();
        if (role is null) return client;
        var token = new JwtSecurityToken("test", "test", new[] { new Claim("sub", Guid.NewGuid().ToString()), new Claim("role", role) },
            expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret)), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }
}