using System.ComponentModel.DataAnnotations.Schema;

namespace TiffinOS.API.Models.Tiffin
{
    public class TiffinPlan
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DietaryType { get; set; } = string.Empty;     // 'veg', 'non_veg', 'both'
        public string? BoxType { get; set; }                        // '2_compartment', '3_compartment', '4_compartment'
        public int? TotalDeliveries { get; set; }                   // null = open-ended
        public DateOnly? AvailableFrom { get; set; }
        public DateOnly? AvailableUntil { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? ImageUrl { get; set; }
        public bool AllowSkip { get; set; } = false;
        public int? MinSkipNoticeHours { get; set; }
        public int? MaxSkipsPerCycle { get; set; }
        public string? SkipCreditPolicy { get; set; }   // 'none', 'credit'
        public int ExpiryReminderDays { get; set; } = 3;
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }


        // Navigation
        public ICollection<TiffinPlanItem> TiffinPlanItems { get; set; } = [];
        public ICollection<PlanPricingTier> PricingTiers { get; set; } = [];
        public ICollection<Subscription> Subscriptions { get; set; } = [];

        [ForeignKey(nameof(CreatedBy))]
        public Common.User? CreatedByUser { get; set; }

        [ForeignKey(nameof(UpdatedBy))]
        public Common.User? UpdatedByUser { get; set; }
    }
}
