namespace TiffinOS.API.DTOs.Tiffin;

// ── Requests ──────────────────────────────────────────────────

public record CreateCategoryRequest(
    string Name,
    int DisplayOrder
);

public record UpdateCategoryRequest(
    string Name,
    int DisplayOrder
);

public record CreateMenuItemRequest(
    Guid? CategoryId,
    string Name,
    string Unit,                        // 'piece', 'portion', 'bowl', 'glass', 'pack'
    string MeasurementType,             // 'volume', 'weight', 'pack', 'count'
    List<string> AvailablePortions,     // ["8oz","12oz"] or ["Pack of 4","Pack of 6"]
    string? Description
);

public record UpdateMenuItemRequest(
    Guid? CategoryId,
    string Name,
    string Unit,
    string MeasurementType,
    List<string> AvailablePortions,
    string? Description
);

// ── Responses ─────────────────────────────────────────────────

public record CategoryResponse(
    Guid Id,
    string Name,
    int DisplayOrder,
    bool IsActive,
    int ItemCount
);

public record MenuItemResponse(
    Guid Id,
    Guid? CategoryId,
    string? CategoryName,
    string Name,
    string Unit,
    string MeasurementType,
    List<string> AvailablePortions,
    string? Description,
    bool IsActive,
    DateTime CreatedAt
);