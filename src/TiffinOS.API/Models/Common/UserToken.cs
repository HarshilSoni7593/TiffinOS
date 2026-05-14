namespace TiffinOS.API.Models.Common
{
    public class UserToken
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Token { get; set; } = string.Empty;       // hashed
        public string TokenType { get; set; } = string.Empty;   // 'password_reset', 'email_verify', 'onboard_invite'
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UsedAt { get; set; }

        // Navigation
        public User User { get; set; } = null!;

    }
}
