namespace TiffinOS.API.Models.Common
{
    public class UserRole
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid TenantId { get; set; }
        public Guid? AssignedBy { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public User User { get; set; } = null!;
        public Role Role { get; set; } = null!;
        public Tenant Tenant { get; set; } = null!;

    }
}
