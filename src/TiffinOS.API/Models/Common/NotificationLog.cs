namespace TiffinOS.API.Models.Common
{
    public class NotificationLog
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public string Channel { get; set; } = string.Empty;        // 'email', 'sms', 'push'
        public string Type { get; set; } = string.Empty;           // 'renewal_reminder', 'delivery_done' etc.
        public Guid? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }
        public string Payload { get; set; } = "{}";                // JSONB
        public string Status { get; set; } = "sent";               // 'sent', 'failed', 'delivered'
        public DateTime SentAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Tenant Tenant { get; set; } = null!;
        public User User { get; set; } = null!;

    }
}
