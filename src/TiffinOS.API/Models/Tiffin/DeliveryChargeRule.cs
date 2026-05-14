using System.ComponentModel.DataAnnotations.Schema;

namespace TiffinOS.API.Models.Tiffin
{
    public class DeliveryChargeRule
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid? ZoneId { get; set; }               // null = all zones
        public Guid? PricingTierId { get; set; }         // null = all tiers
        public string ChargeType { get; set; } = string.Empty; // 'free', 'flat', 'per_delivery'
        public decimal Amount { get; set; } = 0;
        public decimal? MinPlanPricePerDay { get; set; } // null = no minimum
        public bool IsActive { get; set; } = true;
        public int Priority { get; set; } = 0;
        public string? Description { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public DeliveryZone? Zone { get; set; }
        public PlanPricingTier? PricingTier { get; set; }
        public ICollection<Subscription> Subscriptions { get; set; } = [];

        [ForeignKey(nameof(CreatedBy))]
        public Common.User? CreatedByUser { get; set; }

        [ForeignKey(nameof(UpdatedBy))]
        public Common.User? UpdatedByUser { get; set; }
    }
}
