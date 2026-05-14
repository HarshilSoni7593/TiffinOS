namespace TiffinOS.API.DTOs.Tiffin;

// ── Requests ──────────────────────────────────────────────────

public record CreatePlanRequest(
    string Name,
    string? Description,
    string DietaryType,         // 'veg', 'non_veg', 'both'
    string? BoxType,            // '2_compartment', '3_compartment', '4_compartment'
    string? ImageUrl,
    bool AllowSkip,
    int? MinSkipNoticeHours,
    int? MaxSkipsPerCycle,
    string? SkipCreditPolicy,   // 'none', 'credit'
    int ExpiryReminderDays,
    List<CreatePricingTierRequest> PricingTiers,
    List<CreatePlanItemRequest> Items
);

public record CreatePricingTierRequest(
    string DurationType,        // 'daily', 'weekly', 'monthly'
    decimal PricePerDay,
    int BillingCycleDays,       // 1, 7, 30
    decimal TotalAmount
);

public record CreatePlanItemRequest(
    Guid MenuItemId,
    string PortionSize,
    decimal Quantity,
    int DisplayOrder
);

public record UpdatePlanRequest(
    string Name,
    string? Description,
    string DietaryType,
    string? BoxType,
    string? ImageUrl,
    bool AllowSkip,
    int? MinSkipNoticeHours,
    int? MaxSkipsPerCycle,
    string? SkipCreditPolicy,
    int ExpiryReminderDays
);

// ── Responses ─────────────────────────────────────────────────

public record PlanResponse(
    Guid Id,
    string Name,
    string? Description,
    string DietaryType,
    string? BoxType,
    string? ImageUrl,
    bool AllowSkip,
    int? MinSkipNoticeHours,
    int? MaxSkipsPerCycle,
    string? SkipCreditPolicy,
    int ExpiryReminderDays,
    bool IsActive,
    List<PricingTierResponse> PricingTiers,
    List<PlanItemResponse> Items,
    DateTime CreatedAt
);

public record PricingTierResponse(
    Guid Id,
    string DurationType,
    decimal PricePerDay,
    int BillingCycleDays,
    decimal TotalAmount,
    bool IsActive
);

public record PlanItemResponse(
    Guid Id,
    Guid MenuItemId,
    string MenuItemName,
    string Category,
    string PortionSize,
    decimal Quantity,
    int DisplayOrder
);

public record PlanListResponse(
    Guid Id,
    string Name,
    string DietaryType,
    string? ImageUrl,
    bool IsActive,
    int TierCount,
    int ItemCount,
    decimal LowestPrice,
    DateTime CreatedAt
);