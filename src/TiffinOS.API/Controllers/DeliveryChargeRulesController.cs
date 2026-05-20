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
[Route("api/delivery-charges")]
[Authorize]
[RequireModule("tiffin")]
public class DeliveryChargeRulesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;
    private readonly CurrentUserContext _user;

    public DeliveryChargeRulesController(
        AppDbContext db,
        TenantContext tenant,
        CurrentUserContext user)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
    }

    // ── GET ALL RULES ─────────────────────────────────────────
    // Why: Admin needs to see all rules to understand what
    // delivery charge a new customer will get before they subscribe.
    [HttpGet]
    [RequirePermission("tiffin:plans:read")]
    public async Task<IActionResult> GetRules()
    {
        var rules = await _db.DeliveryChargeRules
            .AsNoTracking()
            .Where(r => r.TenantId == _tenant.TenantId)
            .OrderByDescending(r => r.Priority)
            .ToListAsync();

        // Load zone and tier names separately to avoid translation issues
        var zoneIds = rules
            .Where(r => r.ZoneId.HasValue)
            .Select(r => r.ZoneId!.Value)
            .Distinct()
            .ToList();

        var tierIds = rules
            .Where(r => r.PricingTierId.HasValue)
            .Select(r => r.PricingTierId!.Value)
            .Distinct()
            .ToList();

        var zones = await _db.DeliveryZones
            .Where(z => zoneIds.Contains(z.Id))
            .Select(z => new { z.Id, z.Name })
            .ToListAsync();

        var tiers = await _db.PlanPricingTiers
            .Where(t => tierIds.Contains(t.Id))
            .Select(t => new { t.Id, t.DurationType })
            .ToListAsync();

        var response = rules.Select(r => new DeliveryChargeRuleResponse(
            r.Id,
            r.ZoneId,
            zones.FirstOrDefault(z => z.Id == r.ZoneId)?.Name,
            r.PricingTierId,
            tiers.FirstOrDefault(t => t.Id == r.PricingTierId)?.DurationType,
            r.ChargeType,
            r.Amount,
            r.MinPlanPricePerDay,
            r.Priority,
            r.IsActive,
            r.Description,
            r.CreatedAt
        )).ToList();

        return Ok(response);
    }

    // ── CREATE RULE ───────────────────────────────────────────
    // Why: Admin configures delivery pricing rules. More specific
    // rules get higher priority. The rule engine at subscription
    // time picks the first matching rule.
    [HttpPost]
    [RequirePermission("tiffin:plans:write")]
    public async Task<IActionResult> CreateRule(
        [FromBody] CreateDeliveryChargeRuleRequest request)
    {
        // Validate zone belongs to tenant if provided
        if (request.ZoneId.HasValue)
        {
            var zoneExists = await _db.DeliveryZones
                .AnyAsync(z => z.Id == request.ZoneId &&
                               z.TenantId == _tenant.TenantId &&
                               z.IsActive);

            if (!zoneExists)
                return NotFound(new
                {
                    error = "Zone not found.",
                    code = "ZONE_NOT_FOUND"
                });
        }

        // Validate pricing tier belongs to this tenant's plan if provided
        if (request.PricingTierId.HasValue)
        {
            var tierExists = await _db.PlanPricingTiers
                .AnyAsync(pt => pt.Id == request.PricingTierId &&
                                pt.Plan.TenantId == _tenant.TenantId);

            if (!tierExists)
                return NotFound(new
                {
                    error = "Pricing tier not found.",
                    code = "TIER_NOT_FOUND"
                });
        }

        // Free charge must have amount 0
        if (request.ChargeType == "free" && request.Amount != 0)
            return BadRequest(new
            {
                error = "Amount must be 0 when charge type is free.",
                code = "INVALID_FREE_AMOUNT"
            });

        var rule = new DeliveryChargeRule
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            ZoneId = request.ZoneId,
            PricingTierId = request.PricingTierId,
            ChargeType = request.ChargeType,
            Amount = request.Amount,
            MinPlanPricePerDay = request.MinPlanPricePerDay,
            Priority = request.Priority,
            IsActive = true,
            Description = request.Description?.Trim(),
            CreatedBy = _user.UserId,
            UpdatedBy = _user.UserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.DeliveryChargeRules.Add(rule);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Delivery charge rule created successfully.",
            ruleId = rule.Id
        });
    }

    // ── UPDATE RULE ───────────────────────────────────────────
    // Why: Admin may need to change prices or priorities.
    // Existing subscriptions are NOT affected — they have locked
    // charges. Only new subscriptions use updated rules.
    [HttpPut("{id:guid}")]
    [RequirePermission("tiffin:plans:write")]
    public async Task<IActionResult> UpdateRule(
        Guid id,
        [FromBody] UpdateDeliveryChargeRuleRequest request)
    {
        var rule = await _db.DeliveryChargeRules
            .Where(r => r.TenantId == _tenant.TenantId && r.Id == id)
            .FirstOrDefaultAsync();

        if (rule == null)
            return NotFound(new
            {
                error = "Rule not found.",
                code = "RULE_NOT_FOUND"
            });

        if (request.ChargeType == "free" && request.Amount != 0)
            return BadRequest(new
            {
                error = "Amount must be 0 when charge type is free.",
                code = "INVALID_FREE_AMOUNT"
            });

        rule.ChargeType = request.ChargeType;
        rule.Amount = request.Amount;
        rule.MinPlanPricePerDay = request.MinPlanPricePerDay;
        rule.Priority = request.Priority;
        rule.IsActive = request.IsActive;
        rule.Description = request.Description?.Trim();
        rule.UpdatedBy = _user.UserId;
        rule.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Rule updated successfully." });
    }

    // ── DELETE RULE ───────────────────────────────────────────
    // Why: Soft delete only. Historical subscriptions reference
    // this rule ID — hard deleting would break their records.
    [HttpDelete("{id:guid}")]
    [RequirePermission("tiffin:plans:write")]
    public async Task<IActionResult> DeleteRule(Guid id)
    {
        var rule = await _db.DeliveryChargeRules
            .Where(r => r.TenantId == _tenant.TenantId && r.Id == id)
            .FirstOrDefaultAsync();

        if (rule == null)
            return NotFound(new
            {
                error = "Rule not found.",
                code = "RULE_NOT_FOUND"
            });

        rule.IsActive = false;
        rule.UpdatedBy = _user.UserId;
        rule.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Rule deactivated successfully." });
    }

    // ── PREVIEW CHARGE ────────────────────────────────────────
    // Why: Before a customer reaches the checkout summary page,
    // the frontend calls this to show them exactly what delivery
    // charge they will pay based on their zone and chosen plan tier.
    // No data is saved — purely a calculation preview.
    [HttpPost("preview")]
    [AllowAnonymous]
    public async Task<IActionResult> PreviewCharge(
        [FromBody] PreviewChargeRequest request)
    {
        var resolved = await ResolveChargeAsync(
            _tenant.TenantId,
            request.ZoneId,
            request.PricingTierId,
            request.PricePerDay);

        return Ok(new
        {
            chargeType = resolved.RuleId.HasValue ? "rule_applied" : "default_free",
            deliveryCharge = resolved.Amount,
            ruleId = resolved.RuleId
        });
    }

    // ── INTERNAL RULE ENGINE ──────────────────────────────────
    // Why this is a static method: The subscription service also
    // calls this same logic when creating a subscription. Keeping
    // it here as an internal static method means both the preview
    // endpoint and the subscription service use identical logic —
    // the customer always sees the same charge they will actually pay.
    [NonAction]
    public async Task<ResolvedDeliveryCharge> ResolveChargeAsync(
        Guid tenantId,
        Guid zoneId,
        Guid pricingTierId,
        decimal pricePerDay)
    {
        var rules = await _db.DeliveryChargeRules
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.IsActive)
            .OrderByDescending(r => r.Priority)
            .ToListAsync();

        foreach (var rule in rules)
        {
            // Zone filter — null means rule applies to all zones
            if (rule.ZoneId.HasValue && rule.ZoneId != zoneId)
                continue;

            // Tier filter — null means rule applies to all tiers
            if (rule.PricingTierId.HasValue &&
                rule.PricingTierId != pricingTierId)
                continue;

            // Minimum plan price filter
            if (rule.MinPlanPricePerDay.HasValue &&
                pricePerDay < rule.MinPlanPricePerDay.Value)
                continue;

            // First matching rule wins
            return new ResolvedDeliveryCharge(rule.Id, rule.Amount);
        }

        // No rule matched — default is free delivery
        return new ResolvedDeliveryCharge(null, 0);
    }
}