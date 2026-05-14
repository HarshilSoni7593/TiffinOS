using System.ComponentModel.DataAnnotations.Schema;

namespace TiffinOS.API.Models.Tiffin
{
    public class RouteAssignment
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid DriverId { get; set; }
        public Guid ZoneId { get; set; }
        public DateOnly AssignmentDate { get; set; }
        public List<Guid> OptimisedSequence { get; set; } = []; // ordered delivery_schedule ids
        public int TotalStops { get; set; } = 0;
        public string Status { get; set; } = "pending";  // 'pending', 'in_progress', 'completed', 'cancelled'
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public decimal? ActualDistanceKm { get; set; }

        // Navigation
        public DriverProfile Driver { get; set; } = null!;
        public DeliveryZone Zone { get; set; } = null!;
        public ICollection<DeliverySchedule> DeliverySchedules { get; set; } = [];
        public ICollection<DriverLocation> DriverLocations { get; set; } = [];

        [ForeignKey(nameof(CreatedBy))]
        public Common.User? CreatedByUser { get; set; }

        [ForeignKey(nameof(UpdatedBy))]
        public Common.User? UpdatedByUser { get; set; }
    }
}
