namespace TiffinOS.API.Services
{
    public class TenantContext
    {
        public Guid TenantId { get; set; }
        public string TenantSlug { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string[] EnabledModules { get; set; } = [];
        public bool IsResolved { get; set; } = false;
    }
}
