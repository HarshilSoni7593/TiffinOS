namespace TiffinOS.API.Models.Common
{
    public class TenantModule
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string ModuleSlug { get; set; } = string.Empty;  // 'tiffin' | 'inventory' | 'table_booking'
        public bool IsActive { get; set; } = true;
        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }

        // Navigation
        public Tenant Tenant { get; set; } = null!;

    }
}
