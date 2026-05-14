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
[Route("api/plans")]
[Authorize]
[RequireModule("tiffin")]
public class TiffinPlansController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;
    private readonly CurrentUserContext _user;

    public TiffinPlansController(
        AppDbContext db,
        TenantContext tenant,
        CurrentUserContext user)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
    }

    // ── GET ALL PLANS ─────────────────────────────────────────
    [HttpGet]
    [RequirePermission("tiffin:plans:read")]
    public async Task<IActionResult> GetPlans(
        [FromQuery] bool includeInactive = false)
    {
        var query = _db.TiffinPlans
            .AsNoTracking()
            .Where(p => p.TenantId == _tenant.TenantId);

        if (!includeInactive)
            query = query.Where(p => p.IsActive);

        var plans = await query
            .Select(p => new PlanListResponse(
                p.Id,
                p.Name,
                p.DietaryType,
                p.ImageUrl,
                p.IsActive,
                p.PricingTiers.Count(pt => pt.IsActive),
                p.TiffinPlanItems.Count,
                p.PricingTiers
                    .Where(pt => pt.IsActive)
                    .Select(pt => pt.PricePerDay)
                    .OrderBy(price => price)
                    .FirstOrDefault(),
                p.CreatedAt
            ))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Ok(plans);
    }

    // ── GET SINGLE PLAN ───────────────────────────────────────
    [HttpGet("{id:guid}")]
    [RequirePermission("tiffin:plans:read")]
    public async Task<IActionResult> GetPlan(Guid id)
    {
        var plan = await _db.TiffinPlans
            .AsNoTracking()
            .Include(p => p.PricingTiers)
            .Include(p => p.TiffinPlanItems)
                .ThenInclude(pi => pi.MenuItem)
                    .ThenInclude(mi => mi!.Category)
            .Where(p => p.TenantId == _tenant.TenantId && p.Id == id)
            .FirstOrDefaultAsync();

        if (plan == null)
            return NotFound(new { error = "Plan not found.", code = "PLAN_NOT_FOUND" });

        return Ok(MapToResponse(plan));
    }

    // ── CREATE PLAN ───────────────────────────────────────────
    [HttpPost]
    [RequirePermission("tiffin:plans:write")]
    public async Task<IActionResult> CreatePlan(
        [FromBody] CreatePlanRequest request)
    {
        // Validate pricing tiers
        if (request.PricingTiers == null || !request.PricingTiers.Any())
            return BadRequest(new
            {
                error = "At least one pricing tier is required.",
                code = "PRICING_TIER_REQUIRED"
            });

        // Validate menu items exist and belong to this tenant
        var menuItemIds = request.Items.Select(i => i.MenuItemId).ToList();
        var validItems = await _db.MenuItems
            .Where(mi => mi.TenantId == _tenant.TenantId &&
                         menuItemIds.Contains(mi.Id) &&
                         mi.IsActive)
            .Select(mi => mi.Id)
            .ToListAsync();

        var invalidIds = menuItemIds.Except(validItems).ToList();
        if (invalidIds.Any())
            return BadRequest(new
            {
                error = "One or more menu items not found or inactive.",
                code = "INVALID_MENU_ITEMS",
                ids = invalidIds
            });

        // Validate duplicate duration types in pricing tiers
        var duplicateTiers = request.PricingTiers
            .GroupBy(pt => pt.DurationType)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateTiers.Any())
            return BadRequest(new
            {
                error = $"Duplicate duration types: {string.Join(", ", duplicateTiers)}",
                code = "DUPLICATE_TIER"
            });

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // Create plan
            var plan = new TiffinPlan
            {
                Id = Guid.NewGuid(),
                TenantId = _tenant.TenantId,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                DietaryType = request.DietaryType,
                BoxType = request.BoxType,
                ImageUrl = request.ImageUrl,
                AllowSkip = request.AllowSkip,
                MinSkipNoticeHours = request.MinSkipNoticeHours,
                MaxSkipsPerCycle = request.MaxSkipsPerCycle,
                SkipCreditPolicy = request.SkipCreditPolicy,
                ExpiryReminderDays = request.ExpiryReminderDays,
                IsActive = true,
                CreatedBy = _user.UserId,
                UpdatedBy = _user.UserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.TiffinPlans.Add(plan);
            await _db.SaveChangesAsync();

            // Create pricing tiers
            var pricingTiers = request.PricingTiers.Select(pt => new PlanPricingTier
            {
                Id = Guid.NewGuid(),
                PlanId = plan.Id,
                DurationType = pt.DurationType,
                PricePerDay = pt.PricePerDay,
                BillingCycleDays = pt.BillingCycleDays,
                TotalAmount = pt.TotalAmount,
                IsActive = true,
                CreatedBy = _user.UserId,
                UpdatedBy = _user.UserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            _db.PlanPricingTiers.AddRange(pricingTiers);

            // Create plan items
            var planItems = request.Items.Select(item => new TiffinPlanItem
            {
                Id = Guid.NewGuid(),
                PlanId = plan.Id,
                MenuItemId = item.MenuItemId,
                PortionSize = item.PortionSize,
                Quantity = item.Quantity,
                DisplayOrder = item.DisplayOrder
            }).ToList();

            _db.TiffinPlanItems.AddRange(planItems);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

            // Return created plan with all details
            var created = await _db.TiffinPlans
                .AsNoTracking()
                .Include(p => p.PricingTiers)
                .Include(p => p.TiffinPlanItems)
                    .ThenInclude(pi => pi.MenuItem)
                        .ThenInclude(mi => mi!.Category)
                .FirstAsync(p => p.Id == plan.Id);

            return CreatedAtAction(
                nameof(GetPlan),
                new { id = plan.Id },
                MapToResponse(created));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ── UPDATE PLAN ───────────────────────────────────────────
    [HttpPut("{id:guid}")]
    [RequirePermission("tiffin:plans:write")]
    public async Task<IActionResult> UpdatePlan(
        Guid id,
        [FromBody] UpdatePlanRequest request)
    {
        var plan = await _db.TiffinPlans
            .Where(p => p.TenantId == _tenant.TenantId && p.Id == id)
            .FirstOrDefaultAsync();

        if (plan == null)
            return NotFound(new { error = "Plan not found.", code = "PLAN_NOT_FOUND" });

        plan.Name = request.Name.Trim();
        plan.Description = request.Description?.Trim();
        plan.DietaryType = request.DietaryType;
        plan.BoxType = request.BoxType;
        plan.ImageUrl = request.ImageUrl;
        plan.AllowSkip = request.AllowSkip;
        plan.MinSkipNoticeHours = request.MinSkipNoticeHours;
        plan.MaxSkipsPerCycle = request.MaxSkipsPerCycle;
        plan.SkipCreditPolicy = request.SkipCreditPolicy;
        plan.ExpiryReminderDays = request.ExpiryReminderDays;
        plan.UpdatedBy = _user.UserId;
        plan.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Plan updated successfully." });
    }

    // ── ARCHIVE PLAN ──────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    [RequirePermission("tiffin:plans:archive")]
    public async Task<IActionResult> ArchivePlan(Guid id)
    {
        var plan = await _db.TiffinPlans
            .Where(p => p.TenantId == _tenant.TenantId && p.Id == id)
            .FirstOrDefaultAsync();

        if (plan == null)
            return NotFound(new { error = "Plan not found.", code = "PLAN_NOT_FOUND" });

        // Check no active subscriptions exist on this plan
        var activeSubscriptions = await _db.Subscriptions
            .AnyAsync(s => s.PlanId == plan.Id &&
                           (s.Status == "active" || s.Status == "paused"));

        if (activeSubscriptions)
            return BadRequest(new
            {
                error = "Cannot archive a plan with active subscriptions.",
                code = "PLAN_HAS_ACTIVE_SUBSCRIPTIONS"
            });

        plan.IsActive = false;
        plan.UpdatedBy = _user.UserId;
        plan.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Plan archived successfully." });
    }

    // ── ADD PRICING TIER ──────────────────────────────────────
    [HttpPost("{id:guid}/tiers")]
    [RequirePermission("tiffin:plans:write")]
    public async Task<IActionResult> AddPricingTier(
        Guid id,
        [FromBody] CreatePricingTierRequest request)
    {
        var plan = await _db.TiffinPlans
            .Where(p => p.TenantId == _tenant.TenantId && p.Id == id)
            .FirstOrDefaultAsync();

        if (plan == null)
            return NotFound(new { error = "Plan not found.", code = "PLAN_NOT_FOUND" });

        // Check duplicate duration type
        var exists = await _db.PlanPricingTiers
            .AnyAsync(pt => pt.PlanId == id &&
                            pt.DurationType == request.DurationType &&
                            pt.IsActive);

        if (exists)
            return BadRequest(new
            {
                error = $"A {request.DurationType} tier already exists for this plan.",
                code = "DUPLICATE_TIER"
            });

        var tier = new PlanPricingTier
        {
            Id = Guid.NewGuid(),
            PlanId = id,
            DurationType = request.DurationType,
            PricePerDay = request.PricePerDay,
            BillingCycleDays = request.BillingCycleDays,
            TotalAmount = request.TotalAmount,
            IsActive = true,
            CreatedBy = _user.UserId,
            UpdatedBy = _user.UserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.PlanPricingTiers.Add(tier);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Pricing tier added successfully.",
            tierId = tier.Id
        });
    }

    // ── ADD PLAN ITEM ─────────────────────────────────────────
    [HttpPost("{id:guid}/items")]
    [RequirePermission("tiffin:plans:write")]
    public async Task<IActionResult> AddPlanItem(
        Guid id,
        [FromBody] CreatePlanItemRequest request)
    {
        var plan = await _db.TiffinPlans
            .Where(p => p.TenantId == _tenant.TenantId && p.Id == id)
            .FirstOrDefaultAsync();

        if (plan == null)
            return NotFound(new { error = "Plan not found.", code = "PLAN_NOT_FOUND" });

        // Validate menu item belongs to this tenant
        var menuItem = await _db.MenuItems
            .FirstOrDefaultAsync(mi => mi.Id == request.MenuItemId &&
                                       mi.TenantId == _tenant.TenantId &&
                                       mi.IsActive);

        if (menuItem == null)
            return NotFound(new
            {
                error = "Menu item not found.",
                code = "MENU_ITEM_NOT_FOUND"
            });

        // Check if item already in plan
        var alreadyExists = await _db.TiffinPlanItems
            .AnyAsync(pi => pi.PlanId == id &&
                            pi.MenuItemId == request.MenuItemId);

        if (alreadyExists)
            return BadRequest(new
            {
                error = "This item is already in the plan.",
                code = "ITEM_ALREADY_IN_PLAN"
            });

        // Validate portion size exists in menu item
        if (!menuItem.AvailablePortions.Contains(request.PortionSize))
            return BadRequest(new
            {
                error = $"Invalid portion size. Available: {string.Join(", ", menuItem.AvailablePortions)}",
                code = "INVALID_PORTION_SIZE"
            });

        var planItem = new TiffinPlanItem
        {
            Id = Guid.NewGuid(),
            PlanId = id,
            MenuItemId = request.MenuItemId,
            PortionSize = request.PortionSize,
            Quantity = request.Quantity,
            DisplayOrder = request.DisplayOrder
        };

        _db.TiffinPlanItems.Add(planItem);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Item added to plan successfully.",
            planItemId = planItem.Id
        });
    }

    // ── REMOVE PLAN ITEM ──────────────────────────────────────
    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [RequirePermission("tiffin:plans:write")]
    public async Task<IActionResult> RemovePlanItem(Guid id, Guid itemId)
    {
        var planItem = await _db.TiffinPlanItems
            .Where(pi => pi.PlanId == id && pi.Id == itemId)
            .FirstOrDefaultAsync();

        if (planItem == null)
            return NotFound(new
            {
                error = "Plan item not found.",
                code = "PLAN_ITEM_NOT_FOUND"
            });

        _db.TiffinPlanItems.Remove(planItem);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Item removed from plan." });
    }

    // ── PRIVATE MAPPER ────────────────────────────────────────
    private static PlanResponse MapToResponse(TiffinPlan plan) => new(
        plan.Id,
        plan.Name,
        plan.Description,
        plan.DietaryType,
        plan.BoxType,
        plan.ImageUrl,
        plan.AllowSkip,
        plan.MinSkipNoticeHours,
        plan.MaxSkipsPerCycle,
        plan.SkipCreditPolicy,
        plan.ExpiryReminderDays,
        plan.IsActive,
        plan.PricingTiers.Select(pt => new PricingTierResponse(
            pt.Id,
            pt.DurationType,
            pt.PricePerDay,
            pt.BillingCycleDays,
            pt.TotalAmount,
            pt.IsActive
        )).ToList(),
        plan.TiffinPlanItems.Select(pi => new PlanItemResponse(
            pi.Id,
            pi.MenuItemId,
            pi.MenuItem?.Name ?? string.Empty,
            pi.MenuItem?.Category?.Name ?? "Uncategorised",
            pi.PortionSize,
            pi.Quantity,
            pi.DisplayOrder
        )).ToList(),
        plan.CreatedAt
    );
}