namespace TiffinOS.API.Models.Tiffin
{
    public class SubscriptionRenewal
    {
        public Guid Id { get; set; }
        public Guid SubscriptionId { get; set; }
        public Guid PricingTierId { get; set; }
        public DateOnly PeriodStart { get; set; }
        public DateOnly PeriodEnd { get; set; }
        public decimal AmountCharged { get; set; }
        public decimal DeliveryCharge { get; set; } = 0;
        public string Status { get; set; } = "pending";  // 'pending', 'paid', 'failed'
        public Guid? PaymentTransactionId { get; set; }
        public DateTime? ReminderSentAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Subscription Subscription { get; set; } = null!;
        public PlanPricingTier PricingTier { get; set; } = null!;
        public Payments.PaymentTransaction? PaymentTransaction { get; set; }

    }
}
