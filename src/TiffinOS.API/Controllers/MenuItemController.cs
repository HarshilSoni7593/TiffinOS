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
[Route("api/menu-items")]
[Authorize]
[RequireModule("tiffin")]
public class MenuItemsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;
    private readonly CurrentUserContext _user;

    public MenuItemsController(
        AppDbContext db,
        TenantContext tenant,
        CurrentUserContext user)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
    }

    // ── CATEGORIES ────────────────────────────────────────────

    [HttpGet("categories")]
    [RequirePermission("tiffin:plans:read")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _db.MenuItemCategories
        .AsNoTracking()
        .Where(c => c.TenantId == _tenant.TenantId && c.IsActive)
        .OrderBy(c => c.DisplayOrder)
        .Select(c => new
        {
            c.Id,
            c.Name,
            c.DisplayOrder,
            c.IsActive,
            ItemCount = _db.MenuItems
                .Count(mi => mi.CategoryId == c.Id && mi.IsActive)
        })
        .ToListAsync();

        return Ok(categories);
    }

    [HttpPost("categories")]
    [RequirePermission("tiffin:plans:write")]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryRequest request)
    {
        // Check duplicate name within tenant
        var exists = await _db.MenuItemCategories
            .AnyAsync(c => c.TenantId == _tenant.TenantId &&
                           c.Name.ToLower() == request.Name.ToLower().Trim() &&
                           c.IsActive);

        if (exists)
            return BadRequest(new
            {
                error = "A category with this name already exists.",
                code = "CATEGORY_EXISTS"
            });

        var category = new MenuItemCategory
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            Name = request.Name.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsActive = true
        };

        _db.MenuItemCategories.Add(category);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Category created successfully.",
            categoryId = category.Id,
            name = category.Name
        });
    }

    [HttpPut("categories/{id:guid}")]
    [RequirePermission("tiffin:plans:write")]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryRequest request)
    {
        var category = await _db.MenuItemCategories
            .FirstOrDefaultAsync(c => c.TenantId == _tenant.TenantId &&
                                      c.Id == id);

        if (category == null)
            return NotFound(new
            {
                error = "Category not found.",
                code = "CATEGORY_NOT_FOUND"
            });

        category.Name = request.Name.Trim();
        category.DisplayOrder = request.DisplayOrder;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Category updated successfully." });
    }

    [HttpDelete("categories/{id:guid}")]
    [RequirePermission("tiffin:plans:write")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var category = await _db.MenuItemCategories
            .FirstOrDefaultAsync(c => c.TenantId == _tenant.TenantId &&
                                      c.Id == id);

        if (category == null)
            return NotFound(new
            {
                error = "Category not found.",
                code = "CATEGORY_NOT_FOUND"
            });

        // Check if any active items use this category
        var hasItems = await _db.MenuItems
            .AnyAsync(mi => mi.CategoryId == id && mi.IsActive);

        if (hasItems)
            return BadRequest(new
            {
                error = "Cannot delete a category that has active items. " +
                        "Reassign or deactivate items first.",
                code = "CATEGORY_HAS_ITEMS"
            });

        category.IsActive = false;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Category deleted successfully." });
    }

    // ── MENU ITEMS ────────────────────────────────────────────

    [HttpGet]
    [RequirePermission("tiffin:plans:read")]
    public async Task<IActionResult> GetMenuItems(
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool includeInactive = false)
    {
        var query = _db.MenuItems
            .AsNoTracking()
            .Include(mi => mi.Category)
            .Where(mi => mi.TenantId == _tenant.TenantId);

        if (!includeInactive)
            query = query.Where(mi => mi.IsActive);

        if (categoryId.HasValue)
            query = query.Where(mi => mi.CategoryId == categoryId);

        var items = await query
            .OrderBy(mi => mi.Category!.DisplayOrder)
            .ThenBy(mi => mi.Name)
            .Select(mi => new MenuItemResponse(
                mi.Id,
                mi.CategoryId,
                mi.Category != null ? mi.Category.Name : null,
                mi.Name,
                mi.Unit,
                mi.MeasurementType,
                mi.AvailablePortions,
                mi.Description,
                mi.IsActive,
                mi.CreatedAt
            ))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("tiffin:plans:read")]
    public async Task<IActionResult> GetMenuItem(Guid id)
    {
        var item = await _db.MenuItems
            .AsNoTracking()
            .Include(mi => mi.Category)
            .Where(mi => mi.TenantId == _tenant.TenantId && mi.Id == id)
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound(new
            {
                error = "Menu item not found.",
                code = "MENU_ITEM_NOT_FOUND"
            });

        return Ok(new MenuItemResponse(
            item.Id,
            item.CategoryId,
            item.Category?.Name,
            item.Name,
            item.Unit,
            item.MeasurementType,
            item.AvailablePortions,
            item.Description,
            item.IsActive,
            item.CreatedAt
        ));
    }

    [HttpPost]
    [RequirePermission("tiffin:plans:write")]
    public async Task<IActionResult> CreateMenuItem(
        [FromBody] CreateMenuItemRequest request)
    {
        // Validate available portions
        if (request.AvailablePortions == null || !request.AvailablePortions.Any())
            return BadRequest(new
            {
                error = "At least one portion size is required.",
                code = "PORTIONS_REQUIRED"
            });

        // Validate category belongs to tenant if provided
        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _db.MenuItemCategories
                .AnyAsync(c => c.Id == request.CategoryId &&
                               c.TenantId == _tenant.TenantId &&
                               c.IsActive);

            if (!categoryExists)
                return NotFound(new
                {
                    error = "Category not found.",
                    code = "CATEGORY_NOT_FOUND"
                });
        }

        // Check duplicate name within tenant
        var exists = await _db.MenuItems
            .AnyAsync(mi => mi.TenantId == _tenant.TenantId &&
                            mi.Name.ToLower() == request.Name.ToLower().Trim() &&
                            mi.IsActive);

        if (exists)
            return BadRequest(new
            {
                error = "A menu item with this name already exists.",
                code = "MENU_ITEM_EXISTS"
            });

        var menuItem = new MenuItem
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Unit = request.Unit,
            MeasurementType = request.MeasurementType,
            AvailablePortions = request.AvailablePortions,
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.MenuItems.Add(menuItem);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Menu item created successfully.",
            menuItemId = menuItem.Id,
            name = menuItem.Name
        });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("tiffin:plans:write")]
    public async Task<IActionResult> UpdateMenuItem(
        Guid id,
        [FromBody] UpdateMenuItemRequest request)
    {
        var item = await _db.MenuItems
            .FirstOrDefaultAsync(mi => mi.TenantId == _tenant.TenantId &&
                                       mi.Id == id);

        if (item == null)
            return NotFound(new
            {
                error = "Menu item not found.",
                code = "MENU_ITEM_NOT_FOUND"
            });

        // Validate category if changed
        if (request.CategoryId.HasValue &&
            request.CategoryId != item.CategoryId)
        {
            var categoryExists = await _db.MenuItemCategories
                .AnyAsync(c => c.Id == request.CategoryId &&
                               c.TenantId == _tenant.TenantId &&
                               c.IsActive);

            if (!categoryExists)
                return NotFound(new
                {
                    error = "Category not found.",
                    code = "CATEGORY_NOT_FOUND"
                });
        }

        item.CategoryId = request.CategoryId;
        item.Name = request.Name.Trim();
        item.Unit = request.Unit;
        item.MeasurementType = request.MeasurementType;
        item.AvailablePortions = request.AvailablePortions;
        item.Description = request.Description?.Trim();

        await _db.SaveChangesAsync();

        return Ok(new { message = "Menu item updated successfully." });
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("tiffin:plans:write")]
    public async Task<IActionResult> DeactivateMenuItem(Guid id)
    {
        var item = await _db.MenuItems
            .FirstOrDefaultAsync(mi => mi.TenantId == _tenant.TenantId &&
                                       mi.Id == id);

        if (item == null)
            return NotFound(new
            {
                error = "Menu item not found.",
                code = "MENU_ITEM_NOT_FOUND"
            });

        // Check if item is used in any active plan
        var usedInPlan = await _db.TiffinPlanItems
            .AnyAsync(pi => pi.MenuItemId == id &&
                            pi.Plan.IsActive);

        if (usedInPlan)
            return BadRequest(new
            {
                error = "Cannot deactivate an item that is used in an active plan. " +
                        "Remove it from all active plans first.",
                code = "ITEM_IN_ACTIVE_PLAN"
            });

        item.IsActive = false;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Menu item deactivated successfully." });
    }
}