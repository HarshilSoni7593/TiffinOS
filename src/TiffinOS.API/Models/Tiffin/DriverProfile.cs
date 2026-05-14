using System.ComponentModel.DataAnnotations.Schema;
using TiffinOS.API.Models.Common;

namespace TiffinOS.API.Models.Tiffin
{
    public class DriverProfile
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public string VehicleType { get; set; } = string.Empty;  // 'bike', 'scooter', 'car', 'cycle'
        public string? LicenceNumber { get; set; }
        public int MaxDeliveriesPerDay { get; set; } = 100;
        public bool IsAvailable { get; set; } = true;
        public Guid? PayoutPolicyId { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public User User { get; set; } = null!;
        public ICollection<RouteAssignment> RouteAssignments { get; set; } = [];
        public ICollection<DeliverySchedule> DeliverySchedules { get; set; } = [];
        public ICollection<DriverLocation> DriverLocations { get; set; } = [];
        public DriverPayoutPolicy? PayoutPolicy { get; set; }
        public ICollection<DriverPayoutRecord> PayoutRecords { get; set; } = [];
        public ICollection<DriverPayoutSettlement> PayoutSettlements { get; set; } = [];

        [ForeignKey(nameof(CreatedBy))]
        public Common.User? CreatedByUser { get; set; }

        [ForeignKey(nameof(UpdatedBy))]
        public Common.User? UpdatedByUser { get; set; }
    }
}
