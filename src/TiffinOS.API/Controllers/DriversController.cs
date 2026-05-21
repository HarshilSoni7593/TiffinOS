using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiffinOS.API.Data;
using TiffinOS.API.DTOs.Tiffin;
using TiffinOS.API.Middleware;
using TiffinOS.API.Models.Common;
using TiffinOS.API.Models.Tiffin;
using TiffinOS.API.Services;
using TiffinOS.API.Services.Interfaces;

namespace TiffinOS.API.Controllers;

[ApiController]
[Route("api/drivers")]
[Authorize]
[RequireModule("tiffin")]
public class DriversController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;
    private readonly CurrentUserContext _user;
    private readonly IAuthService _auth;

    public DriversController(
        AppDbContext db,
        TenantContext tenant,
        CurrentUserContext user,
        IAuthService auth)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
        _auth = auth;
    }

    // ══════════════════════════════════════════════════════════
    // PAYOUT POLICIES
    // Why: Admin must create payout policies before creating
    // drivers. A driver without a policy cannot have their
    // daily earnings calculated automatically.
    // ══════════════════════════════════════════════════════════

    [HttpGet("payout-policies")]
    [RequirePermission("tiffin:drivers:manage")]
    public async Task<IActionResult> GetPayoutPolicies()
    {
        var policies = await _db.DriverPayoutPolicies
            .AsNoTracking()
            .Where(p => p.TenantId == _tenant.TenantId)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var driverCounts = await _db.DriverProfiles
            .Where(d => d.TenantId == _tenant.TenantId &&
                        d.PayoutPolicyId.HasValue)
            .GroupBy(d => d.PayoutPolicyId)
            .Select(g => new { PolicyId = g.Key, Count = g.Count() })
            .ToListAsync();

        var response = policies.Select(p => new PayoutPolicyResponse(
            p.Id,
            p.Name,
            p.PayoutType,
            p.BaseRate,
            p.BonusPerDelivery,
            p.BonusThreshold,
            p.MinGuaranteed,
            p.Currency,
            p.IsActive,
            driverCounts
                .FirstOrDefault(dc => dc.PolicyId == p.Id)?.Count ?? 0
        )).ToList();

        return Ok(response);
    }

    [HttpPost("payout-policies")]
    [RequirePermission("tiffin:drivers:manage")]
    public async Task<IActionResult> CreatePayoutPolicy(
        [FromBody] CreatePayoutPolicyRequest request)
    {
        // Validate hybrid type has required fields
        if (request.PayoutType == "hybrid")
        {
            if (!request.BonusPerDelivery.HasValue ||
                !request.BonusThreshold.HasValue)
                return BadRequest(new
                {
                    error = "Hybrid payout type requires " +
                            "BonusPerDelivery and BonusThreshold.",
                    code = "HYBRID_FIELDS_REQUIRED"
                });
        }

        var policy = new DriverPayoutPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            Name = request.Name.Trim(),
            PayoutType = request.PayoutType,
            BaseRate = request.BaseRate,
            BonusPerDelivery = request.BonusPerDelivery,
            BonusThreshold = request.BonusThreshold,
            MinGuaranteed = request.MinGuaranteed,
            Currency = request.Currency,
            IsActive = true,
            CreatedBy = _user.UserId,
            UpdatedBy = _user.UserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.DriverPayoutPolicies.Add(policy);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Payout policy created successfully.",
            policyId = policy.Id,
            name = policy.Name
        });
    }

    [HttpPut("payout-policies/{id:guid}")]
    [RequirePermission("tiffin:drivers:manage")]
    public async Task<IActionResult> UpdatePayoutPolicy(
        Guid id,
        [FromBody] UpdatePayoutPolicyRequest request)
    {
        var policy = await _db.DriverPayoutPolicies
            .FirstOrDefaultAsync(p =>
                p.TenantId == _tenant.TenantId && p.Id == id);

        if (policy == null)
            return NotFound(new
            {
                error = "Payout policy not found.",
                code = "POLICY_NOT_FOUND"
            });

        policy.Name = request.Name.Trim();
        policy.BaseRate = request.BaseRate;
        policy.BonusPerDelivery = request.BonusPerDelivery;
        policy.BonusThreshold = request.BonusThreshold;
        policy.MinGuaranteed = request.MinGuaranteed;
        policy.IsActive = request.IsActive;
        policy.UpdatedBy = _user.UserId;
        policy.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Payout policy updated successfully." });
    }

    // ══════════════════════════════════════════════════════════
    // DRIVERS
    // Why creating a driver also creates a user account:
    // A driver needs to log into the driver app. So creating
    // a driver profile also registers them as a user with the
    // 'driver' role. Admin provides their details — the driver
    // receives login credentials.
    // ══════════════════════════════════════════════════════════

    [HttpGet]
    [RequirePermission("tiffin:drivers:manage")]
    public async Task<IActionResult> GetDrivers(
        [FromQuery] bool includeInactive = false)
    {
        var query = _db.DriverProfiles
            .AsNoTracking()
            .Where(d => d.TenantId == _tenant.TenantId);

        if (!includeInactive)
            query = query.Where(d => d.IsAvailable || d.IsAvailable);

        var drivers = await query
            .Include(d => d.User)
            .Include(d => d.PayoutPolicy)
            .OrderBy(d => d.User.FirstName)
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var deliveryCounts = await _db.DeliverySchedules
            .Where(ds =>
                ds.TenantId == _tenant.TenantId &&
                ds.ScheduledDate == today &&
                ds.DriverId != null)
            .GroupBy(ds => ds.DriverId)
            .Select(g => new { DriverId = g.Key, Count = g.Count() })
            .ToListAsync();

        var response = drivers.Select(d => new DriverListResponse(
            d.Id,
            d.UserId,
            $"{d.User.FirstName} {d.User.LastName}",
            d.VehicleType,
            d.IsAvailable,
            d.IsAvailable,
            d.PayoutPolicy?.Name,
            deliveryCounts
                .FirstOrDefault(dc => dc.DriverId == d.Id)?.Count ?? 0
        )).ToList();

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("tiffin:drivers:manage")]
    public async Task<IActionResult> GetDriver(Guid id)
    {
        var driver = await _db.DriverProfiles
            .AsNoTracking()
            .Include(d => d.User)
            .Include(d => d.PayoutPolicy)
            .Where(d =>
                d.TenantId == _tenant.TenantId && d.Id == id)
            .FirstOrDefaultAsync();

        if (driver == null)
            return NotFound(new
            {
                error = "Driver not found.",
                code = "DRIVER_NOT_FOUND"
            });

        return Ok(MapToResponse(driver));
    }

    [HttpPost]
    [RequirePermission("tiffin:drivers:manage")]
    public async Task<IActionResult> CreateDriver(
        [FromBody] CreateDriverRequest request)
    {
        // Validate payout policy if provided
        if (request.PayoutPolicyId.HasValue)
        {
            var policyExists = await _db.DriverPayoutPolicies
                .AnyAsync(p =>
                    p.Id == request.PayoutPolicyId &&
                    p.TenantId == _tenant.TenantId &&
                    p.IsActive);

            if (!policyExists)
                return NotFound(new
                {
                    error = "Payout policy not found or inactive.",
                    code = "POLICY_NOT_FOUND"
                });
        }

        // Check email not already used in this tenant
        var emailExists = await _db.Users
            .AnyAsync(u =>
                u.TenantId == _tenant.TenantId &&
                u.Email == request.Email.ToLower());

        if (emailExists)
            return BadRequest(new
            {
                error = "A user with this email already exists.",
                code = "EMAIL_EXISTS"
            });

        // Get driver role
        var driverRole = await _db.Roles
            .FirstOrDefaultAsync(r => r.Slug == "driver");

        if (driverRole == null)
            return StatusCode(500, new
            {
                error = "Driver role not found. " +
                        "Check seed data.",
                code = "ROLE_NOT_FOUND"
            });

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // Create user account for the driver
            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = _tenant.TenantId,
                Email = request.Email.ToLower().Trim(),
                PasswordHash = BCrypt.Net.BCrypt
                    .HashPassword(request.Password, workFactor: 12),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Phone = request.Phone.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Assign driver role
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = driverRole.Id,
                TenantId = _tenant.TenantId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = _user.UserId
            };

            _db.UserRoles.Add(userRole);

            // Create driver profile
            var driver = new DriverProfile
            {
                Id = Guid.NewGuid(),
                TenantId = _tenant.TenantId,
                UserId = user.Id,
                VehicleType = request.VehicleType,
                LicenceNumber = request.LicenceNumber?.Trim(),
                MaxDeliveriesPerDay = request.MaxDeliveriesPerDay,
                PayoutPolicyId = request.PayoutPolicyId,
                IsAvailable = true,
                CreatedBy = _user.UserId,
                UpdatedBy = _user.UserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.DriverProfiles.Add(driver);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Driver created successfully.",
                driverId = driver.Id,
                userId = user.Id,
                name = $"{user.FirstName} {user.LastName}",
                email = user.Email
            });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("tiffin:drivers:manage")]
    public async Task<IActionResult> UpdateDriver(
        Guid id,
        [FromBody] UpdateDriverRequest request)
    {
        var driver = await _db.DriverProfiles
            .Include(d => d.User)
            .FirstOrDefaultAsync(d =>
                d.TenantId == _tenant.TenantId && d.Id == id);

        if (driver == null)
            return NotFound(new
            {
                error = "Driver not found.",
                code = "DRIVER_NOT_FOUND"
            });

        if (request.PayoutPolicyId.HasValue)
        {
            var policyExists = await _db.DriverPayoutPolicies
                .AnyAsync(p =>
                    p.Id == request.PayoutPolicyId &&
                    p.TenantId == _tenant.TenantId &&
                    p.IsActive);

            if (!policyExists)
                return NotFound(new
                {
                    error = "Payout policy not found.",
                    code = "POLICY_NOT_FOUND"
                });
        }

        // Update user details
        driver.User.FirstName = request.FirstName.Trim();
        driver.User.LastName = request.LastName.Trim();
        driver.User.Phone = request.Phone.Trim();
        driver.User.UpdatedAt = DateTime.UtcNow;

        // Update driver profile
        driver.VehicleType = request.VehicleType;
        driver.LicenceNumber = request.LicenceNumber?.Trim();
        driver.MaxDeliveriesPerDay = request.MaxDeliveriesPerDay;
        driver.PayoutPolicyId = request.PayoutPolicyId;
        driver.IsAvailable = request.IsAvailable;
        driver.UpdatedBy = _user.UserId;
        driver.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Driver updated successfully." });
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("tiffin:drivers:manage")]
    public async Task<IActionResult> DeactivateDriver(Guid id)
    {
        var driver = await _db.DriverProfiles
            .Include(d => d.User)
            .FirstOrDefaultAsync(d =>
                d.TenantId == _tenant.TenantId && d.Id == id);

        if (driver == null)
            return NotFound(new
            {
                error = "Driver not found.",
                code = "DRIVER_NOT_FOUND"
            });

        // Check no active route assignment today
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var hasActiveRoute = await _db.RouteAssignments
            .AnyAsync(ra =>
                ra.DriverId == id &&
                ra.AssignmentDate == today &&
                ra.Status == "in_progress");

        if (hasActiveRoute)
            return BadRequest(new
            {
                error = "Cannot deactivate a driver with an " +
                        "active route in progress.",
                code = "DRIVER_HAS_ACTIVE_ROUTE"
            });

        driver.IsAvailable = false;
        driver.User.IsActive = false;
        driver.UpdatedBy = _user.UserId;
        driver.UpdatedAt = DateTime.UtcNow;
        driver.User.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Driver deactivated successfully." });
    }

    // ── Driver's own route view ───────────────────────────────
    // Why: Driver logs into the app and sees only their own
    // assigned deliveries for today — not other drivers' routes.
    [HttpGet("my-route")]
    [RequirePermission("tiffin:routes:view")]
    public async Task<IActionResult> GetMyRoute()
    {
        // Find driver profile for current user
        var driver = await _db.DriverProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(d =>
                d.TenantId == _tenant.TenantId &&
                d.UserId == _user.UserId);

        if (driver == null)
            return NotFound(new
            {
                error = "No driver profile found for your account.",
                code = "DRIVER_PROFILE_NOT_FOUND"
            });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var schedules = await _db.DeliverySchedules
            .AsNoTracking()
            .Include(ds => ds.Subscription)
                .ThenInclude(s => s!.Plan)
            .Where(ds =>
                ds.DriverId == driver.Id &&
                ds.ScheduledDate == today &&
                ds.Status != "skipped")
            .OrderBy(ds => ds.SequenceNumber)
            .ToListAsync();

        // Load customer details
        var customerIds = schedules
            .Select(ds => ds.Subscription!.CustomerId)
            .Distinct()
            .ToList();

        var customers = await _db.Users
            .Where(u => customerIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                FullName = u.FirstName + " " + u.LastName,
                u.Phone
            })
            .ToListAsync();

        var route = schedules.Select(ds =>
        {
            var customer = customers.FirstOrDefault(
                c => c.Id == ds.Subscription!.CustomerId);
            return new
            {
                scheduleId = ds.Id,
                sequenceNumber = ds.SequenceNumber,
                status = ds.Status,
                customerName = customer?.FullName,
                customerPhone = customer?.Phone,
                deliveryAddress = ds.Subscription!.DeliveryAddress,
                floorOrUnit = ds.Subscription.FloorOrUnit,
                deliveryInstructions = ds.Subscription.DeliveryInstructions,
                spicePreference = ds.Subscription.SpicePreference,
                planName = ds.Subscription.Plan!.Name,
                lat = ds.Subscription.DeliveryLat,
                lng = ds.Subscription.DeliveryLng,
                hasPod = false  // POD check added later
            };
        }).ToList();

        return Ok(new
        {
            driverId = driver.Id,
            date = today,
            totalStops = route.Count,
            completedStops = route.Count(r => r.status == "delivered"),
            route
        });
    }

    // ── PRIVATE MAPPER ────────────────────────────────────────
    private static DriverResponse MapToResponse(DriverProfile d) => new(
        d.Id,
        d.UserId,
        d.User.FirstName,
        d.User.LastName,
        d.User.Email,
        d.User.Phone,
        d.VehicleType,
        d.LicenceNumber,
        d.MaxDeliveriesPerDay,
        d.IsAvailable,
        d.User.IsActive,
        d.PayoutPolicy == null ? null : new PayoutPolicySummary(
            d.PayoutPolicy.Id,
            d.PayoutPolicy.Name,
            d.PayoutPolicy.PayoutType,
            d.PayoutPolicy.BaseRate),
        d.CreatedAt
    );
}