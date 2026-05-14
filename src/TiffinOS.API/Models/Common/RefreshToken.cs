namespace TiffinOS.API.Models.Common
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Token { get; set; } = string.Empty;      // hashed
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAt { get; set; }
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsActive => RevokedAt == null && !IsExpired;

        // Navigation
        public User User { get; set; } = null!;

    }
}
