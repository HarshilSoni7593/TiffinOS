namespace TiffinOS.API.Models.Tiffin
{
    public class DriverLocation
    {
        public Guid Id { get; set; }
        public Guid DriverId { get; set; }
        public Guid RouteAssignmentId { get; set; }
        public decimal Lat { get; set; }
        public decimal Lng { get; set; }
        public decimal? AccuracyMetres { get; set; }
        public DateTime RecordedAt { get; set; }

        // Navigation
        public DriverProfile Driver { get; set; } = null!;
        public RouteAssignment RouteAssignment { get; set; } = null!;
    }
}
