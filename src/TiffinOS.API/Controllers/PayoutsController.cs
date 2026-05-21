using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiffinOS.API.Data;
using TiffinOS.API.DTOs.Tiffin;
using TiffinOS.API.Middleware;
using TiffinOS.API.Models.Tiffin;
using TiffinOS.API.Services;
using TiffinOS.API.Services.Interfaces;

namespace TiffinOS.API.Controllers;

[ApiController]
[Route("api/payouts")]
[Authorize]
[RequireModule("tiffin")]
public class PayoutsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;
    private readonly CurrentUserContext _user;
    private readonly IDeliveryEngineService _engine;

    public PayoutsController(
        AppDbContext db,
        TenantContext tenant,
        CurrentUserContext user,
        IDeliveryEngineService engine)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
        _engine = engine;
    }

    // ── GET PAYOUT RECORDS ────────────────────────────────────
    // Admin sees all payout records filterable by driver,
    // date range, and status.
    [HttpGet]
    [RequirePermission("tiffin:drivers:manage")]
    public async Task<IActionResult> GetPayoutRecords(
        [FromQuery] Guid? driverId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] string? status = null)
    {
        var fromDate = from ??
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var toDate = to ??
            DateOnly.FromDateTime(DateTime.UtcNow);

        var query = _db.DriverPayoutRecords
            .AsNoTracking()
            .Where(pr =>
                pr.TenantId == _tenant.TenantId &&
                pr.PayoutDate >= fromDate &&
                pr.PayoutDate <= toDate);

        if (driverId.HasValue)
            query = query.Where(pr => pr.DriverId == driverId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(pr => pr.Status == status);

        var records = await query
            .OrderByDescending(pr => pr.PayoutDate)
            .ToListAsync();

        var driverIds = records.Select(r => r.DriverId)
            .Distinct().ToList();
        var policyIds = records.Select(r => r.PolicyId)
            .Distinct().ToList();

        var drivers = await _db.DriverProfiles
            .Include(d => d.User)
            .Where(d => driverIds.Contains(d.Id))
            .Select(d => new
            {
                d.Id,
                FullName = d.User.FirstName + " " + d.User.LastName
            })
            .ToListAsync();

        var policies = await _db.DriverPayoutPolicies
            .Where(p => policyIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToListAsync();

        var response = records.Select(pr => new PayoutRecordResponse(
            pr.Id,
            pr.DriverId,
            drivers.FirstOrDefault(d => d.Id == pr.DriverId)
                ?.FullName ?? "Unknown",
            policies.FirstOrDefault(p => p.Id == pr.PolicyId)
                ?.Name ?? "Unknown",
            pr.PayoutType,
            pr.PayoutDate,
            pr.TotalDeliveries,
            pr.TotalDistanceKm,
            pr.TotalZones,
            pr.BaseAmount,
            pr.BonusAmount,
            pr.TotalAmount,
            pr.Status,
            pr.Notes,
            pr.CreatedAt
        )).ToList();

        return Ok(new
        {
            records = response,
            summary = new
            {
                totalRecords = response.Count,
                totalAmount = response.Sum(r => r.TotalAmount),
                pendingAmount = response
                    .Where(r => r.Status == "pending")
                    .Sum(r => r.TotalAmount),
                approvedAmount = response
                    .Where(r => r.Status == "approved")
                    .Sum(r => r.TotalAmount),
                paidAmount = response
                    .Where(r => r.Status == "paid")
                    .Sum(r => r.TotalAmount)
            }
        });
    }

    // ── APPROVE PAYOUT ────────────────────────────────────────
    // Admin reviews and approves a daily payout record.
    // Approved records are ready for settlement.
    [HttpPost("{id:guid}/approve")]
    [RequirePermission("tiffin:drivers:manage")]
    public async Task<IActionResult> ApprovePayout(
        Guid id,
        [FromBody] ApprovePayoutRequest request)
    {
        var record = await _db.DriverPayoutRecords
            .FirstOrDefaultAsync(pr =>
                pr.TenantId == _tenant.TenantId &&
                pr.Id == id);

        if (record == null)
            return NotFound(new
            {
                error = "Payout record not found.",
                code = "PAYOUT_NOT_FOUND"
            });

        if (record.Status != "pending")
            return BadRequest(new
            {
                error = "Only pending payouts can be approved.",
                code = "INVALID_STATUS"
            });

        record.Status = "approved";
        record.ApprovedBy = _user.UserId;
        record.ApprovedAt = DateTime.UtcNow;
        record.Notes = request.Notes;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Payout approved successfully." });
    }

    // ── SETTLE PAYOUTS ────────────────────────────────────────
    // Admin selects multiple approved payout records and
    // settles them in one batch — e.g. weekly payment to driver.
    // Creates a settlement record grouping all selected payouts.
    [HttpPost("settle")]
    [RequirePermission("tiffin:drivers:manage")]
    public async Task<IActionResult> SettlePayouts(
        [FromBody] SettlePayoutsRequest request)
    {
        if (!request.PayoutRecordIds.Any())
            return BadRequest(new
            {
                error = "At least one payout record is required.",
                code = "NO_RECORDS"
            });

        var records = await _db.DriverPayoutRecords
            .Where(pr =>
                pr.TenantId == _tenant.TenantId &&
                request.PayoutRecordIds.Contains(pr.Id) &&
                pr.Status == "approved")
            .ToListAsync();

        if (!records.Any())
            return BadRequest(new
            {
                error = "No approved payout records found " +
                        "with the provided IDs.",
                code = "NO_APPROVED_RECORDS"
            });

        // All records must belong to the same driver
        var driverIds = records.Select(r => r.DriverId).Distinct().ToList();
        if (driverIds.Count > 1)
            return BadRequest(new
            {
                error = "All payout records in a settlement must " +
                        "belong to the same driver.",
                code = "MULTIPLE_DRIVERS"
            });

        var driverId = driverIds.First();
        var totalAmount = records.Sum(r => r.TotalAmount);
        var periodStart = records.Min(r => r.PayoutDate);
        var periodEnd = records.Max(r => r.PayoutDate);

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var settlement = new DriverPayoutSettlement
            {
                Id = Guid.NewGuid(),
                TenantId = _tenant.TenantId,
                DriverId = driverId,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                TotalAmount = totalAmount,
                PaymentMethod = request.PaymentMethod,
                PaymentReference = request.PaymentReference,
                Status = "paid",
                ProcessedBy = _user.UserId,
                ProcessedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _db.DriverPayoutSettlements.Add(settlement);

            // Mark all records as paid
            foreach (var record in records)
                record.Status = "paid";

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            var driver = await _db.DriverProfiles
                .Include(d => d.User)
                .FirstAsync(d => d.Id == driverId);

            return Ok(new
            {
                message = "Settlement created successfully.",
                settlementId = settlement.Id,
                driverName = $"{driver.User.FirstName} " +
                                 $"{driver.User.LastName}",
                recordsSettled = records.Count,
                totalAmount,
                periodStart,
                periodEnd,
                paymentMethod = request.PaymentMethod
            });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ── MANUAL TRIGGER — GENERATE TODAY'S PAYOUTS ────────────
    // Admin can trigger payout generation manually
    // without waiting for the 10 PM cron.
    [HttpPost("generate-today")]
    [RequirePermission("tiffin:drivers:manage")]
    public async Task<IActionResult> GenerateTodayPayouts()
    {
        await _engine.GenerateDriverPayoutsAsync(_tenant.TenantId);

        return Ok(new
        {
            message = "Driver payouts generated for today. " +
                      "Check GET /api/payouts for results."
        });
    }

    // ── GET SETTLEMENTS ───────────────────────────────────────
    [HttpGet("settlements")]
    [RequirePermission("tiffin:drivers:manage")]
    public async Task<IActionResult> GetSettlements(
        [FromQuery] Guid? driverId = null)
    {
        var query = _db.DriverPayoutSettlements
            .AsNoTracking()
            .Where(s => s.TenantId == _tenant.TenantId);

        if (driverId.HasValue)
            query = query.Where(s => s.DriverId == driverId);

        var settlements = await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        var driverIds = settlements
            .Select(s => s.DriverId).Distinct().ToList();

        var drivers = await _db.DriverProfiles
            .Include(d => d.User)
            .Where(d => driverIds.Contains(d.Id))
            .Select(d => new
            {
                d.Id,
                FullName = d.User.FirstName + " " + d.User.LastName
            })
            .ToListAsync();

        var response = settlements.Select(s => new SettlementResponse(
            s.Id,
            s.DriverId,
            drivers.FirstOrDefault(d => d.Id == s.DriverId)
                ?.FullName ?? "Unknown",
            s.PeriodStart,
            s.PeriodEnd,
            s.TotalAmount,
            s.PaymentMethod,
            s.PaymentReference,
            s.Status,
            s.ProcessedAt
        )).ToList();

        return Ok(response);
    }
}