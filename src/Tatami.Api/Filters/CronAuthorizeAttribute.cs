using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Tatami.Api.Filters;

public sealed class CronAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var secret = configuration["Cron:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            context.Result = new ObjectResult(new { error = "Cron não configurado." }) { StatusCode = StatusCodes.Status503ServiceUnavailable };
            return;
        }
        var header = context.HttpContext.Request.Headers.Authorization.ToString();
        if (header.Length > 4096 || !AuthenticationHeaderValue.TryParse(header, out var authorization)
            || !string.Equals(authorization.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(authorization.Parameter)
            || !CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(secret)),
                SHA256.HashData(Encoding.UTF8.GetBytes(authorization.Parameter))))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Credencial de cron inválida." });
        }
    }
}