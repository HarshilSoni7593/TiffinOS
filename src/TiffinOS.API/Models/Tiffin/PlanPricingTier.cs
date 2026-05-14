using System.ComponentModel.DataAnnotations.Schema;

namespace TiffinOS.API.Models.Tiffin
{
    public class PlanPricingTier
    {
        public Guid Id { get; set; }
        public Guid PlanId { get; set; }
        public string DurationType { get; set; } = string.Empty;    // 'daily', 'weekly', 'monthly'
        public decimal PricePerDay { get; set; }                    // effective daily rate
        public int BillingCycleDays { get; set; }                   // 1, 7, or 30
        public decimal TotalAmount { get; set; }                    // actual charge per cycle
        public bool IsActive { get; set; } = true;
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;


        // Navigation
        public TiffinPlan Plan { get; set; } = null!;
        public ICollection<Subscription> Subscriptions { get; set; } = [];

        [ForeignKey(nameof(CreatedBy))]
        public Common.User? CreatedByUser { get; set; }

        [ForeignKey(nameof(UpdatedBy))]
        public Common.User? UpdatedByUser { get; set; }
    }
}
