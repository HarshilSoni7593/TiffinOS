using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiffinOS.API.Data;
using TiffinOS.API.Middleware;
using TiffinOS.API.Services;
using TiffinOS.API.Services.Interfaces;

namespace TiffinOS.API.Controllers;

[ApiController]
[Route("api/delivery-engine")]
[Authorize]
[RequireModule("tiffin")]
public class DeliveryEngineController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;
    private readonly IDeliveryEngineService _engine;

    public DeliveryEngineController(
        AppDbContext db,
        TenantContext tenant,
        IDeliveryEngineService engine)
    {
        _db = db;
        _tenant = tenant;
        _engine = engine;
    }

    // ── GET TODAY'S PACKING SUMMARY ───────────────────────────
    // What cook sees when they open the app
    [HttpGet("packing-summary")]
    [RequirePermission("tiffin:packing:summary:view")]
    public async Task<IActionResult> GetPackingSummary(
        [FromQuery] DateOnly? date = null)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var summaries = await _db.DailyPackingSummaries
            .AsNoTracking()
            .Include(dp => dp.MenuItem)
                .ThenInclude(mi => mi!.Category)
            .Where(dp =>
                dp.TenantId == _tenant.TenantId &&
                dp.SummaryDate == targetDate)
            .OrderBy(dp => dp.MenuItem!.Category!.DisplayOrder)
            .ThenBy(dp => dp.MenuItem!.Name)
            .ToListAsync();

        if (!summaries.Any())
            return Ok(new
            {
                date = targetDate,
                message = "No packing summary for this date. " +
                           "Run the packing job first.",
                items = Array.Empty<object>(),
                totalBoxes = 0
            });

        var items = summaries.Select(s => new
        {
            menuItemId = s.MenuItemId,
            itemName = s.MenuItem?.Name,
            category = s.MenuItem?.Category?.Name ?? "Uncategorised",
            portionSize = s.PortionSize,
            totalQuantity = s.TotalQuantity,
            unit = s.MenuItem?.Unit
        }).ToList();

        return Ok(new
        {
            date = targetDate,
            totalBoxes = summaries.First().TotalBoxes,
            generatedAt = summaries.First().GeneratedAt,
            items
        });
    }

    // ── GET TODAY'S DISPATCH LIST ─────────────────────────────
    // What admin sees — all deliveries grouped by zone
    [HttpGet("dispatch-list")]
    [RequirePermission("tiffin:deliveries:read")]
    public async Task<IActionResult> GetDispatchList(
        [FromQuery] DateOnly? date = null)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var schedules = await _db.DeliverySchedules
            .AsNoTracking()
            .Include(ds => ds.Subscription)
                .ThenInclude(s => s!.Zone)
            .Include(ds => ds.Subscription)
                .ThenInclude(s => s!.Plan)
            .Where(ds =>
                ds.TenantId == _tenant.TenantId &&
                ds.ScheduledDate == targetDate)
            .ToListAsync();

        if (!schedules.Any())
            return Ok(new
            {
                date = targetDate,
                message = "No deliveries scheduled for this date.",
                zones = Array.Empty<object>(),
                total = 0
            });

        // Load customer names
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

        // Group by zone
        var grouped = schedules
            .GroupBy(ds => ds.Subscription!.Zone)
            .Select(g => new
            {
                zoneId = g.Key!.Id,
                zoneName = g.Key.Name,
                zoneCode = g.Key.ZoneCode,
                totalDeliveries = g.Count(),
                deliveries = g.Select(ds =>
                {
                    var customer = customers.FirstOrDefault(
                        c => c.Id == ds.Subscription!.CustomerId);
                    return new
                    {
                        scheduleId = ds.Id,
                        subscriptionId = ds.SubscriptionId,
                        customerName = customer?.FullName ?? "Unknown",
                        customerPhone = customer?.Phone,
                        deliveryAddress = ds.Subscription!.DeliveryAddress,
                        floorOrUnit = ds.Subscription.FloorOrUnit,
                        deliveryInstructions = ds.Subscription.DeliveryInstructions,
                        spicePreference = ds.Subscription.SpicePreference,
                        planName = ds.Subscription.Plan!.Name,
                        lat = ds.Subscription.DeliveryLat,
                        lng = ds.Subscription.DeliveryLng,
                        status = ds.Status,
                        driverId = ds.DriverId,
                        sequenceNumber = ds.SequenceNumber
                    };
                }).ToList()
            }).ToList();

        return Ok(new
        {
            date = targetDate,
            total = schedules.Count,
            zones = grouped
        });
    }

    // ── MANUAL TRIGGER — PACKING SUMMARY ─────────────────────
    // Admin button: "Generate tomorrow's packing list"
    // Useful if the cron failed or for testing
    [HttpPost("generate-packing-summary")]
    [RequirePermission("tiffin:reports:delivery")]
    public async Task<IActionResult> TriggerPackingSummary()
    {
        BackgroundJob.Enqueue<IDeliveryEngineService>(
            service => service.GeneratePackingSummaryAsync(_tenant.TenantId));

        return Ok(new
        {
            message = "Packing summary job queued. " +
                      "Check /api/delivery-engine/packing-summary " +
                      "in a few seconds."
        });
    }

    // ── MANUAL TRIGGER — DISPATCH LIST ───────────────────────
    // Admin button: "Generate today's delivery list"
    [HttpPost("generate-dispatch-list")]
    [RequirePermission("tiffin:reports:delivery")]
    public async Task<IActionResult> TriggerDispatchList()
    {
        BackgroundJob.Enqueue<IDeliveryEngineService>(
            service => service.GenerateDispatchListAsync(_tenant.TenantId));

        return Ok(new
        {
            message = "Dispatch list job queued. " +
                      "Check /api/delivery-engine/dispatch-list " +
                      "in a few seconds."
        });
    }

    // ── MANUAL REGENERATE FOR SPECIFIC DATE ──────────────────
    // Admin uses this when the cron failed for a past date
    [HttpPost("regenerate")]
    [RequirePermission("tiffin:reports:delivery")]
    public async Task<IActionResult> Regenerate(
        [FromBody] RegenerateRequest request)
    {
        var count = await _engine.RegenerateForDateAsync(
            _tenant.TenantId,
            request.Date);

        return Ok(new
        {
            message = $"Regenerated {count} delivery schedule rows " +
                      $"for {request.Date}.",
            count
        });
    }

    // ── GET SCHEDULE RUN LOGS ─────────────────────────────────
    // Admin can see if cron jobs ran successfully or failed
    [HttpGet("run-logs")]
    [RequirePermission("tiffin:reports:delivery")]
    public async Task<IActionResult> GetRunLogs(
        [FromQuery] int days = 7)
    {
        var from = DateTime.UtcNow.AddDays(-days);

        var logs = await _db.ScheduleRunLogs
            .AsNoTracking()
            .Where(l =>
                l.TenantId == _tenant.TenantId &&
                l.CreatedAt >= from)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new
            {
                l.Id,
                l.RunType,
                l.TargetDate,
                l.Status,
                l.RecordsGenerated,
                l.ErrorMessage,
                l.ScheduledFor,
                l.StartedAt,
                l.CompletedAt,
                l.RetryCount
            })
            .ToListAsync();

        return Ok(logs);
    }
}

public record RegenerateRequest(DateOnly Date);