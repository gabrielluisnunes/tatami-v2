using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Tatami.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(ClaimTypes.Name)
            ?? user.FindFirstValue("sub");

        if (value is null || !Guid.TryParse(value, out var userId))
        {
            throw new UnauthorizedAccessException("Usuário não autenticado.");
        }

        return userId;
    }
}
