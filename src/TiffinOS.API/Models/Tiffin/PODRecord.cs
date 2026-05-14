namespace TiffinOS.API.Models.Tiffin
{
    public class PODRecord
    {
        public Guid Id { get; set; }
        public Guid DeliveryId { get; set; }
        public string PhotoUrl { get; set; } = string.Empty;    // Azure Blob Storage URL
        public decimal CaptureLat { get; set; }                 // GPS at shutter fire
        public decimal CaptureLng { get; set; }                 // GPS at shutter fire
        public DateTime CaptureAt { get; set; }                 // timestamp at shutter fire
        public DateTime UploadAt { get; set; }                  // timestamp when received by server
        public string Status { get; set; } = "uploaded";        // 'uploaded', 'verified', 'disputed'

        // Navigation
        public DeliverySchedule Delivery { get; set; } = null!;
    }
}
