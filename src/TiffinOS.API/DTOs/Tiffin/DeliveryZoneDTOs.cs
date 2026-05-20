namespace TiffinOS.API.DTOs.Tiffin;

public record CreateZoneRequest(
    string Name,
    string ZoneCode,
    string? ColorHex,
    List<PolygonCoordinate> PolygonCoords
);

public record UpdateZoneRequest(
    string Name,
    string? ColorHex,
    List<PolygonCoordinate> PolygonCoords
);

public record PolygonCoordinate(
    decimal Lat,
    decimal Lng
);

public record ZoneResponse(
    Guid Id,
    string Name,
    string ZoneCode,
    string? ColorHex,
    List<PolygonCoordinate> PolygonCoords,
    bool IsActive,
    int ActiveSubscriptionCount,
    DateTime CreatedAt
);

public record ZoneListResponse(
    Guid Id,
    string Name,
    string ZoneCode,
    string? ColorHex,
    bool IsActive,
    int ActiveSubscriptionCount
);

public record ResolveZoneRequest(decimal Lat, decimal Lng);