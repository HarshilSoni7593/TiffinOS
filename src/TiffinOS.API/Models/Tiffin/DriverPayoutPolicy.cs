namespace TiffinOS.API.Models.Tiffin
{
    public class DriverPayoutPolicy
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PayoutType { get; set; } = string.Empty;  // 'per_delivery','per_day','per_km','per_zone','hybrid'
        public decimal BaseRate { get; set; } = 0;
        public decimal? BonusPerDelivery { get; set; }          // hybrid only
        public int? BonusThreshold { get; set; }                // hybrid only
        public decimal? MinGuaranteed { get; set; }
        public string Currency { get; set; } = "CAD";
        public bool IsActive { get; set; } = true;
        public Guid? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<DriverProfile> DriverProfiles { get; set; } = [];
        public ICollection<DriverPayoutRecord> PayoutRecords { get; set; } = [];

    }
}
