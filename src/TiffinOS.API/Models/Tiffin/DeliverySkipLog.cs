namespace TiffinOS.API.Models.Tiffin
{
    public class DeliverySkipLog
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid SubscriptionId { get; set; }
        public DateOnly SkippedDate { get; set; }
        public string Reason { get; set; } = string.Empty;      // 'customer_request', 'admin_override', 'holiday'
        public string CreditPolicy { get; set; } = string.Empty; // snapshot of plan's skip_credit_policy at time of skip
        public Guid? RequestedBy { get; set; }                   // FK → users.id
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Subscription Subscription { get; set; } = null!;
    }
}
