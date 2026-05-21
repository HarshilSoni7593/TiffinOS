using Microsoft.EntityFrameworkCore;
using TiffinOS.API.Models.Common;
using TiffinOS.API.Models.Tiffin;
using TiffinOS.API.Models.Inventory;
using TiffinOS.API.Models.Payments;

namespace TiffinOS.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Common ────────────────────────────────────────────────
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<TenantModule> TenantModules => Set<TenantModule>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<TenantHoliday> TenantHolidays => Set<TenantHoliday>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<ScheduleRunLog> ScheduleRunLogs => Set<ScheduleRunLog>();

    // ── Tiffin ────────────────────────────────────────────────
    public DbSet<DeliveryZone> DeliveryZones => Set<DeliveryZone>();
    public DbSet<DeliveryChargeRule> DeliveryChargeRules => Set<DeliveryChargeRule>();
    public DbSet<DriverPayoutPolicy> DriverPayoutPolicies => Set<DriverPayoutPolicy>();
    public DbSet<DriverProfile> DriverProfiles => Set<DriverProfile>();
    public DbSet<TiffinPlan> TiffinPlans => Set<TiffinPlan>();
    public DbSet<PlanPricingTier> PlanPricingTiers => Set<PlanPricingTier>();
    public DbSet<MenuItemCategory> MenuItemCategories => Set<MenuItemCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<TiffinPlanItem> TiffinPlanItems => Set<TiffinPlanItem>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<SubscriptionRenewal> SubscriptionRenewals => Set<SubscriptionRenewal>();
    public DbSet<DeliverySkipLog> DeliverySkipLogs => Set<DeliverySkipLog>();
    public DbSet<RouteAssignment> RouteAssignments => Set<RouteAssignment>();
    public DbSet<DeliverySchedule> DeliverySchedules => Set<DeliverySchedule>();
    public DbSet<DailyPackingSummary> DailyPackingSummaries => Set<DailyPackingSummary>();
    public DbSet<DriverLocation> DriverLocations => Set<DriverLocation>();
    public DbSet<PODRecord> PODRecords => Set<PODRecord>();
    public DbSet<DriverPayoutRecord> DriverPayoutRecords => Set<DriverPayoutRecord>();
    public DbSet<DriverPayoutSettlement> DriverPayoutSettlements => Set<DriverPayoutSettlement>();

    // ── Inventory ─────────────────────────────────────────────
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<ItemSupplier> ItemSuppliers => Set<ItemSupplier>();
    public DbSet<StockLog> StockLogs => Set<StockLog>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();

    // ── Payments ──────────────────────────────────────────────
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);
        ConfigureCommon(mb);
        ConfigureTiffin(mb);
        ConfigureInventory(mb);
        ConfigurePayments(mb);
        SeedPermissions(mb);
        SeedRoles(mb);
        SeedRolePermissions(mb);
    }

    // ── COMMON CONFIGURATION ──────────────────────────────────
    private static void ConfigureCommon(ModelBuilder mb)
    {
        // Tenant
        mb.Entity<Tenant>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(t => t.Slug).IsUnique();
            e.Property(t => t.Slug).IsRequired().HasMaxLength(100);
            e.Property(t => t.Name).IsRequired().HasMaxLength(150);
            e.Property(t => t.PlanTier).HasMaxLength(50).HasDefaultValue("starter");
            e.Property(t => t.Timezone).HasMaxLength(50).HasDefaultValue("America/Toronto");
            e.Property(t => t.Currency).HasMaxLength(3).HasDefaultValue("CAD");
            e.Property(t => t.Settings).HasColumnType("jsonb").HasDefaultValue("{}");
            e.Property(t => t.CreatedAt).HasDefaultValueSql("now()");
            e.Property(t => t.UpdatedAt).HasDefaultValueSql("now()");
        });

        // Permission
        mb.Entity<Permission>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(p => p.Slug).IsUnique();
            e.Property(p => p.Slug).IsRequired().HasMaxLength(150);
            e.Property(p => p.Name).IsRequired().HasMaxLength(150);
            e.Property(p => p.Module).IsRequired().HasMaxLength(50);
            e.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
        });

        // Role
        mb.Entity<Role>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(r => r.Slug).IsUnique();
            e.Property(r => r.Slug).IsRequired().HasMaxLength(100);
            e.Property(r => r.Name).IsRequired().HasMaxLength(100);
            e.HasOne(r => r.Tenant)
             .WithMany(t => t.Roles)
             .HasForeignKey(r => r.TenantId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(r => r.CreatedAt).HasDefaultValueSql("now()");
        });

        // TenantModule
        mb.Entity<TenantModule>(e =>
        {
            e.HasKey(tm => tm.Id);
            e.Property(tm => tm.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(tm => new { tm.TenantId, tm.ModuleSlug }).IsUnique();
            e.Property(tm => tm.ModuleSlug).IsRequired().HasMaxLength(50);
            e.HasOne(tm => tm.Tenant)
             .WithMany(t => t.TenantModules)
             .HasForeignKey(tm => tm.TenantId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(tm => tm.SubscribedAt).HasDefaultValueSql("now()");
        });

        // User
        mb.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
            e.Property(u => u.Email).IsRequired().HasMaxLength(255);
            e.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            e.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            e.Property(u => u.Phone).HasMaxLength(20);
            e.HasOne(u => u.Tenant)
             .WithMany(t => t.Users)
             .HasForeignKey(u => u.TenantId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(u => u.CreatedAt).HasDefaultValueSql("now()");
            e.Property(u => u.UpdatedAt).HasDefaultValueSql("now()");
        });

        // RefreshToken
        mb.Entity<RefreshToken>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(r => r.Token).IsUnique();
            e.Property(r => r.Token).IsRequired();
            e.HasOne(r => r.User)
             .WithMany()
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(r => r.CreatedAt).HasDefaultValueSql("now()");
        });

        // UserToken
        mb.Entity<UserToken>(e =>
        {
            e.HasKey(ut => ut.Id);
            e.Property(ut => ut.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(ut => ut.Token).IsUnique();
            e.Property(ut => ut.Token).IsRequired();
            e.Property(ut => ut.TokenType).IsRequired().HasMaxLength(50);
            e.HasOne(ut => ut.User)
             .WithMany()
             .HasForeignKey(ut => ut.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(ut => ut.CreatedAt).HasDefaultValueSql("now()");
        });

        // UserRole — composite PK
        mb.Entity<UserRole>(e =>
        {
            e.HasKey(ur => new { ur.UserId, ur.RoleId, ur.TenantId });
            e.HasOne(ur => ur.User)
             .WithMany(u => u.UserRoles)
             .HasForeignKey(ur => ur.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ur => ur.Role)
             .WithMany(r => r.UserRoles)
             .HasForeignKey(ur => ur.RoleId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ur => ur.Tenant)
             .WithMany(t => t.UserRoles)
             .HasForeignKey(ur => ur.TenantId)
             .OnDelete(DeleteBehavior.NoAction);
            e.Property(ur => ur.AssignedAt).HasDefaultValueSql("now()");
        });

        // RolePermission — composite PK
        mb.Entity<RolePermission>(e =>
        {
            e.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            e.HasOne(rp => rp.Role)
             .WithMany(r => r.RolePermissions)
             .HasForeignKey(rp => rp.RoleId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(rp => rp.Permission)
             .WithMany(p => p.RolePermissions)
             .HasForeignKey(rp => rp.PermissionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // TenantHoliday
        mb.Entity<TenantHoliday>(e =>
        {
            e.HasKey(th => th.Id);
            e.Property(th => th.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(th => new { th.TenantId, th.HolidayDate }).IsUnique();
            e.HasOne(th => th.Tenant)
             .WithMany()
             .HasForeignKey(th => th.TenantId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(th => th.CreatedAt).HasDefaultValueSql("now()");
            e.Property(th => th.UpdatedAt).HasDefaultValueSql("now()");
        });

        // NotificationLog
        mb.Entity<NotificationLog>(e =>
        {
            e.HasKey(nl => nl.Id);
            e.Property(nl => nl.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(nl => new { nl.TenantId, nl.UserId });
            e.HasIndex(nl => nl.ReferenceId);
            e.Property(nl => nl.Channel).IsRequired().HasMaxLength(10);
            e.Property(nl => nl.Type).IsRequired().HasMaxLength(50);
            e.Property(nl => nl.Payload).HasColumnType("jsonb").HasDefaultValue("{}");
            e.HasOne(nl => nl.Tenant)
             .WithMany()
             .HasForeignKey(nl => nl.TenantId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(nl => nl.User)
             .WithMany()
             .HasForeignKey(nl => nl.UserId)
             .OnDelete(DeleteBehavior.NoAction);
            e.Property(nl => nl.CreatedAt).HasDefaultValueSql("now()");
        });

        // ScheduleRunLog
        mb.Entity<ScheduleRunLog>(e =>
        {
            e.HasKey(sl => sl.Id);
            e.Property(sl => sl.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(sl => new { sl.TenantId, sl.RunType, sl.TargetDate });
            e.Property(sl => sl.RunType).IsRequired().HasMaxLength(30);
            e.Property(sl => sl.Status).IsRequired().HasMaxLength(20);
            e.HasOne(sl => sl.Tenant)
             .WithMany()
             .HasForeignKey(sl => sl.TenantId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(sl => sl.CreatedAt).HasDefaultValueSql("now()");
        });
    }

    // ── TIFFIN CONFIGURATION ──────────────────────────────────
    private static void ConfigureTiffin(ModelBuilder mb)
    {
        // DeliveryZone
        mb.Entity<DeliveryZone>(e =>
        {
            e.HasKey(dz => dz.Id);
            e.Property(dz => dz.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(dz => new { dz.TenantId, dz.ZoneCode }).IsUnique();
            e.Property(dz => dz.Name).IsRequired().HasMaxLength(100);
            e.Property(dz => dz.ZoneCode).IsRequired().HasMaxLength(20);
            e.Property(dz => dz.PolygonCoords).HasColumnType("jsonb").HasDefaultValue("[]");
            e.Property(dz => dz.ColorHex).HasMaxLength(7);
            e.Property(dz => dz.CreatedAt).HasDefaultValueSql("now()");
        });

        // DeliveryChargeRule
        mb.Entity<DeliveryChargeRule>(e =>
        {
            e.HasKey(dc => dc.Id);
            e.Property(dc => dc.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(dc => new { dc.TenantId, dc.Priority });
            e.Property(dc => dc.ChargeType).IsRequired().HasMaxLength(20);
            e.Property(dc => dc.Amount).HasPrecision(10, 2);
            e.Property(dc => dc.MinPlanPricePerDay).HasPrecision(10, 2);
            e.Property(dc => dc.Description).HasMaxLength(255);
            e.HasOne(dc => dc.Zone)
             .WithMany()
             .HasForeignKey(dc => dc.ZoneId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(dc => dc.PricingTier)
             .WithMany()
             .HasForeignKey(dc => dc.PricingTierId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
            e.Property(dc => dc.CreatedAt).HasDefaultValueSql("now()");
            e.Property(dc => dc.UpdatedAt).HasDefaultValueSql("now()");
        });

        // DriverPayoutPolicy
        mb.Entity<DriverPayoutPolicy>(e =>
        {
            e.HasKey(dp => dp.Id);
            e.Property(dp => dp.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(dp => dp.Name).IsRequired().HasMaxLength(150);
            e.Property(dp => dp.PayoutType).IsRequired().HasMaxLength(20);
            e.Property(dp => dp.BaseRate).HasPrecision(10, 2);
            e.Property(dp => dp.BonusPerDelivery).HasPrecision(10, 2);
            e.Property(dp => dp.MinGuaranteed).HasPrecision(10, 2);
            e.Property(dp => dp.Currency).HasMaxLength(3).HasDefaultValue("INR");
            e.Property(dp => dp.CreatedAt).HasDefaultValueSql("now()");
            e.Property(dp => dp.UpdatedAt).HasDefaultValueSql("now()");
            e.HasOne(dp => dp.CreatedByUser)
             .WithMany()
             .HasForeignKey(dp => dp.CreatedBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(dp => dp.UpdatedByUser)
             .WithMany()
             .HasForeignKey(dp => dp.UpdatedBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.NoAction);

        });

        // DriverProfile
        mb.Entity<DriverProfile>(e =>
        {
            e.HasKey(dp => dp.Id);
            e.Property(dp => dp.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(dp => new { dp.TenantId, dp.UserId }).IsUnique();
            e.Property(dp => dp.VehicleType).IsRequired().HasMaxLength(50);
            e.Property(dp => dp.LicenceNumber).HasMaxLength(50);
            e.HasOne(dp => dp.User)
             .WithMany()
             .HasForeignKey(dp => dp.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(dp => dp.PayoutPolicy)
             .WithMany(p => p.DriverProfiles)
             .HasForeignKey(dp => dp.PayoutPolicyId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
            // created_by / updated_by — no cascade to avoid multiple cascade paths
            e.HasOne(dp => dp.CreatedByUser)
             .WithMany()
             .HasForeignKey(dp => dp.CreatedBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(dp => dp.UpdatedByUser)
             .WithMany()
             .HasForeignKey(dp => dp.UpdatedBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.NoAction);
            e.Property(dp => dp.CreatedAt).HasDefaultValueSql("now()");
            e.Property(dp => dp.UpdatedAt).HasDefaultValueSql("now()");
        });

        // TiffinPlan
        mb.Entity<TiffinPlan>(e =>
        {
            e.HasKey(tp => tp.Id);
            e.Property(tp => tp.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(tp => tp.Name).IsRequired().HasMaxLength(150);
            e.Property(tp => tp.DietaryType).IsRequired().HasMaxLength(20);
            e.Property(tp => tp.BoxType).HasMaxLength(50);
            e.Property(tp => tp.SkipCreditPolicy).HasMaxLength(20);
            e.HasOne(tp => tp.CreatedByUser)
             .WithMany()
             .HasForeignKey(tp => tp.CreatedBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(tp => tp.UpdatedByUser)
             .WithMany()
             .HasForeignKey(tp => tp.UpdatedBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.NoAction);
            e.Property(tp => tp.CreatedAt).HasDefaultValueSql("now()");
            e.Property(tp => tp.UpdatedAt).HasDefaultValueSql("now()");
        });

        // PlanPricingTier
        mb.Entity<PlanPricingTier>(e =>
        {
            e.HasKey(pt => pt.Id);
            e.Property(pt => pt.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(pt => new { pt.PlanId, pt.DurationType }).IsUnique();
            e.Property(pt => pt.DurationType).IsRequired().HasMaxLength(20);
            e.Property(pt => pt.PricePerDay).HasPrecision(10, 2);
            e.Property(pt => pt.TotalAmount).HasPrecision(10, 2);
            e.HasOne(pt => pt.Plan)
             .WithMany(tp => tp.PricingTiers)
             .HasForeignKey(pt => pt.PlanId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(pt => pt.CreatedByUser)
             .WithMany()
             .HasForeignKey(pt => pt.CreatedBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(pt => pt.UpdatedByUser)
             .WithMany()
             .HasForeignKey(pt => pt.UpdatedBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.NoAction);
            e.Property(pt => pt.CreatedAt).HasDefaultValueSql("now()");
            e.Property(pt => pt.UpdatedAt).HasDefaultValueSql("now()");
        });

        // MenuItemCategory
        mb.Entity<MenuItemCategory>(e =>
        {
            e.HasKey(mc => mc.Id);
            e.Property(mc => mc.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(mc => new { mc.TenantId, mc.Name }).IsUnique();
            e.Property(mc => mc.Name).IsRequired().HasMaxLength(100);
        });

        // MenuItem
        mb.Entity<MenuItem>(e =>
        {
            e.HasKey(mi => mi.Id);
            e.Property(mi => mi.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(mi => new { mi.TenantId, mi.Name }).IsUnique();
            e.Property(mi => mi.Name).IsRequired().HasMaxLength(150);
            e.Property(mi => mi.Unit).IsRequired().HasMaxLength(30);
            e.Property(mi => mi.MeasurementType).IsRequired().HasMaxLength(20);
            var stringListConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<List<string>, string>(
    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
);
            var stringListComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()
            );
            e.Property(mi => mi.AvailablePortions)
             .HasColumnType("jsonb")
             .HasConversion(stringListConverter, stringListComparer);
            e.HasOne(mi => mi.Category)
             .WithMany(mc => mc.MenuItems)
             .HasForeignKey(mi => mi.CategoryId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
            e.Property(mi => mi.CreatedAt).HasDefaultValueSql("now()");
        });

        // TiffinPlanItem
        mb.Entity<TiffinPlanItem>(e =>
        {
            e.HasKey(ti => ti.Id);
            e.Property(ti => ti.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(ti => new { ti.PlanId, ti.MenuItemId }).IsUnique();
            e.Property(ti => ti.PortionSize).IsRequired().HasMaxLength(50);
            e.Property(ti => ti.Quantity).HasPrecision(6, 2);
            e.HasOne(ti => ti.Plan)
             .WithMany(tp => tp.TiffinPlanItems)
             .HasForeignKey(ti => ti.PlanId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ti => ti.MenuItem)
             .WithMany(mi => mi.TiffinPlanItems)
             .HasForeignKey(ti => ti.MenuItemId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Subscription
        mb.Entity<Subscription>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(s => new { s.TenantId, s.CustomerId });
            e.HasIndex(s => new { s.TenantId, s.Status });
            e.Property(s => s.Status).IsRequired().HasMaxLength(20).HasDefaultValue("pending");
            e.Property(s => s.DeliveryLat).HasPrecision(10, 7);
            e.Property(s => s.DeliveryLng).HasPrecision(10, 7);
            e.Property(s => s.LockedPricePerDay).HasPrecision(10, 2);
            e.Property(s => s.LockedTotalAmount).HasPrecision(10, 2);
            e.Property(s => s.LockedDeliveryCharge).HasPrecision(10, 2);
            e.HasOne(s => s.Plan)
             .WithMany(tp => tp.Subscriptions)
             .HasForeignKey(s => s.PlanId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Zone)
             .WithMany(dz => dz.Subscriptions)
             .HasForeignKey(s => s.ZoneId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.PricingTier)
             .WithMany(pt => pt.Subscriptions)
             .HasForeignKey(s => s.PricingTierId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.DeliveryChargeRule)
             .WithMany(dc => dc.Subscriptions)
             .HasForeignKey(s => s.DeliveryChargeRuleId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(s => s.CreatedByUser)
             .WithMany()
             .HasForeignKey(s => s.CreatedBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(s => s.UpdatedByUser)
             .WithMany()
             .HasForeignKey(s => s.UpdatedBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.NoAction);
            e.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
            e.Property(s => s.UpdatedAt).HasDefaultValueSql("now()");
        });

        // SubscriptionRenewal
        mb.Entity<SubscriptionRenewal>(e =>
        {
            e.HasKey(sr => sr.Id);
            e.Property(sr => sr.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(sr => new { sr.SubscriptionId, sr.PeriodStart });
            e.Property(sr => sr.Status).IsRequired().HasMaxLength(20).HasDefaultValue("pending");
            e.Property(sr => sr.AmountCharged).HasPrecision(10, 2);
            e.Property(sr => sr.DeliveryCharge).HasPrecision(10, 2);
            e.HasOne(sr => sr.Subscription)
             .WithMany(s => s.SubscriptionRenewals)
             .HasForeignKey(sr => sr.SubscriptionId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(sr => sr.PricingTier)
             .WithMany()
             .HasForeignKey(sr => sr.PricingTierId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(sr => sr.PaymentTransaction)
             .WithMany()
             .HasForeignKey(sr => sr.PaymentTransactionId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
            e.Property(sr => sr.CreatedAt).HasDefaultValueSql("now()");
        });

        // DeliverySkipLog
        mb.Entity<DeliverySkipLog>(e =>
        {
            e.HasKey(ds => ds.Id);
            e.Property(ds => ds.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(ds => new { ds.SubscriptionId, ds.SkippedDate }).IsUnique();
            e.Property(ds => ds.Reason).IsRequired().HasMaxLength(50);
            e.Property(ds => ds.CreditPolicy).IsRequired().HasMaxLength(20);
            e.HasOne(ds => ds.Subscription)
             .WithMany()
             .HasForeignKey(ds => ds.SubscriptionId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(ds => ds.CreatedAt).HasDefaultValueSql("now()");
        });

        // RouteAssignment
        mb.Entity<RouteAssignment>(e =>
        {
            e.HasKey(ra => ra.Id);
            e.Property(ra => ra.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(ra => new { ra.DriverId, ra.AssignmentDate, ra.ZoneId }).IsUnique();
            e.Property(ra => ra.Status).IsRequired().HasMaxLength(20).HasDefaultValue("pending");
            e.Property(ra => ra.ActualDistanceKm).HasPrecision(8, 2);
            var guidListConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<List<Guid>, string>(
    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
    v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Guid>()
);
            var guidListComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<Guid>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()
            );
            e.Property(ra => ra.OptimisedSequence)
             .HasColumnType("jsonb")
             .HasConversion(guidListConverter, guidListComparer);
            e.HasOne(ra => ra.Driver)
             .WithMany(dp => dp.RouteAssignments)
             .HasForeignKey(ra => ra.DriverId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(ra => ra.Zone)
             .WithMany(dz => dz.RouteAssignments)
             .HasForeignKey(ra => ra.ZoneId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(ra => ra.CreatedByUser)
             .WithMany()
             .HasForeignKey(ra => ra.CreatedBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(ra => ra.UpdatedByUser)
             .WithMany()
             .HasForeignKey(ra => ra.UpdatedBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.NoAction);
            e.Property(ra => ra.CreatedAt).HasDefaultValueSql("now()");
            e.Property(ra => ra.UpdatedAt).HasDefaultValueSql("now()");
        });

        // DeliverySchedule
        mb.Entity<DeliverySchedule>(e =>
        {
            e.HasKey(ds => ds.Id);
            e.Property(ds => ds.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(ds => new { ds.SubscriptionId, ds.ScheduledDate }).IsUnique();
            e.HasIndex(ds => new { ds.TenantId, ds.ScheduledDate, ds.Status });
            e.Property(ds => ds.Status).IsRequired().HasMaxLength(20).HasDefaultValue("pending");
            e.HasOne(ds => ds.Subscription)
             .WithMany(s => s.DeliverySchedules)
             .HasForeignKey(ds => ds.SubscriptionId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ds => ds.RouteAssignment)
             .WithMany(ra => ra.DeliverySchedules)
             .HasForeignKey(ds => ds.RouteAssignmentId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(ds => ds.Driver)
             .WithMany(dp => dp.DeliverySchedules)
             .HasForeignKey(ds => ds.DriverId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(ds => ds.CreatedByUser)
             .WithMany()
             .HasForeignKey(ds => ds.CreatedBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(ds => ds.UpdatedByUser)
             .WithMany()
             .HasForeignKey(ds => ds.UpdatedBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.NoAction);
            e.Property(ds => ds.CreatedAt).HasDefaultValueSql("now()");
            e.Property(ds => ds.UpdatedAt).HasDefaultValueSql("now()");
        });

        // DailyPackingSummary
        mb.Entity<DailyPackingSummary>(e =>
        {
            e.HasKey(dp => dp.Id);
            e.Property(dp => dp.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(dp => new { dp.TenantId, dp.SummaryDate, dp.MenuItemId, dp.PortionSize }).IsUnique();
            e.Property(dp => dp.PortionSize).IsRequired().HasMaxLength(50);
            e.Property(dp => dp.TotalQuantity).HasPrecision(8, 2);
            e.HasOne(dp => dp.MenuItem)
             .WithMany(mi => mi.DailyPackingSummaries)
             .HasForeignKey(dp => dp.MenuItemId)
             .OnDelete(DeleteBehavior.Restrict);
            e.Property(dp => dp.GeneratedAt).HasDefaultValueSql("now()");
        });

        // DriverLocation
        mb.Entity<DriverLocation>(e =>
        {
            e.HasKey(dl => dl.Id);
            e.Property(dl => dl.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(dl => new { dl.DriverId, dl.RecordedAt });
            e.HasIndex(dl => dl.RouteAssignmentId);
            e.Property(dl => dl.Lat).HasPrecision(10, 7);
            e.Property(dl => dl.Lng).HasPrecision(10, 7);
            e.Property(dl => dl.AccuracyMetres).HasPrecision(6, 2);
            e.HasOne(dl => dl.Driver)
             .WithMany(dp => dp.DriverLocations)
             .HasForeignKey(dl => dl.DriverId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(dl => dl.RouteAssignment)
             .WithMany(ra => ra.DriverLocations)
             .HasForeignKey(dl => dl.RouteAssignmentId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // PODRecord
        mb.Entity<PODRecord>(e =>
        {
            e.HasKey(pr => pr.Id);
            e.Property(pr => pr.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(pr => pr.DeliveryId).IsUnique();
            e.Property(pr => pr.PhotoUrl).IsRequired();
            e.Property(pr => pr.Status).IsRequired().HasMaxLength(20).HasDefaultValue("uploaded");
            e.Property(pr => pr.CaptureLat).HasPrecision(10, 7);
            e.Property(pr => pr.CaptureLng).HasPrecision(10, 7);
            e.HasOne(pr => pr.Delivery)
             .WithOne(ds => ds.PODRecord)
             .HasForeignKey<PODRecord>(pr => pr.DeliveryId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // DriverPayoutRecord
        mb.Entity<DriverPayoutRecord>(e =>
        {
            e.HasKey(dr => dr.Id);
            e.Property(dr => dr.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(dr => new { dr.DriverId, dr.PayoutDate }).IsUnique();
            e.Property(dr => dr.PayoutType).IsRequired().HasMaxLength(20);
            e.Property(dr => dr.Status).IsRequired().HasMaxLength(20).HasDefaultValue("pending");
            e.Property(dr => dr.BaseAmount).HasPrecision(10, 2);
            e.Property(dr => dr.BonusAmount).HasPrecision(10, 2);
            e.Property(dr => dr.TotalAmount).HasPrecision(10, 2);
            e.Property(dr => dr.TotalDistanceKm).HasPrecision(8, 2);
            e.HasOne(dr => dr.Driver)
             .WithMany(dp => dp.PayoutRecords)
             .HasForeignKey(dr => dr.DriverId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(dr => dr.Policy)
             .WithMany(p => p.PayoutRecords)
             .HasForeignKey(dr => dr.PolicyId)
             .OnDelete(DeleteBehavior.Restrict);
            e.Property(dr => dr.CreatedAt).HasDefaultValueSql("now()");
        });

        // DriverPayoutSettlement
        mb.Entity<DriverPayoutSettlement>(e =>
        {
            e.HasKey(ds => ds.Id);
            e.Property(ds => ds.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(ds => new { ds.DriverId, ds.PeriodStart, ds.PeriodEnd });
            e.Property(ds => ds.Status).IsRequired().HasMaxLength(20).HasDefaultValue("pending");
            e.Property(ds => ds.TotalAmount).HasPrecision(10, 2);
            e.Property(ds => ds.PaymentMethod).HasMaxLength(30);
            e.Property(ds => ds.PaymentReference).HasMaxLength(255);
            e.HasOne(ds => ds.Driver)
             .WithMany(dp => dp.PayoutSettlements)
             .HasForeignKey(ds => ds.DriverId)
             .OnDelete(DeleteBehavior.Restrict);
            e.Property(ds => ds.CreatedAt).HasDefaultValueSql("now()");
        });
    }

    // ── INVENTORY CONFIGURATION ───────────────────────────────
    private static void ConfigureInventory(ModelBuilder mb)
    {
        // InventoryItem
        mb.Entity<InventoryItem>(e =>
        {
            e.HasKey(ii => ii.Id);
            e.Property(ii => ii.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(ii => new { ii.TenantId, ii.Name }).IsUnique();
            e.Property(ii => ii.Name).IsRequired().HasMaxLength(150);
            e.Property(ii => ii.Unit).IsRequired().HasMaxLength(20);
            e.Property(ii => ii.Category).IsRequired().HasMaxLength(100);
            e.Property(ii => ii.ReorderThreshold).HasPrecision(10, 3);
            e.Property(ii => ii.CreatedAt).HasDefaultValueSql("now()");
        });

        // Supplier
        mb.Entity<Supplier>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(s => s.Name).IsRequired().HasMaxLength(150);
            e.Property(s => s.Phone).IsRequired().HasMaxLength(20);
            e.Property(s => s.WhatsappNumber).HasMaxLength(20);
            e.Property(s => s.Email).HasMaxLength(255);
            e.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
        });

        // ItemSupplier — composite PK
        mb.Entity<ItemSupplier>(e =>
        {
            e.HasKey(is_ => new { is_.ItemId, is_.SupplierId });
            e.HasOne(is_ => is_.Item)
             .WithMany(ii => ii.ItemSuppliers)
             .HasForeignKey(is_ => is_.ItemId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(is_ => is_.Supplier)
             .WithMany(s => s.ItemSuppliers)
             .HasForeignKey(is_ => is_.SupplierId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // StockLog
        mb.Entity<StockLog>(e =>
        {
            e.HasKey(sl => sl.Id);
            e.Property(sl => sl.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(sl => new { sl.TenantId, sl.ItemId, sl.LogDate }).IsUnique();
            e.Property(sl => sl.OpeningQty).HasPrecision(10, 3);
            e.Property(sl => sl.ClosingQty).HasPrecision(10, 3);
            e.Property(sl => sl.PurchasedQty).HasPrecision(10, 3);
            e.HasOne(sl => sl.Item)
             .WithMany(ii => ii.StockLogs)
             .HasForeignKey(sl => sl.ItemId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(sl => sl.CreatedByUser)
             .WithMany()
             .HasForeignKey(sl => sl.CreatedBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(sl => sl.UpdatedByUser)
             .WithMany()
             .HasForeignKey(sl => sl.UpdatedBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.NoAction);
            e.Property(sl => sl.CreatedAt).HasDefaultValueSql("now()");
            e.Property(sl => sl.UpdatedAt).HasDefaultValueSql("now()");
        });

        // PurchaseOrder
        mb.Entity<PurchaseOrder>(e =>
        {
            e.HasKey(po => po.Id);
            e.Property(po => po.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(po => new { po.TenantId, po.Status });
            e.Property(po => po.Status).IsRequired().HasMaxLength(20).HasDefaultValue("draft");
            e.Property(po => po.TotalCost).HasPrecision(12, 2);
            e.HasOne(po => po.Supplier)
             .WithMany(s => s.PurchaseOrders)
             .HasForeignKey(po => po.SupplierId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(po => po.CreatedByUser)
             .WithMany()
             .HasForeignKey(po => po.CreatedBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(po => po.UpdatedByUser)
             .WithMany()
             .HasForeignKey(po => po.UpdatedBy)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.NoAction);
            e.Property(po => po.CreatedAt).HasDefaultValueSql("now()");
            e.Property(po => po.UpdatedAt).HasDefaultValueSql("now()");
        });

        // PurchaseOrderItem
        mb.Entity<PurchaseOrderItem>(e =>
        {
            e.HasKey(poi => poi.Id);
            e.Property(poi => poi.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(poi => poi.Quantity).HasPrecision(10, 3);
            e.Property(poi => poi.UnitPrice).HasPrecision(10, 2);
            e.HasOne(poi => poi.Order)
             .WithMany(po => po.Items)
             .HasForeignKey(poi => poi.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(poi => poi.Item)
             .WithMany(ii => ii.PurchaseOrderItems)
             .HasForeignKey(poi => poi.ItemId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }

    // ── PAYMENTS CONFIGURATION ────────────────────────────────
    private static void ConfigurePayments(ModelBuilder mb)
    {
        mb.Entity<PaymentTransaction>(e =>
        {
            e.HasKey(pt => pt.Id);
            e.Property(pt => pt.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(pt => pt.GatewayRef).IsUnique();
            e.HasIndex(pt => pt.IdempotencyKey).IsUnique();
            e.HasIndex(pt => new { pt.TenantId, pt.Status });
            e.Property(pt => pt.Status).IsRequired().HasMaxLength(20).HasDefaultValue("pending");
            e.Property(pt => pt.Currency).HasMaxLength(3).HasDefaultValue("INR");
            e.Property(pt => pt.Gateway).IsRequired().HasMaxLength(20);
            e.Property(pt => pt.IdempotencyKey).IsRequired();
            e.Property(pt => pt.Amount).HasPrecision(12, 2);
            e.HasOne(pt => pt.Subscription)
             .WithMany(s => s.PaymentTransactions)
             .HasForeignKey(pt => pt.SubscriptionId)
             .OnDelete(DeleteBehavior.Restrict);
            e.Property(pt => pt.CreatedAt).HasDefaultValueSql("now()");
        });
    }

    // ── SEED DATA ─────────────────────────────────────────────
    private static void SeedPermissions(ModelBuilder mb)
    {
        var now = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var permissions = new[]
        {
            // Platform
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "Manage Tenants", Slug = "platform:tenants:manage", Module = "platform", CreatedAt = now },
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = "Manage Billing", Slug = "platform:billing:manage", Module = "platform", CreatedAt = now },

            // Tiffin — Plans
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), Name = "Read Plans", Slug = "tiffin:plans:read", Module = "tiffin", CreatedAt = now },
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000004"), Name = "Write Plans", Slug = "tiffin:plans:write", Module = "tiffin", CreatedAt = now },
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000005"), Name = "Archive Plans", Slug = "tiffin:plans:archive", Module = "tiffin", CreatedAt = now },

            // Tiffin — Subscriptions
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000006"), Name = "Read All Subscriptions", Slug = "tiffin:subscriptions:read:all", Module = "tiffin", CreatedAt = now },
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000007"), Name = "Read Own Subscription", Slug = "tiffin:subscriptions:read:own", Module = "tiffin", CreatedAt = now },
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000008"), Name = "Write Subscriptions", Slug = "tiffin:subscriptions:write", Module = "tiffin", CreatedAt = now },
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000009"), Name = "Cancel Subscriptions", Slug = "tiffin:subscriptions:cancel", Module = "tiffin", CreatedAt = now },

            // Tiffin — Deliveries
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000010"), Name = "Read Deliveries", Slug = "tiffin:deliveries:read", Module = "tiffin", CreatedAt = now },
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000011"), Name = "Assign Deliveries", Slug = "tiffin:deliveries:assign", Module = "tiffin", CreatedAt = now },
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000012"), Name = "Update Delivery Status", Slug = "tiffin:deliveries:status:update", Module = "tiffin", CreatedAt = now },
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000013"), Name = "Upload POD", Slug = "tiffin:deliveries:pod:upload", Module = "tiffin", CreatedAt = now },
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000014"), Name = "Track Own Delivery", Slug = "tiffin:deliveries:track:own", Module = "tiffin", CreatedAt = now },

            // Tiffin — Drivers
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000015"), Name = "Manage Drivers", Slug = "tiffin:drivers:manage", Module = "tiffin", CreatedAt = now },
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000016"), Name = "View Driver Location", Slug = "tiffin:drivers:location:view", Module = "tiffin", CreatedAt = now },

            // Tiffin — Routes
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000017"), Name = "View Routes", Slug = "tiffin:routes:view", Module = "tiffin", CreatedAt = now },

            // Tiffin — Packing
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000018"), Name = "View Packing Summary", Slug = "tiffin:packing:summary:view", Module = "tiffin", CreatedAt = now },
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000019"), Name = "Confirm Packing Summary", Slug = "tiffin:packing:summary:confirm", Module = "tiffin", CreatedAt = now },
            // Tiffin — Reports
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000020"), Name = "View Delivery Reports", Slug = "tiffin:reports:delivery", Module = "tiffin", CreatedAt = now },
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000021"), Name = "View Revenue Reports", Slug = "tiffin:reports:revenue", Module = "tiffin", CreatedAt = now },

            // Inventory
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000022"), Name = "Manage Inventory Items", Slug = "inventory:items:manage", Module = "inventory", CreatedAt = now },
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000023"), Name = "Log Stock", Slug = "inventory:stock:log", Module = "inventory", CreatedAt = now },
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000024"), Name = "View Stock", Slug = "inventory:stock:view", Module = "inventory", CreatedAt = now },
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000025"), Name = "Manage Suppliers", Slug = "inventory:suppliers:manage", Module = "inventory", CreatedAt = now },
            new Permission { Id = Guid.Parse("00000000-0000-0000-0000-000000000026"), Name = "Manage Purchase Orders", Slug = "inventory:orders:manage", Module = "inventory", CreatedAt = now },
        };

        mb.Entity<Permission>().HasData(permissions);
    }

    private static void SeedRoles(ModelBuilder mb)
    {
        var now = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var roles = new[]
        {
            new Role { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = "Super Admin", Slug = "super_admin", IsSystemRole = true, Description = "Full platform access", TenantId = null, CreatedAt = now },
            new Role { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = "Tenant Admin", Slug = "tenant_admin", IsSystemRole = true, Description = "Full access within their tenant", TenantId = null, CreatedAt = now },
            new Role { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Name = "Manager", Slug = "manager", IsSystemRole = true, Description = "Operational access, no billing", TenantId = null, CreatedAt = now },
            new Role { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), Name = "Cook", Slug = "cook", IsSystemRole = true, Description = "Kitchen packing access only", TenantId = null, CreatedAt = now },
            new Role { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), Name = "Driver", Slug = "driver", IsSystemRole = true, Description = "Delivery route and POD only", TenantId = null, CreatedAt = now },
            new Role { Id = Guid.Parse("10000000-0000-0000-0000-000000000006"), Name = "Customer", Slug = "customer", IsSystemRole = true, Description = "Own subscription and tracking only", TenantId = null, CreatedAt = now },
        };

        mb.Entity<Role>().HasData(roles);
    }

    private static void SeedRolePermissions(ModelBuilder mb)
    {
        // Role GUIDs
        var superAdmin = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var tenantAdmin = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var manager = Guid.Parse("10000000-0000-0000-0000-000000000003");
        var cook = Guid.Parse("10000000-0000-0000-0000-000000000004");
        var driver = Guid.Parse("10000000-0000-0000-0000-000000000005");
        var customer = Guid.Parse("10000000-0000-0000-0000-000000000006");

        // Permission GUIDs
        var p = (int n) => Guid.Parse($"00000000-0000-0000-0000-{n:D12}");

        var mappings = new List<RolePermission>();

        // Super Admin — all permissions
        for (int i = 1; i <= 26; i++)
            mappings.Add(new RolePermission { RoleId = superAdmin, PermissionId = p(i) });

        // Tenant Admin — all except platform
        for (int i = 3; i <= 26; i++)
            mappings.Add(new RolePermission { RoleId = tenantAdmin, PermissionId = p(i) });

        // Manager
        int[] managerPerms = [3, 4, 6, 8, 9, 10, 11, 12, 15, 16, 17, 18, 19, 20, 22, 23, 24, 26];
        foreach (var perm in managerPerms)
            mappings.Add(new RolePermission { RoleId = manager, PermissionId = p(perm) });

        // Cook
        int[] cookPerms = [18, 19, 23, 24];
        foreach (var perm in cookPerms)
            mappings.Add(new RolePermission { RoleId = cook, PermissionId = p(perm) });

        // Driver
        int[] driverPerms = [10, 12, 13, 17];
        foreach (var perm in driverPerms)
            mappings.Add(new RolePermission { RoleId = driver, PermissionId = p(perm) });

        // Customer
        int[] customerPerms = [3, 7, 8, 9, 14, 16];
        foreach (var perm in customerPerms)
            mappings.Add(new RolePermission { RoleId = customer, PermissionId = p(perm) });

        mb.Entity<RolePermission>().HasData(mappings);
    }
}