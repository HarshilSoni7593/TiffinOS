namespace TiffinOS.API.Models.Tiffin
{
    public class DeliveryZone
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ZoneCode { get; set; } = string.Empty;
        public string PolygonCoords { get; set; } = "[]";   // JSONB — array of lat/lng pairs
        public string? ColorHex { get; set; }               // e.g. '#3B82F6' for map display
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Subscription> Subscriptions { get; set; } = [];
        public ICollection<RouteAssignment> RouteAssignments { get; set; } = [];
    }
}
