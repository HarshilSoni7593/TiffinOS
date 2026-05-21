using Microsoft.EntityFrameworkCore;
using TiffinOS.API.Data;
using TiffinOS.API.Models.Common;
using TiffinOS.API.Models.Tiffin;
using TiffinOS.API.Services.Interfaces;

namespace TiffinOS.API.Services;

public class DeliveryEngineService : IDeliveryEngineService
{
    private readonly AppDbContext _db;
    private readonly ILogger<DeliveryEngineService> _logger;

    public DeliveryEngineService(
        AppDbContext db,
        ILogger<DeliveryEngineService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── PACKING SUMMARY (night before) ────────────────────────
    // Why: Cook needs to know TONIGHT what to prepare tomorrow.
    // Groups all tomorrow's deliveries by menu item + portion size
    // so the kitchen knows exactly what quantities to prepare.
    public async Task GeneratePackingSummaryAsync(Guid tenantId)
    {
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var runLog = await StartRunLog(
            tenantId, "packing_summary", targetDate);

        try
        {
            // Check if today is a holiday
            var isHoliday = await _db.TenantHolidays
                .AnyAsync(h =>
                    h.TenantId == tenantId &&
                    h.HolidayDate == targetDate &&
                    h.IsDeliveryOff);

            if (isHoliday)
            {
                _logger.LogInformation(
                    "Skipping packing summary for tenant {TenantId} " +
                    "on {Date} — holiday", tenantId, targetDate);
                await CompleteRunLog(runLog, "success", 0,
                    "Skipped — holiday");
                return;
            }

            // Delete existing summary for this date if regenerating
            var existing = await _db.DailyPackingSummaries
                .Where(dp => dp.TenantId == tenantId &&
                             dp.SummaryDate == targetDate)
                .ToListAsync();

            if (existing.Any())
                _db.DailyPackingSummaries.RemoveRange(existing);

            // Get all active subscriptions delivering tomorrow
            var activeSubscriptions = await _db.Subscriptions
                .AsNoTracking()
                .Where(s =>
                    s.TenantId == tenantId &&
                    s.Status == "active" &&
                    s.StartDate <= targetDate &&
                    (s.EndDate == null || s.EndDate >= targetDate))
                .ToListAsync();

            if (!activeSubscriptions.Any())
            {
                await CompleteRunLog(runLog, "success", 0,
                    "No active subscriptions");
                return;
            }

            // Group by plan to load items efficiently
            var planIds = activeSubscriptions
                .Select(s => s.PlanId)
                .Distinct()
                .ToList();

            var planItems = await _db.TiffinPlanItems
                .AsNoTracking()
                .Include(pi => pi.MenuItem)
                .Where(pi => planIds.Contains(pi.PlanId))
                .ToListAsync();

            // Calculate totals per menu item + portion size
            // Dictionary key: (MenuItemId, PortionSize)
            var totals = new Dictionary<(Guid, string), decimal>();

            foreach (var sub in activeSubscriptions)
            {
                var items = planItems
                    .Where(pi => pi.PlanId == sub.PlanId)
                    .ToList();

                foreach (var item in items)
                {
                    var key = (item.MenuItemId, item.PortionSize);
                    if (!totals.ContainsKey(key))
                        totals[key] = 0;
                    totals[key] += item.Quantity;
                }
            }

            // Create summary rows
            var summaries = totals.Select(kvp => new DailyPackingSummary
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SummaryDate = targetDate,
                MenuItemId = kvp.Key.Item1,
                PortionSize = kvp.Key.Item2,
                TotalQuantity = kvp.Value,
                TotalBoxes = activeSubscriptions.Count,
                GeneratedAt = DateTime.UtcNow
            }).ToList();

            _db.DailyPackingSummaries.AddRange(summaries);
            await _db.SaveChangesAsync();

            await CompleteRunLog(runLog, "success", summaries.Count);

            _logger.LogInformation(
                "Packing summary generated for tenant {TenantId}: " +
                "{Count} item rows for {Date}",
                tenantId, summaries.Count, targetDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Packing summary failed for tenant {TenantId}", tenantId);
            await CompleteRunLog(runLog, "failed", 0, ex.Message);

            // Re-throw so Hangfire marks the job as failed
            // and retries automatically
            throw;
        }
    }

    // ── DISPATCH LIST (delivery morning) ─────────────────────
    // Why: Generates individual delivery_schedule rows for today.
    // Each row represents one tiffin box that needs to be delivered.
    // Admin sees these grouped by zone and assigns drivers.
    public async Task GenerateDispatchListAsync(Guid tenantId)
    {
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var runLog = await StartRunLog(
            tenantId, "dispatch_list", targetDate);

        try
        {
            // Check holiday
            var isHoliday = await _db.TenantHolidays
                .AnyAsync(h =>
                    h.TenantId == tenantId &&
                    h.HolidayDate == targetDate &&
                    h.IsDeliveryOff);

            if (isHoliday)
            {
                await CompleteRunLog(runLog, "success", 0,
                    "Skipped — holiday");
                return;
            }

            // Get active subscriptions for today
            var activeSubscriptions = await _db.Subscriptions
                .AsNoTracking()
                .Where(s =>
                    s.TenantId == tenantId &&
                    s.Status == "active" &&
                    s.StartDate <= targetDate &&
                    (s.EndDate == null || s.EndDate >= targetDate))
                .ToListAsync();

            if (!activeSubscriptions.Any())
            {
                await CompleteRunLog(runLog, "success", 0,
                    "No active subscriptions");
                return;
            }

            // Skip subscriptions already scheduled today
            // (prevents duplicate rows if job runs twice)
            var existingScheduleIds = await _db.DeliverySchedules
                .Where(ds =>
                    ds.TenantId == tenantId &&
                    ds.ScheduledDate == targetDate)
                .Select(ds => ds.SubscriptionId)
                .ToListAsync();

            var toSchedule = activeSubscriptions
                .Where(s => !existingScheduleIds.Contains(s.Id))
                .ToList();

            if (!toSchedule.Any())
            {
                await CompleteRunLog(runLog, "success", 0,
                    "All subscriptions already scheduled");
                return;
            }

            // Create delivery schedule rows
            var schedules = toSchedule.Select(sub =>
                new DeliverySchedule
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SubscriptionId = sub.Id,
                    ScheduledDate = targetDate,
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }).ToList();

            _db.DeliverySchedules.AddRange(schedules);
            await _db.SaveChangesAsync();

            await CompleteRunLog(runLog, "success", schedules.Count);

            _logger.LogInformation(
                "Dispatch list generated for tenant {TenantId}: " +
                "{Count} deliveries for {Date}",
                tenantId, schedules.Count, targetDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Dispatch list failed for tenant {TenantId}", tenantId);
            await CompleteRunLog(runLog, "failed", 0, ex.Message);
            throw;
        }
    }

    // ── MANUAL REGENERATE ─────────────────────────────────────
    // Why: If the cron fails or admin needs to regenerate for a
    // specific date, this endpoint handles it. The admin dashboard
    // has a "Regenerate" button that calls this.
    public async Task<int> RegenerateForDateAsync(
        Guid tenantId,
        DateOnly date)
    {
        // Remove existing schedules for this date
        var existing = await _db.DeliverySchedules
            .Where(ds =>
                ds.TenantId == tenantId &&
                ds.ScheduledDate == date &&
                ds.Status == "pending")
            .ToListAsync();

        _db.DeliverySchedules.RemoveRange(existing);

        var activeSubscriptions = await _db.Subscriptions
            .AsNoTracking()
            .Where(s =>
                s.TenantId == tenantId &&
                s.Status == "active" &&
                s.StartDate <= date &&
                (s.EndDate == null || s.EndDate >= date))
            .ToListAsync();

        var schedules = activeSubscriptions.Select(sub =>
            new DeliverySchedule
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SubscriptionId = sub.Id,
                ScheduledDate = date,
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

        _db.DeliverySchedules.AddRange(schedules);
        await _db.SaveChangesAsync();

        return schedules.Count;
    }

    // ── PRIVATE HELPERS ───────────────────────────────────────

    private async Task<ScheduleRunLog> StartRunLog(
        Guid tenantId,
        string runType,
        DateOnly targetDate)
    {
        var log = new ScheduleRunLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RunType = runType,
            ScheduledFor = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow,
            Status = "running",
            TargetDate = targetDate,
            CreatedAt = DateTime.UtcNow
        };

        _db.ScheduleRunLogs.Add(log);
        await _db.SaveChangesAsync();
        return log;
    }

    private async Task CompleteRunLog(
        ScheduleRunLog log,
        string status,
        int recordsGenerated,
        string? errorMessage = null)
    {
        log.Status = status;
        log.CompletedAt = DateTime.UtcNow;
        log.RecordsGenerated = recordsGenerated;
        log.ErrorMessage = errorMessage;
        await _db.SaveChangesAsync();
    }
}