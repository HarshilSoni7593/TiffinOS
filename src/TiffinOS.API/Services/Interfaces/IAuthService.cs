using Microsoft.AspNetCore.Identity.Data;

namespace TiffinOS.API.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(RegisterRequest request);
        Task<AuthResult> LoginAsync(LoginRequest request, string tenantSlug);
        Task<AuthResult> RefreshTokenAsync(string refreshToken);
        Task RevokeTokenAsync(string refreshToken);
        Task<bool> InitiatePasswordResetAsync(string email, string tenantSlug);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
    }

    public record RegisterRequest(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string? Phone,
        string TenantSlug,
        string Role         // 'customer' | 'driver' | 'manager' | 'cook'
    );

    public record LoginRequest(
        string Email,
        string Password
    );

    public record AuthResult(
        bool Success,
        string? AccessToken,
        string? RefreshToken,
        DateTime? ExpiresAt,
        string? Error
    );
}
