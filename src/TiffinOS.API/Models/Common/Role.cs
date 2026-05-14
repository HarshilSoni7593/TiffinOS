namespace TiffinOS.API.Models.Common
{
    public class Role
    {
        public Guid Id { get; set; }
        public Guid? TenantId { get; set; }         // null = system-wide role
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public bool IsSystemRole { get; set; } = false;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Tenant? Tenant { get; set; }
        public ICollection<UserRole> UserRoles { get; set; } = [];
        public ICollection<RolePermission> RolePermissions { get; set; } = [];
    }
}
