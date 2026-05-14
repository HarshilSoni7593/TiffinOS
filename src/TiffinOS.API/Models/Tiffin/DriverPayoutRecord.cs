namespace TiffinOS.API.Models.Tiffin
{
    public class DriverPayoutRecord
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid DriverId { get; set; }
        public Guid PolicyId { get; set; }
        public DateOnly PayoutDate { get; set; }
        public string PayoutType { get; set; } = string.Empty;  // snapshot of policy on that day
        public int TotalDeliveries { get; set; } = 0;
        public decimal? TotalDistanceKm { get; set; }
        public int? TotalZones { get; set; }
        public decimal BaseAmount { get; set; } = 0;
        public decimal BonusAmount { get; set; } = 0;
        public decimal TotalAmount { get; set; } = 0;
        public string Status { get; set; } = "pending";         // 'pending', 'approved', 'paid'
        public Guid? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public DriverProfile Driver { get; set; } = null!;
        public DriverPayoutPolicy Policy { get; set; } = null!;

    }
}
