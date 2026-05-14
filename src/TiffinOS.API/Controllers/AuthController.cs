using Microsoft.AspNetCore.Mvc;
using TiffinOS.API.Services;
using TiffinOS.API.Services.Interfaces;

namespace TiffinOS.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly TenantContext _tenant;

    public AuthController(IAuthService auth, TenantContext tenant)
    {
        _auth = auth;
        _tenant = tenant;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _auth.RegisterAsync(request);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            accessToken = result.AccessToken!,
            refreshToken = result.RefreshToken!,
            expiresAt = result.ExpiresAt!.Value.ToString("o")
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _auth.LoginAsync(request, _tenant.TenantSlug);

        if (!result.Success)
            return Unauthorized(new { error = result.Error });

        return Ok(new
        {
            accessToken = result.AccessToken!,
            refreshToken = result.RefreshToken!,
            expiresAt = result.ExpiresAt!.Value.ToString("o")
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await _auth.RefreshTokenAsync(request.RefreshToken);

        if (!result.Success)
            return Unauthorized(new { error = result.Error });

        return Ok(new
        {
            accessToken = result.AccessToken!,
            refreshToken = result.RefreshToken!,
            expiresAt = result.ExpiresAt!.Value.ToString("o")
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
    {
        await _auth.RevokeTokenAsync(request.RefreshToken);
        return Ok(new { message = "Logged out successfully." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request)
    {
        await _auth.InitiatePasswordResetAsync(
            request.Email, _tenant.TenantSlug);

        // Always return success — never reveal if email exists
        return Ok(new
        {
            message = "If an account exists, a reset link has been sent."
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request)
    {
        var success = await _auth.ResetPasswordAsync(
            request.Token, request.NewPassword);

        if (!success)
            return BadRequest(new
            {
                error = "Invalid or expired reset token."
            });

        return Ok(new { message = "Password reset successfully." });
    }
}

// ── Request Records ───────────────────────────────────────────
public record RefreshRequest(string RefreshToken);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);