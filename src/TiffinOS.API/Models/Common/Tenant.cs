using System.Data;

namespace TiffinOS.API.Models.Common
{
    public class Tenant
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string PlanTier { get; set; } = "starter";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public TimeOnly PrepScheduleTime { get; set; } = new TimeOnly(21, 0);
        public int PrepScheduleOffsetDays { get; set; } = 1;
        public TimeOnly DispatchScheduleTime { get; set; } = new TimeOnly(7, 0);
        public TimeOnly DeliveryStartTime { get; set; } = new TimeOnly(8, 0);
        public TimeOnly DeliveryEndTime { get; set; } = new TimeOnly(20, 0);
        public string Timezone { get; set; } = "Asia/Kolkata";
        public string Currency { get; set; } = "CAD";
        public string? SmsSenderName { get; set; }
        public string Settings { get; set; } = "{}";

        // Navigation
        public ICollection<User> Users { get; set; } = [];
        public ICollection<Role> Roles { get; set; } = [];
        public ICollection<TenantModule> TenantModules { get; set; } = [];
        public ICollection<UserRole> UserRoles { get; set; } = [];

    }
}
