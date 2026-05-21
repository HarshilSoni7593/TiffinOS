using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiffinOS.API.Data;
using TiffinOS.API.DTOs.Tiffin;
using TiffinOS.API.Middleware;
using TiffinOS.API.Models.Tiffin;
using TiffinOS.API.Services;

namespace TiffinOS.API.Controllers;

[ApiController]
[Route("api/routes")]
[Authorize]
[RequireModule("tiffin")]
public class RouteAssignmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;
    private readonly CurrentUserContext _user;
    private readonly RouteOptimisationService _optimiser;

    public RouteAssignmentsController(
        AppDbContext db,
        TenantContext tenant,
        CurrentUserContext user,
        RouteOptimisationService optimiser)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
        _optimiser = optimiser;
    }

    // ── ASSIGN DRIVER TO ZONE ─────────────────────────────────
    // Why: Admin picks a driver and a zone. System takes all
    // pending deliveries in that zone, runs the optimisation
    // algorithm, creates a route_assignment row, and updates
    // every delivery_schedule row with the driver_id and
    // sequence_number. Driver immediately sees their route.
    [HttpPost("assign")]
    [RequirePermission("tiffin:deliveries:assign")]
    public async Task<IActionResult> AssignDriver(
        [FromBody] AssignDriverRequest request)
    {
        // Validate driver belongs to this tenant
        var driver = await _db.DriverProfiles
            .Include(d => d.User)
            .FirstOrDefaultAsync(d =>
                d.TenantId == _tenant.TenantId &&
                d.Id == request.DriverId &&
                d.IsAvailable);

        if (driver == null)
            return NotFound(new
            {
                error = "Driver not found or unavailable.",
                code = "DRIVER_NOT_FOUND"
            });

        // Validate zone
        var zone = await _db.DeliveryZones
            .FirstOrDefaultAsync(z =>
                z.TenantId == _tenant.TenantId &&
                z.Id == request.ZoneId &&
                z.IsActive);

        if (zone == null)
            return NotFound(new
            {
                error = "Zone not found.",
                code = "ZONE_NOT_FOUND"
            });

        // Check driver not already assigned to this zone today
        var alreadyAssigned = await _db.RouteAssignments
            .AnyAsync(ra =>
                ra.DriverId == request.DriverId &&
                ra.ZoneId == request.ZoneId &&
                ra.AssignmentDate == request.AssignmentDate &&
                ra.Status != "cancelled");

        if (alreadyAssigned)
            return BadRequest(new
            {
                error = "Driver is already assigned to this " +
                        "zone on this date.",
                code = "ALREADY_ASSIGNED"
            });

        // Get all pending deliveries in this zone for this date
        var pendingDeliveries = await _db.DeliverySchedules
            .Include(ds => ds.Subscription)
            .Where(ds =>
                ds.TenantId == _tenant.TenantId &&
                ds.ScheduledDate == request.AssignmentDate &&
                ds.Subscription!.ZoneId == request.ZoneId &&
                ds.Status == "pending" &&
                ds.DriverId == null)
            .ToListAsync();

        if (!pendingDeliveries.Any())
            return BadRequest(new
            {
                error = "No pending unassigned deliveries in " +
                        "this zone for this date.",
                code = "NO_PENDING_DELIVERIES"
            });

        // Build stop list for optimisation
        var stops = pendingDeliveries.Select(ds => new DeliveryStop(
            ds.Id,
            (double)ds.Subscription!.DeliveryLat,
            (double)ds.Subscription!.DeliveryLng
        )).ToList();

        // Run route optimisation
        var optimisedSequence = _optimiser.OptimiseRoute(stops);

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // Create route assignment
            var routeAssignment = new RouteAssignment
            {
                Id = Guid.NewGuid(),
                TenantId = _tenant.TenantId,
                DriverId = request.DriverId,
                ZoneId = request.ZoneId,
                AssignmentDate = request.AssignmentDate,
                OptimisedSequence = optimisedSequence,
                TotalStops = pendingDeliveries.Count,
                Status = "pending",
                CreatedBy = _user.UserId,
                UpdatedBy = _user.UserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.RouteAssignments.Add(routeAssignment);
            await _db.SaveChangesAsync();

            // Update each delivery schedule with driver
            // and sequence number
            for (int i = 0; i < optimisedSequence.Count; i++)
            {
                var scheduleId = optimisedSequence[i];
                var schedule = pendingDeliveries
                    .First(ds => ds.Id == scheduleId);

                schedule.DriverId = request.DriverId;
                schedule.RouteAssignmentId = routeAssignment.Id;
                schedule.SequenceNumber = i + 1;
                schedule.Status = "assigned";
                schedule.UpdatedBy = _user.UserId;
                schedule.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                message = $"Route assigned successfully. " +
                                   $"{pendingDeliveries.Count} stops " +
                                   $"optimised for {driver.User.FirstName} " +
                                   $"{driver.User.LastName}.",
                routeAssignmentId = routeAssignment.Id,
                driverName = $"{driver.User.FirstName} " +
                                    $"{driver.User.LastName}",
                totalStops = pendingDeliveries.Count,
                zone = zone.Name
            });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ── GET ROUTE ASSIGNMENT ──────────────────────────────────
    // Admin sees full route with all stops in order.
    // Driver uses this to navigate their deliveries.
    [HttpGet("{id:guid}")]
    [RequirePermission("tiffin:routes:view")]
    public async Task<IActionResult> GetRoute(Guid id)
    {
        var assignment = await _db.RouteAssignments
            .AsNoTracking()
            .Include(ra => ra.Driver)
                .ThenInclude(d => d!.User)
            .Include(ra => ra.Zone)
            .FirstOrDefaultAsync(ra =>
                ra.TenantId == _tenant.TenantId &&
                ra.Id == id);

        if (assignment == null)
            return NotFound(new
            {
                error = "Route assignment not found.",
                code = "ROUTE_NOT_FOUND"
            });

        return Ok(await BuildRouteResponse(assignment));
    }

    // ── GET TODAY'S ROUTES ────────────────────────────────────
    [HttpGet]
    [RequirePermission("tiffin:routes:view")]
    public async Task<IActionResult> GetTodaysRoutes(
        [FromQuery] DateOnly? date = null)
    {
        var targetDate = date ??
            DateOnly.FromDateTime(DateTime.UtcNow);

        var assignments = await _db.RouteAssignments
            .AsNoTracking()
            .Include(ra => ra.Driver)
                .ThenInclude(d => d!.User)
            .Include(ra => ra.Zone)
            .Where(ra =>
                ra.TenantId == _tenant.TenantId &&
                ra.AssignmentDate == targetDate)
            .OrderBy(ra => ra.Zone!.Name)
            .ToListAsync();

        var results = new List<RouteAssignmentResponse>();
        foreach (var a in assignments)
            results.Add(await BuildRouteResponse(a));

        return Ok(results);
    }

    // ── START ROUTE ───────────────────────────────────────────
    // Driver taps "Start Route" in the app.
    // Records the time they began deliveries.
    [HttpPost("{id:guid}/start")]
    [RequirePermission("tiffin:deliveries:status:update")]
    public async Task<IActionResult> StartRoute(Guid id)
    {
        var assignment = await _db.RouteAssignments
            .FirstOrDefaultAsync(ra =>
                ra.TenantId == _tenant.TenantId &&
                ra.Id == id);

        if (assignment == null)
            return NotFound(new
            {
                error = "Route not found.",
                code = "ROUTE_NOT_FOUND"
            });

        if (assignment.Status != "pending")
            return BadRequest(new
            {
                error = "Route is not in pending status.",
                code = "INVALID_STATUS"
            });

        // Ensure only the assigned driver can start their route
        var driver = await _db.DriverProfiles
            .FirstOrDefaultAsync(d =>
                d.UserId == _user.UserId &&
                d.TenantId == _tenant.TenantId);

        if (driver == null || assignment.DriverId != driver.Id)
            if (!_user.HasRole("tenant_admin") &&
                !_user.HasRole("manager"))
                return Forbid();

        assignment.Status = "in_progress";
        assignment.StartedAt = DateTime.UtcNow;
        assignment.UpdatedBy = _user.UserId;
        assignment.UpdatedAt = DateTime.UtcNow;

        // Mark all assigned deliveries as in_progress
        var schedules = await _db.DeliverySchedules
            .Where(ds => ds.RouteAssignmentId == id)
            .ToListAsync();

        foreach (var s in schedules)
            if (s.Status == "assigned")
                s.Status = "in_progress";

        await _db.SaveChangesAsync();

        return Ok(new { message = "Route started." });
    }

    // ── UPDATE DELIVERY STATUS ────────────────────────────────
    // Driver marks each stop as delivered or attempted.
    // This is the core loop of the driver's workflow —
    // they tap Delivered or Attempted at each stop.
    [HttpPost("deliveries/{scheduleId:guid}/status")]
    [RequirePermission("tiffin:deliveries:status:update")]
    public async Task<IActionResult> UpdateDeliveryStatus(
        Guid scheduleId,
        [FromBody] UpdateDeliveryStatusRequest request)
    {
        var schedule = await _db.DeliverySchedules
            .Include(ds => ds.RouteAssignment)
            .FirstOrDefaultAsync(ds =>
                ds.TenantId == _tenant.TenantId &&
                ds.Id == scheduleId);

        if (schedule == null)
            return NotFound(new
            {
                error = "Delivery not found.",
                code = "DELIVERY_NOT_FOUND"
            });

        if (schedule.Status == "delivered")
            return BadRequest(new
            {
                error = "Delivery is already marked as delivered.",
                code = "ALREADY_DELIVERED"
            });

        var validStatuses = new[] { "delivered", "attempted" };
        if (!validStatuses.Contains(request.Status))
            return BadRequest(new
            {
                error = "Status must be 'delivered' or 'attempted'.",
                code = "INVALID_STATUS"
            });

        schedule.Status = request.Status;
        schedule.FailureReason = request.FailureReason;
        schedule.UpdatedBy = _user.UserId;
        schedule.UpdatedAt = DateTime.UtcNow;

        if (request.Status == "delivered")
            schedule.DeliveredAt = DateTime.UtcNow;
        else
            schedule.AttemptedAt = DateTime.UtcNow;

        // Check if all stops in this route are complete
        var allStops = await _db.DeliverySchedules
            .Where(ds => ds.RouteAssignmentId ==
                         schedule.RouteAssignmentId)
            .ToListAsync();

        var allDone = allStops.All(s =>
            s.Status == "delivered" ||
            s.Status == "attempted" ||
            s.Status == "skipped");

        if (allDone && schedule.RouteAssignment != null)
        {
            schedule.RouteAssignment.Status = "completed";
            schedule.RouteAssignment.CompletedAt = DateTime.UtcNow;
            schedule.RouteAssignment.UpdatedBy = _user.UserId;
            schedule.RouteAssignment.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = $"Delivery marked as {request.Status}.",
            scheduleId,
            status = request.Status,
            routeCompleted = allDone
        });
    }

    // ── PRIVATE HELPER ────────────────────────────────────────
    private async Task<RouteAssignmentResponse> BuildRouteResponse(
        RouteAssignment assignment)
    {
        var schedules = await _db.DeliverySchedules
            .AsNoTracking()
            .Include(ds => ds.Subscription)
                .ThenInclude(s => s!.Plan)
            .Where(ds => ds.RouteAssignmentId == assignment.Id)
            .OrderBy(ds => ds.SequenceNumber)
            .ToListAsync();

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

        var podIds = await _db.PODRecords
            .Where(p => schedules
                .Select(s => s.Id).Contains(p.DeliveryId))
            .Select(p => p.DeliveryId)
            .ToListAsync();

        var stops = schedules.Select(ds =>
        {
            var customer = customers.FirstOrDefault(
                c => c.Id == ds.Subscription!.CustomerId);
            return new RouteStopResponse(
                ds.Id,
                ds.SequenceNumber ?? 0,
                customer?.FullName ?? "Unknown",
                customer?.Phone,
                ds.Subscription!.DeliveryAddress,
                ds.Subscription.FloorOrUnit,
                ds.Subscription.DeliveryInstructions,
                ds.Subscription.SpicePreference,
                ds.Subscription.Plan!.Name,
                ds.Subscription.DeliveryLat,
                ds.Subscription.DeliveryLng,
                ds.Status,
                ds.DeliveredAt,
                ds.AttemptedAt,
                ds.FailureReason,
                podIds.Contains(ds.Id)
            );
        }).ToList();

        var completedStops = stops.Count(s => s.Status == "delivered");
        var attemptedStops = stops.Count(s => s.Status == "attempted");

        return new RouteAssignmentResponse(
            assignment.Id,
            assignment.DriverId,
            $"{assignment.Driver!.User.FirstName} " +
            $"{assignment.Driver.User.LastName}",
            assignment.ZoneId,
            assignment.Zone!.Name,
            assignment.AssignmentDate,
            assignment.TotalStops,
            completedStops,
            attemptedStops,
            assignment.Status,
            assignment.ActualDistanceKm,
            assignment.StartedAt,
            assignment.CompletedAt,
            stops
        );
    }
}