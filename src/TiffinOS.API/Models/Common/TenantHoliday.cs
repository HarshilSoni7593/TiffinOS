using System.ComponentModel.DataAnnotations.Schema;

namespace TiffinOS.API.Models.Common
{
    public class TenantHoliday
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public DateOnly HolidayDate { get; set; }
        public string? Reason { get; set; }
        public bool IsDeliveryOff { get; set; } = true;
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;


        // Navigation
        public Tenant Tenant { get; set; } = null!;

        [ForeignKey(nameof(CreatedBy))]
        public User? CreatedByUser { get; set; }

        [ForeignKey(nameof(UpdatedBy))]
        public User? UpdatedByUser { get; set; }
    }
}
