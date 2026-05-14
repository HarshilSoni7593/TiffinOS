namespace TiffinOS.API.Models.Tiffin
{
    public class DriverPayoutSettlement
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid DriverId { get; set; }
        public DateOnly PeriodStart { get; set; }
        public DateOnly PeriodEnd { get; set; }
        public decimal TotalAmount { get; set; }
        public string? PaymentMethod { get; set; }      // 'cash', 'bank_transfer', 'upi'
        public string? PaymentReference { get; set; }
        public string Status { get; set; } = "pending"; // 'pending', 'paid'
        public Guid? ProcessedBy { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public DriverProfile Driver { get; set; } = null!;

    }
}
