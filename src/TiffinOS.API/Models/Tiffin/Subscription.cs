using System.ComponentModel.DataAnnotations.Schema;

namespace TiffinOS.API.Models.Tiffin
{
    public class Subscription
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid PlanId { get; set; }
        public Guid ZoneId { get; set; }
        public string? SpicePreference { get; set; }        // 'mild', 'medium', 'spicy'
        public string DeliveryAddress { get; set; } = string.Empty;
        public decimal DeliveryLat { get; set; }
        public decimal DeliveryLng { get; set; }
        public string? FloorOrUnit { get; set; }
        public string? DeliveryInstructions { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string Status { get; set; } = "pending";     // 'pending', 'active', 'paused', 'cancelled', 'expired'
        public DateOnly? PausedUntil { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public Guid PricingTierId { get; set; }
        public decimal LockedPricePerDay { get; set; }      // snapshot at subscription time
        public decimal LockedTotalAmount { get; set; }       // snapshot at subscription time
        public Guid? DeliveryChargeRuleId { get; set; }
        public decimal LockedDeliveryCharge { get; set; } = 0;
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;


        // Navigation
        public TiffinPlan Plan { get; set; } = null!;
        public DeliveryZone Zone { get; set; } = null!;
        public DeliveryChargeRule? DeliveryChargeRule { get; set; }
        public PlanPricingTier PricingTier { get; set; } = null!;
        public ICollection<SubscriptionRenewal> SubscriptionRenewals { get; set; } = [];
        public ICollection<DeliverySchedule> DeliverySchedules { get; set; } = [];
        public ICollection<Payments.PaymentTransaction> PaymentTransactions { get; set; } = [];

        [ForeignKey(nameof(CreatedBy))]
        public Common.User? CreatedByUser { get; set; }

        [ForeignKey(nameof(UpdatedBy))]
        public Common.User? UpdatedByUser { get; set; }
    }
}
