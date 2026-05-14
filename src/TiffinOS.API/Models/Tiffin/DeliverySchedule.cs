using System.ComponentModel.DataAnnotations.Schema;

namespace TiffinOS.API.Models.Tiffin
{
    public class DeliverySchedule
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid SubscriptionId { get; set; }
        public Guid? RouteAssignmentId { get; set; }    // set when driver is assigned
        public Guid? DriverId { get; set; }
        public DateOnly ScheduledDate { get; set; }
        public int? SequenceNumber { get; set; }         // position in driver's route
        public string Status { get; set; } = "pending"; // 'pending','assigned','in_progress','delivered','attempted','skipped','paused'
        public DateTime? AttemptedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? FailureReason { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Subscription Subscription { get; set; } = null!;
        public RouteAssignment? RouteAssignment { get; set; }
        public DriverProfile? Driver { get; set; }
        public PODRecord? PODRecord { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public Common.User? CreatedByUser { get; set; }

        [ForeignKey(nameof(UpdatedBy))]
        public Common.User? UpdatedByUser { get; set; }
    }
}
