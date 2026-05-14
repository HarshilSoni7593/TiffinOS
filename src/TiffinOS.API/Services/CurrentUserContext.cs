namespace TiffinOS.API.Services
{
    public class CurrentUserContext
    {
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string[] Roles { get; set; } = [];
        public string[] Permissions { get; set; } = [];
        public bool IsAuthenticated { get; set; } = false;

        public bool HasPermission(string slug) =>
        Permissions.Contains(slug);

        public bool HasRole(string role) =>
            Roles.Contains(role);

        public bool IsInTenant(Guid tenantId) =>
            TenantId == tenantId;

    }
}
