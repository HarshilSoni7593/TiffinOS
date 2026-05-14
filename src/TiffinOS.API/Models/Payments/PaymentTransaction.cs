using TiffinOS.API.Models.Tiffin;

namespace TiffinOS.API.Models.Payments
{
    public class PaymentTransaction
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid SubscriptionId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "CAD";
        public string Status { get; set; } = "pending";     // 'pending', 'success', 'failed', 'refunded'
        public string Gateway { get; set; } = string.Empty; // 'razorpay', 'stripe'
        public string? GatewayRef { get; set; }             // gateway's transaction ID — UNIQUE
        public string IdempotencyKey { get; set; } = string.Empty; // prevents duplicate charges — UNIQUE
        public string? FailureReason { get; set; }
        public DateTime? RefundedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Subscription Subscription { get; set; } = null!;

    }
}
