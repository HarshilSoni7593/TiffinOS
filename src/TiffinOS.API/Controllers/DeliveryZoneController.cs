using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TiffinOS.API.Data;
using TiffinOS.API.DTOs.Tiffin;
using TiffinOS.API.Middleware;
using TiffinOS.API.Models.Tiffin;
using TiffinOS.API.Services;

namespace TiffinOS.API.Controllers;

[ApiController]
[Route("api/zones")]
[Authorize]
[RequireModule("tiffin")]
public class DeliveryZonesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;
    private readonly CurrentUserContext _user;

    public DeliveryZonesController(
        AppDbContext db,
        TenantContext tenant,
        CurrentUserContext user)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
    }

    // ── GET ALL ZONES ─────────────────────────────────────────
    // Why: Admin needs to see all zones to manage driver assignments.
    // Customer checkout calls this to validate their address zone.
    [HttpGet]
    [RequirePermission("tiffin:plans:read")]
    public async Task<IActionResult> GetZones(
        [FromQuery] bool includeInactive = false)
    {
        var query = _db.DeliveryZones
            .AsNoTracking()
            .Where(z => z.TenantId == _tenant.TenantId);

        if (!includeInactive)
            query = query.Where(z => z.IsActive);

        var zones = await query
            .OrderBy(z => z.Name)
            .ToListAsync();

        var response = zones.Select(z => new ZoneListResponse(
            z.Id,
            z.Name,
            z.ZoneCode,
            z.ColorHex,
            z.IsActive,
            _db.Subscriptions.Count(s =>
                s.ZoneId == z.Id &&
                (s.Status == "active" || s.Status == "paused"))
        )).ToList();

        return Ok(response);
    }

    // ── GET SINGLE ZONE ───────────────────────────────────────
    // Why: Returns full polygon coordinates needed to render the
    // zone boundary on a map in the admin dashboard.
    [HttpGet("{id:guid}")]
    [RequirePermission("tiffin:plans:read")]
    public async Task<IActionResult> GetZone(Guid id)
    {
        var zone = await _db.DeliveryZones
            .AsNoTracking()
            .Where(z => z.TenantId == _tenant.TenantId && z.Id == id)
            .FirstOrDefaultAsync();

        if (zone == null)
            return NotFound(new
            {
                error = "Zone not found.",
                code = "ZONE_NOT_FOUND"
            });

        var activeSubscriptions = await _db.Subscriptions
            .CountAsync(s => s.ZoneId == id &&
                             (s.Status == "active" || s.Status == "paused"));

        var coords = JsonSerializer
            .Deserialize<List<PolygonCoordinate>>(zone.PolygonCoords)
            ?? new List<PolygonCoordinate>();

        return Ok(new ZoneResponse(
            zone.Id,
            zone.Name,
            zone.ZoneCode,
            zone.ColorHex,
            coords,
            zone.IsActive,
            activeSubscriptions,
            zone.CreatedAt
        ));
    }

    // ── CREATE ZONE ───────────────────────────────────────────
    // Why: Admin defines delivery boundaries. Each zone gets a
    // zone_code used to batch deliveries for drivers.
    // The polygon is an array of lat/lng points defining the boundary.
    [HttpPost]
    [RequirePermission("tiffin:plans:write")]
    public async Task<IActionResult> CreateZone(
        [FromBody] CreateZoneRequest request)
    {
        // Zone code must be unique per tenant
        var codeExists = await _db.DeliveryZones
            .AnyAsync(z => z.TenantId == _tenant.TenantId &&
                           z.ZoneCode.ToLower() ==
                           request.ZoneCode.ToLower().Trim());

        if (codeExists)
            return BadRequest(new
            {
                error = "A zone with this code already exists.",
                code = "ZONE_CODE_EXISTS"
            });

        // Need at least 3 points to form a valid polygon
        if (request.PolygonCoords == null ||
            request.PolygonCoords.Count < 3)
            return BadRequest(new
            {
                error = "A zone requires at least 3 coordinates " +
                        "to form a valid boundary.",
                code = "INVALID_POLYGON"
            });

        var zone = new DeliveryZone
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            Name = request.Name.Trim(),
            ZoneCode = request.ZoneCode.ToUpper().Trim(),
            ColorHex = request.ColorHex,
            PolygonCoords = JsonSerializer.Serialize(request.PolygonCoords),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.DeliveryZones.Add(zone);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Delivery zone created successfully.",
            zoneId = zone.Id,
            name = zone.Name,
            code = zone.ZoneCode
        });
    }

    // ── UPDATE ZONE ───────────────────────────────────────────
    // Why: Admin may need to adjust zone boundaries as the
    // delivery area expands. Existing subscriptions keep their
    // zone assignment — only future ones use the new boundary.
    [HttpPut("{id:guid}")]
    [RequirePermission("tiffin:plans:write")]
    public async Task<IActionResult> UpdateZone(
        Guid id,
        [FromBody] UpdateZoneRequest request)
    {
        var zone = await _db.DeliveryZones
            .Where(z => z.TenantId == _tenant.TenantId && z.Id == id)
            .FirstOrDefaultAsync();

        if (zone == null)
            return NotFound(new
            {
                error = "Zone not found.",
                code = "ZONE_NOT_FOUND"
            });

        if (request.PolygonCoords == null ||
            request.PolygonCoords.Count < 3)
            return BadRequest(new
            {
                error = "A zone requires at least 3 coordinates.",
                code = "INVALID_POLYGON"
            });

        zone.Name = request.Name.Trim();
        zone.ColorHex = request.ColorHex;
        zone.PolygonCoords = JsonSerializer.Serialize(request.PolygonCoords);

        await _db.SaveChangesAsync();

        return Ok(new { message = "Zone updated successfully." });
    }

    // ── DEACTIVATE ZONE ───────────────────────────────────────
    // Why: You cannot delete a zone that has existing subscriptions.
    // Deactivating stops new subscriptions from using it while
    // keeping historical data intact.
    [HttpDelete("{id:guid}")]
    [RequirePermission("tiffin:plans:write")]
    public async Task<IActionResult> DeactivateZone(Guid id)
    {
        var zone = await _db.DeliveryZones
            .Where(z => z.TenantId == _tenant.TenantId && z.Id == id)
            .FirstOrDefaultAsync();

        if (zone == null)
            return NotFound(new
            {
                error = "Zone not found.",
                code = "ZONE_NOT_FOUND"
            });

        // Block deactivation if active subscriptions exist
        var activeCount = await _db.Subscriptions
            .CountAsync(s => s.ZoneId == id &&
                             (s.Status == "active" || s.Status == "paused"));

        if (activeCount > 0)
            return BadRequest(new
            {
                error = $"Cannot deactivate a zone with " +
                        $"{activeCount} active subscription(s). " +
                        $"Reassign those subscriptions first.",
                code = "ZONE_HAS_ACTIVE_SUBSCRIPTIONS"
            });

        zone.IsActive = false;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Zone deactivated successfully." });
    }

    // ── RESOLVE ZONE FROM COORDINATES ─────────────────────────
    // Why: When a customer enters their delivery address at checkout,
    // the frontend geocodes it to lat/lng and calls this endpoint.
    // The system checks which zone polygon contains those coordinates
    // and returns the zone. If no zone matches, delivery is unavailable
    // at that address.
    [HttpPost("resolve")]
    [AllowAnonymous]
    public async Task<IActionResult> ResolveZone(
        [FromBody] ResolveZoneRequest request)
    {
        var zones = await _db.DeliveryZones
            .AsNoTracking()
            .Where(z => z.TenantId == _tenant.TenantId && z.IsActive)
            .ToListAsync();

        foreach (var zone in zones)
        {
            var coords = JsonSerializer
                .Deserialize<List<PolygonCoordinate>>(zone.PolygonCoords)
                ?? new List<PolygonCoordinate>();

            if (IsPointInPolygon(
                    request.Lat, request.Lng, coords))
            {
                return Ok(new
                {
                    zoneId = zone.Id,
                    zoneName = zone.Name,
                    zoneCode = zone.ZoneCode
                });
            }
        }

        return Ok(new
        {
            zoneId = (Guid?)null,
            zoneName = (string?)null,
            zoneCode = (string?)null,
            message = "No delivery zone covers this address."
        });
    }

    // ── POINT IN POLYGON ALGORITHM ────────────────────────────
    // Why: This is the Ray Casting algorithm. It fires an imaginary
    // horizontal ray from the customer's location and counts how many
    // times it crosses the zone boundary. Odd crossings = inside the
    // zone. Even crossings = outside. This is the standard algorithm
    // used for geographic point-in-polygon checks.
    private static bool IsPointInPolygon(
        decimal lat,
        decimal lng,
        List<PolygonCoordinate> polygon)
    {
        int n = polygon.Count;
        bool inside = false;

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            decimal xi = polygon[i].Lat, yi = polygon[i].Lng;
            decimal xj = polygon[j].Lat, yj = polygon[j].Lng;

            bool intersect =
                ((yi > lng) != (yj > lng)) &&
                (lat < (xj - xi) * (lng - yi) / (yj - yi) + xi);

            if (intersect) inside = !inside;
        }

        return inside;
    }
}