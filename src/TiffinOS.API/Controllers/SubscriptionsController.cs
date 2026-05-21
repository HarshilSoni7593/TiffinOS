using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiffinOS.API.Controllers;
using TiffinOS.API.Data;
using TiffinOS.API.DTOs.Tiffin;
using TiffinOS.API.Middleware;
using TiffinOS.API.Models.Tiffin;
using TiffinOS.API.Models.Payments;
using TiffinOS.API.Services;

namespace TiffinOS.API.Controllers;

[ApiController]
[Route("api/subscriptions")]
[Authorize]
[RequireModule("tiffin")]
public class SubscriptionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;
    private readonly CurrentUserContext _user;
    private readonly DeliveryChargeRulesController _chargeRules;

    public SubscriptionsController(
        AppDbContext db,
        TenantContext tenant,
        CurrentUserContext user,
        DeliveryChargeRulesController chargeRules)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
        _chargeRules = chargeRules;
    }

    // ── GET ALL SUBSCRIPTIONS (Admin) ─────────────────────────
    // Why: Admin sees all subscriptions across all customers.
    // Filterable by status so they can quickly find active,
    // paused, or cancelled subscriptions.
    [HttpGet]
    [RequirePermission("tiffin:subscriptions:read:all")]
    public async Task<IActionResult> GetSubscriptions(
        [FromQuery] string? status = null,
        [FromQuery] Guid? zoneId = null,
        [FromQuery] Guid? planId = null)
    {
        var query = _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.TenantId == _tenant.TenantId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(s => s.Status == status);

        if (zoneId.HasValue)
            query = query.Where(s => s.ZoneId == zoneId);

        if (planId.HasValue)
            query = query.Where(s => s.PlanId == planId);

        var subscriptions = await query
            .Include(s => s.Plan)
            .Include(s => s.PricingTier)
            .Include(s => s.Zone)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        // Load customer names separately
        var customerIds = subscriptions
            .Select(s => s.CustomerId)
            .Distinct()
            .ToList();

        var customers = await _db.Users
            .Where(u => customerIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                FullName = u.FirstName + " " + u.LastName
            })
            .ToListAsync();

        var response = subscriptions.Select(s => new SubscriptionListResponse(
            s.Id,
            customers.FirstOrDefault(c => c.Id == s.CustomerId)?.FullName
                ?? "Unknown",
            s.Plan.Name,
            s.PricingTier.DurationType,
            s.Zone.Name,
            s.Status,
            s.StartDate,
            s.EndDate,
            s.LockedTotalAmount,
            s.LockedDeliveryCharge,
            s.CreatedAt
        )).ToList();

        return Ok(response);
    }

    // ── GET OWN SUBSCRIPTIONS (Customer) ─────────────────────
    // Why: Customer can only see their own subscriptions —
    // never other customers' data. Enforced by filtering on
    // CustomerId = current user's ID.
    [HttpGet("my")]
    [RequirePermission("tiffin:subscriptions:read:own")]
    public async Task<IActionResult> GetMySubscriptions()
    {
        var subscriptions = await _db.Subscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .Include(s => s.PricingTier)
            .Include(s => s.Zone)
            .Where(s => s.TenantId == _tenant.TenantId &&
                        s.CustomerId == _user.UserId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        var response = subscriptions.Select(s =>
            MapToResponse(s, _user.Email,
                $"{s.Plan?.Name ?? ""} subscriber")).ToList();

        return Ok(response);
    }

    // ── GET SINGLE SUBSCRIPTION ───────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSubscription(Guid id)
    {
        var subscription = await _db.Subscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .Include(s => s.PricingTier)
            .Include(s => s.Zone)
            .Where(s => s.TenantId == _tenant.TenantId &&
                        s.Id == id)
            .FirstOrDefaultAsync();

        if (subscription == null)
            return NotFound(new
            {
                error = "Subscription not found.",
                code = "SUBSCRIPTION_NOT_FOUND"
            });

        // Customer can only see their own subscription
        if (_user.HasRole("customer") &&
            subscription.CustomerId != _user.UserId)
            return Forbid();

        var customer = await _db.Users
            .Where(u => u.Id == subscription.CustomerId)
            .Select(u => new
            {
                FullName = u.FirstName + " " + u.LastName,
                u.Email
            })
            .FirstOrDefaultAsync();

        return Ok(MapToResponse(
            subscription,
            customer?.Email ?? "",
            customer?.FullName ?? ""));
    }

    // ── CREATE SUBSCRIPTION ───────────────────────────────────
    // Why this is the most critical endpoint:
    // 1. Validates all referenced entities exist and are active
    // 2. Runs the charge rule engine to get locked delivery price
    // 3. Snapshots all prices at the moment of subscription —
    //    future price changes never affect existing subscribers
    // 4. Creates a pending payment transaction
    // All of this happens in a single database transaction —
    // either everything succeeds or nothing is saved.
    [HttpPost]
    [RequirePermission("tiffin:subscriptions:write")]
    public async Task<IActionResult> CreateSubscription(
        [FromBody] CreateSubscriptionRequest request)
    {
        // 1. Validate plan
        var plan = await _db.TiffinPlans
            .FirstOrDefaultAsync(p =>
                p.TenantId == _tenant.TenantId &&
                p.Id == request.PlanId &&
                p.IsActive);

        if (plan == null)
            return NotFound(new
            {
                error = "Plan not found or inactive.",
                code = "PLAN_NOT_FOUND"
            });

        // 2. Validate pricing tier belongs to this plan
        var tier = await _db.PlanPricingTiers
            .FirstOrDefaultAsync(pt =>
                pt.Id == request.PricingTierId &&
                pt.PlanId == plan.Id &&
                pt.IsActive);

        if (tier == null)
            return NotFound(new
            {
                error = "Pricing tier not found or does not " +
                        "belong to this plan.",
                code = "TIER_NOT_FOUND"
            });

        // 3. Validate zone
        var zone = await _db.DeliveryZones
            .FirstOrDefaultAsync(z =>
                z.TenantId == _tenant.TenantId &&
                z.Id == request.ZoneId &&
                z.IsActive);

        if (zone == null)
            return NotFound(new
            {
                error = "Delivery zone not found or inactive.",
                code = "ZONE_NOT_FOUND"
            });

        // 4. Validate start date is not in the past
        if (request.StartDate < DateOnly.FromDateTime(DateTime.UtcNow))
            return BadRequest(new
            {
                error = "Start date cannot be in the past.",
                code = "INVALID_START_DATE"
            });

        // 5. Check customer doesn't already have an active
        //    subscription on this plan
        var alreadySubscribed = await _db.Subscriptions
            .AnyAsync(s =>
                s.TenantId == _tenant.TenantId &&
                s.CustomerId == _user.UserId &&
                s.PlanId == request.PlanId &&
                (s.Status == "active" || s.Status == "paused"));

        if (alreadySubscribed)
            return BadRequest(new
            {
                error = "You already have an active subscription " +
                        "to this plan.",
                code = "ALREADY_SUBSCRIBED"
            });

        // 6. Run delivery charge rule engine
        var resolvedCharge = await _chargeRules.ResolveChargeAsync(
            _tenant.TenantId,
            request.ZoneId,
            request.PricingTierId,
            tier.PricePerDay);

        // 7. Calculate end date based on duration type
        var endDate = tier.DurationType switch
        {
            "daily" => (DateOnly?)null,           // open-ended
            "weekly" => request.StartDate.AddDays(7),
            "monthly" => request.StartDate.AddDays(30),
            _ => (DateOnly?)null
        };

        // 8. Total due now = plan cost + delivery charge
        var totalDue = tier.TotalAmount + resolvedCharge.Amount;

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                TenantId = _tenant.TenantId,
                CustomerId = _user.UserId,
                PlanId = request.PlanId,
                PricingTierId = request.PricingTierId,
                ZoneId = request.ZoneId,
                SpicePreference = request.SpicePreference,
                DeliveryAddress = request.DeliveryAddress.Trim(),
                DeliveryLat = request.DeliveryLat,
                DeliveryLng = request.DeliveryLng,
                FloorOrUnit = request.FloorOrUnit?.Trim(),
                DeliveryInstructions = request.DeliveryInstructions?.Trim(),
                StartDate = request.StartDate,
                EndDate = endDate,
                Status = "pending",   // moves to 'active' after payment
                LockedPricePerDay = tier.PricePerDay,
                LockedTotalAmount = tier.TotalAmount,
                LockedDeliveryCharge = resolvedCharge.Amount,
                DeliveryChargeRuleId = resolvedCharge.RuleId,
                CreatedBy = _user.UserId,
                UpdatedBy = _user.UserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Subscriptions.Add(subscription);
            await _db.SaveChangesAsync();

            // 9. Create pending payment transaction
            var payment = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                TenantId = _tenant.TenantId,
                SubscriptionId = subscription.Id,
                Amount = totalDue,
                Currency = "CAD",
                Status = "pending",
                Gateway = "razorpay",
                IdempotencyKey = $"sub_{subscription.Id}_{DateTime.UtcNow.Ticks}",
                CreatedAt = DateTime.UtcNow
            };

            _db.PaymentTransactions.Add(payment);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            // Return full subscription details
            var created = await _db.Subscriptions
                .AsNoTracking()
                .Include(s => s.Plan)
                .Include(s => s.PricingTier)
                .Include(s => s.Zone)
                .FirstAsync(s => s.Id == subscription.Id);

            var customer = await _db.Users
                .Where(u => u.Id == _user.UserId)
                .Select(u => new
                {
                    FullName = u.FirstName + " " + u.LastName,
                    u.Email
                })
                .FirstAsync();

            return Ok(new
            {
                subscription = MapToResponse(
                    created,
                    customer.Email,
                    customer.FullName),
                payment = new
                {
                    paymentId = payment.Id,
                    amountDue = totalDue,
                    currency = "CAD",
                    breakdown = new
                    {
                        planCost = tier.TotalAmount,
                        deliveryCharge = resolvedCharge.Amount
                    }
                },
                message = "Subscription created. Complete payment to activate."
            });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ── PAUSE SUBSCRIPTION ────────────────────────────────────
    // Why: Customer may go on vacation. Pausing stops delivery
    // generation for the paused period without cancelling.
    // The cron job checks paused_until before generating schedules.
    [HttpPost("{id:guid}/pause")]
    [RequirePermission("tiffin:subscriptions:cancel")]
    public async Task<IActionResult> PauseSubscription(
        Guid id,
        [FromBody] PauseSubscriptionRequest request)
    {
        var subscription = await GetOwnedSubscription(id);
        if (subscription == null)
            return NotFound(new
            {
                error = "Subscription not found.",
                code = "SUBSCRIPTION_NOT_FOUND"
            });

        if (subscription.Status != "active")
            return BadRequest(new
            {
                error = "Only active subscriptions can be paused.",
                code = "INVALID_STATUS"
            });

        // Validate plan allows skipping
        var plan = await _db.TiffinPlans
            .FirstAsync(p => p.Id == subscription.PlanId);

        if (!plan.AllowSkip)
            return BadRequest(new
            {
                error = "This plan does not allow pausing.",
                code = "PAUSE_NOT_ALLOWED"
            });

        if (request.PauseUntil <= DateOnly.FromDateTime(DateTime.UtcNow))
            return BadRequest(new
            {
                error = "Pause until date must be in the future.",
                code = "INVALID_PAUSE_DATE"
            });

        subscription.Status = "paused";
        subscription.PausedUntil = request.PauseUntil;
        subscription.UpdatedBy = _user.UserId;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = $"Subscription paused until {request.PauseUntil}.",
            pausedUntil = request.PauseUntil
        });
    }

    // ── RESUME SUBSCRIPTION ───────────────────────────────────
    [HttpPost("{id:guid}/resume")]
    [RequirePermission("tiffin:subscriptions:cancel")]
    public async Task<IActionResult> ResumeSubscription(Guid id)
    {
        var subscription = await GetOwnedSubscription(id);
        if (subscription == null)
            return NotFound(new
            {
                error = "Subscription not found.",
                code = "SUBSCRIPTION_NOT_FOUND"
            });

        if (subscription.Status != "paused")
            return BadRequest(new
            {
                error = "Only paused subscriptions can be resumed.",
                code = "INVALID_STATUS"
            });

        subscription.Status = "active";
        subscription.PausedUntil = null;
        subscription.UpdatedBy = _user.UserId;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Subscription resumed successfully." });
    }

    // ── CANCEL SUBSCRIPTION ───────────────────────────────────
    // Why: Soft cancel — status changes to 'cancelled', existing
    // delivery schedules for future dates are marked 'skipped'.
    // Historical delivery records are preserved.
    [HttpPost("{id:guid}/cancel")]
    [RequirePermission("tiffin:subscriptions:cancel")]
    public async Task<IActionResult> CancelSubscription(
        Guid id,
        [FromBody] CancelSubscriptionRequest request)
    {
        var subscription = await GetOwnedSubscription(id);
        if (subscription == null)
            return NotFound(new
            {
                error = "Subscription not found.",
                code = "SUBSCRIPTION_NOT_FOUND"
            });

        if (subscription.Status == "cancelled")
            return BadRequest(new
            {
                error = "Subscription is already cancelled.",
                code = "ALREADY_CANCELLED"
            });

        subscription.Status = "cancelled";
        subscription.CancelledAt = DateTime.UtcNow;
        subscription.CancellationReason = request.Reason;
        subscription.UpdatedBy = _user.UserId;
        subscription.UpdatedAt = DateTime.UtcNow;

        // Cancel all future pending delivery schedules
        var futureSchedules = await _db.DeliverySchedules
            .Where(ds =>
                ds.SubscriptionId == id &&
                ds.ScheduledDate > DateOnly.FromDateTime(DateTime.UtcNow) &&
                ds.Status == "pending")
            .ToListAsync();

        foreach (var schedule in futureSchedules)
            schedule.Status = "skipped";

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Subscription cancelled successfully.",
            cancelledAt = subscription.CancelledAt,
            schedulesSkipped = futureSchedules.Count
        });
    }

    // ── ACTIVATE SUBSCRIPTION (after payment) ─────────────────
    // Why: This endpoint is called by the payment webhook handler
    // after Razorpay confirms payment. It moves the subscription
    // from 'pending' to 'active'. In a real setup this would be
    // triggered by the payment gateway webhook, not manually.
    // For now it allows manual activation for testing.
    [HttpPost("{id:guid}/activate")]
    [RequirePermission("tiffin:subscriptions:write")]
    public async Task<IActionResult> ActivateSubscription(Guid id)
    {
        var subscription = await _db.Subscriptions
            .Where(s => s.TenantId == _tenant.TenantId &&
                        s.Id == id)
            .FirstOrDefaultAsync();

        if (subscription == null)
            return NotFound(new
            {
                error = "Subscription not found.",
                code = "SUBSCRIPTION_NOT_FOUND"
            });

        if (subscription.Status != "pending")
            return BadRequest(new
            {
                error = "Only pending subscriptions can be activated.",
                code = "INVALID_STATUS"
            });

        subscription.Status = "active";
        subscription.UpdatedBy = _user.UserId;
        subscription.UpdatedAt = DateTime.UtcNow;

        // Mark payment as success
        var payment = await _db.PaymentTransactions
            .Where(pt => pt.SubscriptionId == id &&
                         pt.Status == "pending")
            .FirstOrDefaultAsync();

        if (payment != null)
            payment.Status = "success";

        await _db.SaveChangesAsync();

        return Ok(new { message = "Subscription activated successfully." });
    }

    // ── PRIVATE HELPERS ───────────────────────────────────────

    // Ensures admin can see any subscription but customer
    // can only access their own
    private async Task<Subscription?> GetOwnedSubscription(Guid id)
    {
        var query = _db.Subscriptions
            .Where(s => s.TenantId == _tenant.TenantId && s.Id == id);

        if (_user.HasRole("customer"))
            query = query.Where(s => s.CustomerId == _user.UserId);

        return await query.FirstOrDefaultAsync();
    }

    private static SubscriptionResponse MapToResponse(
        Subscription s,
        string email,
        string customerName) => new(
        s.Id,
        s.CustomerId,
        customerName,
        email,
        new PlanSummary(
            s.Plan!.Id,
            s.Plan.Name,
            s.Plan.DietaryType,
            s.Plan.BoxType),
        new PricingTierSummary(
            s.PricingTier!.Id,
            s.PricingTier.DurationType,
            s.PricingTier.PricePerDay,
            s.PricingTier.BillingCycleDays,
            s.PricingTier.TotalAmount),
        new ZoneSummary(
            s.Zone!.Id,
            s.Zone.Name,
            s.Zone.ZoneCode),
        s.SpicePreference,
        s.DeliveryAddress,
        s.FloorOrUnit,
        s.DeliveryInstructions,
        s.DeliveryLat,
        s.DeliveryLng,
        s.StartDate,
        s.EndDate,
        s.Status,
        s.PausedUntil,
        s.LockedPricePerDay,
        s.LockedTotalAmount,
        s.LockedDeliveryCharge,
        s.LockedTotalAmount + s.LockedDeliveryCharge,
        s.CreatedAt
    );
}