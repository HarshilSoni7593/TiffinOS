namespace TiffinOS.API.Models.Common
{
    public class ScheduleRunLog
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string RunType { get; set; } = string.Empty;     // 'packing_summary', 'dispatch_list', 'expiry_reminder'
        public DateTime ScheduledFor { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = "pending";         // 'pending', 'running', 'success', 'failed'
        public DateOnly TargetDate { get; set; }
        public int? RecordsGenerated { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Tenant Tenant { get; set; } = null!;

    }
}
