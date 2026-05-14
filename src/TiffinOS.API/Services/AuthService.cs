using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TiffinOS.API.Data;
using TiffinOS.API.Models.Common;
using TiffinOS.API.Services.Interfaces;

namespace TiffinOS.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<AuthResult> RegisterAsync(RegisterRequest request)
        {
            // Find tenant
            var tenant = await _db.Tenants
                .FirstOrDefaultAsync(t => t.Slug == request.TenantSlug && t.IsActive);

            if (tenant == null)
                return Fail("Tenant not found.");

            // Check duplicate email within tenant
            var exists = await _db.Users
                .AnyAsync(u => u.TenantId == tenant.Id &&
                               u.Email == request.Email.ToLower());

            if (exists)
                return Fail("An account with this email already exists.");

            // Find the role
            var role = await _db.Roles
                .FirstOrDefaultAsync(r => r.Slug == request.Role && r.IsSystemRole);

            if (role == null)
                return Fail("Invalid role specified.");

            // Create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Email = request.Email.ToLower().Trim(),
                PasswordHash = HashPassword(request.Password),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Phone = request.Phone?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Assign role
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                TenantId = tenant.Id,
                AssignedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            _db.UserRoles.Add(userRole);
            await _db.SaveChangesAsync();

            // Generate tokens
            return await GenerateTokensAsync(user, tenant.Id, [role.Slug]);
        }

        public async Task<AuthResult> LoginAsync(LoginRequest request, string tenantSlug)
        {
            var tenant = await _db.Tenants
                .FirstOrDefaultAsync(t => t.Slug == tenantSlug && t.IsActive);

            if (tenant == null)
                return Fail("Tenant not found.");

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.TenantId == tenant.Id &&
                                          u.Email == request.Email.ToLower() &&
                                          u.IsActive);

            // Use same error for both not found and wrong password
            // Never tell the caller which one failed — security best practice
            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
                return Fail("Invalid email or password.");

            // Load user roles
            var roles = await _db.UserRoles
                .Where(ur => ur.UserId == user.Id && ur.TenantId == tenant.Id)
                .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Slug)
                .ToArrayAsync();

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return await GenerateTokensAsync(user, tenant.Id, roles);
        }

        public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
        {
            var hashedToken = HashToken(refreshToken);

            var token = await _db.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == hashedToken);

            if (token == null || !token.IsActive)
                return Fail("Invalid or expired refresh token.");

            // Revoke current token
            token.RevokedAt = DateTime.UtcNow;

            // Load roles
            var roles = await _db.UserRoles
                .Where(ur => ur.UserId == token.UserId &&
                             ur.TenantId == token.User.TenantId)
                .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Slug)
                .ToArrayAsync();

            await _db.SaveChangesAsync();

            return await GenerateTokensAsync(token.User, token.User.TenantId, roles);
        }

        // ── REVOKE TOKEN ──────────────────────────────────────────
        public async Task RevokeTokenAsync(string refreshToken)
        {
            var hashedToken = HashToken(refreshToken);

            var token = await _db.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == hashedToken);

            if (token != null && token.IsActive)
            {
                token.RevokedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        // ── PASSWORD RESET ────────────────────────────────────────
        public async Task<bool> InitiatePasswordResetAsync(string email, string tenantSlug)
        {
            var tenant = await _db.Tenants
                .FirstOrDefaultAsync(t => t.Slug == tenantSlug && t.IsActive);

            if (tenant == null) return true; // Silent — don't reveal tenant existence

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.TenantId == tenant.Id &&
                                          u.Email == email.ToLower() &&
                                          u.IsActive);

            if (user == null) return true; // Silent — don't reveal email existence

            // Expire any existing reset tokens
            var existingTokens = await _db.UserTokens
                .Where(ut => ut.UserId == user.Id &&
                             ut.TokenType == "password_reset" &&
                             !ut.IsUsed)
                .ToListAsync();

            foreach (var t in existingTokens)
                t.IsUsed = true;

            // Generate new reset token
            var rawToken = GenerateSecureToken();
            var userToken = new UserToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = HashToken(rawToken),
                TokenType = "password_reset",
                ExpiresAt = DateTime.UtcNow.AddHours(2),
                CreatedAt = DateTime.UtcNow
            };

            _db.UserTokens.Add(userToken);
            await _db.SaveChangesAsync();

            // TODO: Send email with reset link containing rawToken
            // EmailService.SendPasswordReset(user.Email, rawToken);

            return true;
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var hashedToken = HashToken(token);

            var userToken = await _db.UserTokens
                .Include(ut => ut.User)
                .FirstOrDefaultAsync(ut => ut.Token == hashedToken &&
                                           ut.TokenType == "password_reset" &&
                                           !ut.IsUsed &&
                                           ut.ExpiresAt > DateTime.UtcNow);

            if (userToken == null) return false;

            // Update password
            userToken.User.PasswordHash = HashPassword(newPassword);
            userToken.User.UpdatedAt = DateTime.UtcNow;
            userToken.IsUsed = true;
            userToken.UsedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────
        private async Task<AuthResult> GenerateTokensAsync(
            User user,
            Guid tenantId,
            string[] roles)
        {
            // Load permissions for these roles
            var roleIds = await _db.UserRoles
                .Where(ur => ur.UserId == user.Id && ur.TenantId == tenantId)
                .Select(ur => ur.RoleId)
                .ToArrayAsync();

            var permissions = await _db.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Join(_db.Permissions,
                      rp => rp.PermissionId,
                      p => p.Id,
                      (rp, p) => p.Slug)
                .Distinct()
                .ToArrayAsync();

            // Build JWT claims
            var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new("tenant_id",                   tenantId.ToString()),
            new("first_name",                  user.FirstName),
        };

            // Add roles as claims
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            // Add permissions as claims
            foreach (var permission in permissions)
                claims.Add(new Claim("permission", permission));

            // Generate access token
            var key = new SymmetricSecurityKey(
                                 Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddMinutes(
                                 _config.GetValue<int>("Jwt:ExpiryMinutes"));
            var jwtToken = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            // Generate refresh token
            var rawRefresh = GenerateSecureToken();
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = HashToken(rawRefresh),
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };

            _db.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync();

            return new AuthResult(
                Success: true,
                AccessToken: accessToken,
                RefreshToken: rawRefresh,   // return raw — never store raw
                ExpiresAt: expiry,
                Error: null
            );
        }

        private static string HashPassword(string password) =>
            BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        private static bool VerifyPassword(string password, string hash) =>
            BCrypt.Net.BCrypt.Verify(password, hash);

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }

        private static string GenerateSecureToken()
        {
            var bytes = new byte[64];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }

        private static AuthResult Fail(string error) =>
            new(false, null, null, null, error);
    }
}
