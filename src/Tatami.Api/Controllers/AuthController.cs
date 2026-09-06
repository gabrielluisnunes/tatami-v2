using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tatami.Api.Extensions;
using Tatami.Application.Auth;
using Tatami.Infrastructure.Auth;

namespace Tatami.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RegisterAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (AuthException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LoginAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (AuthException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RefreshAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (AuthException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPatch("profile")]
    public async Task<IActionResult> UpdateProfile(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();
            var response = await _authService.UpdateProfileAsync(userId, request, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Usuário não autenticado." });
        }
        catch (AuthException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();
            await _authService.ChangePasswordAsync(userId, request, cancellationToken);
            return Ok(new { message = "Senha alterada com sucesso." });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Usuário não autenticado." });
        }
        catch (AuthException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
