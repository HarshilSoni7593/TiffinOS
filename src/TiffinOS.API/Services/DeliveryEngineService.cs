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

    // ── DRIVER PAYOUT GENERATION (end of day) ────────────────────
    // Why: Runs automatically after all routes are completed.
    // Calculates each driver's earnings for the day based on their
    // payout policy. Admin just reviews and approves — no manual math.
    public async Task GenerateDriverPayoutsAsync(Guid tenantId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var runLog = await StartRunLog(tenantId, "driver_payouts", today);

        try
        {
            // Get all completed route assignments for today
            var completedRoutes = await _db.RouteAssignments
                .AsNoTracking()
                .Where(ra =>
                    ra.TenantId == tenantId &&
                    ra.AssignmentDate == today &&
                    ra.Status == "completed")
                .ToListAsync();

            if (!completedRoutes.Any())
            {
                await CompleteRunLog(runLog, "success", 0,
                    "No completed routes today");
                return;
            }

            var driverIds = completedRoutes
                .Select(ra => ra.DriverId)
                .Distinct()
                .ToList();

            var drivers = await _db.DriverProfiles
                .AsNoTracking()
                .Include(d => d.PayoutPolicy)
                .Where(d => driverIds.Contains(d.Id))
                .ToListAsync();

            int recordsCreated = 0;

            foreach (var driver in drivers)
            {
                // Skip if payout already exists for today
                var alreadyExists = await _db.DriverPayoutRecords
                    .AnyAsync(pr =>
                        pr.DriverId == driver.Id &&
                        pr.PayoutDate == today);

                if (alreadyExists) continue;

                if (driver.PayoutPolicy == null)
                {
                    _logger.LogWarning(
                        "Driver {DriverId} has no payout policy — " +
                        "skipping payout calculation", driver.Id);
                    continue;
                }

                // Get driver's deliveries for today
                var driverDeliveries = await _db.DeliverySchedules
                    .Where(ds =>
                        ds.DriverId == driver.Id &&
                        ds.ScheduledDate == today &&
                        ds.Status == "delivered")
                    .CountAsync();

                // Get driver's routes for today
                var driverRoutes = completedRoutes
                    .Where(ra => ra.DriverId == driver.Id)
                    .ToList();

                var totalZones = driverRoutes.Count;
                var totalDistance = driverRoutes
                    .Sum(ra => ra.ActualDistanceKm ?? 0);

                // Calculate payout based on policy type
                var (baseAmount, bonusAmount) = CalculatePayout(
                    driver.PayoutPolicy,
                    driverDeliveries,
                    totalDistance,
                    totalZones);

                var record = new DriverPayoutRecord
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    DriverId = driver.Id,
                    PolicyId = driver.PayoutPolicy.Id,
                    PayoutDate = today,
                    PayoutType = driver.PayoutPolicy.PayoutType,
                    TotalDeliveries = driverDeliveries,
                    TotalDistanceKm = totalDistance > 0
                        ? totalDistance : null,
                    TotalZones = totalZones,
                    BaseAmount = baseAmount,
                    BonusAmount = bonusAmount,
                    TotalAmount = baseAmount + bonusAmount,
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow
                };

                _db.DriverPayoutRecords.Add(record);
                recordsCreated++;
            }

            await _db.SaveChangesAsync();
            await CompleteRunLog(runLog, "success", recordsCreated);

            _logger.LogInformation(
                "Driver payouts generated for tenant {TenantId}: " +
                "{Count} records for {Date}",
                tenantId, recordsCreated, today);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Driver payout generation failed for " +
                "tenant {TenantId}", tenantId);
            await CompleteRunLog(runLog, "failed", 0, ex.Message);
            throw;
        }
    }

    // ── PAYOUT CALCULATION ENGINE ─────────────────────────────────
    private static (decimal baseAmount, decimal bonusAmount)
        CalculatePayout(
            DriverPayoutPolicy policy,
            int deliveries,
            decimal distanceKm,
            int zones)
    {
        decimal baseAmount = 0;
        decimal bonusAmount = 0;

        switch (policy.PayoutType)
        {
            case "per_delivery":
                baseAmount = deliveries * policy.BaseRate;
                break;

            case "per_day":
                baseAmount = policy.BaseRate;
                break;

            case "per_km":
                baseAmount = distanceKm * policy.BaseRate;
                break;

            case "per_zone":
                baseAmount = zones * policy.BaseRate;
                break;

            case "hybrid":
                // Base daily rate + bonus per delivery above threshold
                baseAmount = policy.BaseRate;
                if (policy.BonusThreshold.HasValue &&
                    policy.BonusPerDelivery.HasValue &&
                    deliveries > policy.BonusThreshold.Value)
                {
                    var bonusDeliveries =
                        deliveries - policy.BonusThreshold.Value;
                    bonusAmount =
                        bonusDeliveries * policy.BonusPerDelivery.Value;
                }
                break;
        }

        // Apply minimum guaranteed floor
        var total = baseAmount + bonusAmount;
        if (policy.MinGuaranteed.HasValue &&
            total < policy.MinGuaranteed.Value)
        {
            baseAmount = policy.MinGuaranteed.Value;
            bonusAmount = 0;
        }

        return (baseAmount, bonusAmount);
    }
}