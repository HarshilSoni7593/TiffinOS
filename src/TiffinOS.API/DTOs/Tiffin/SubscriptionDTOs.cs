namespace TiffinOS.API.DTOs.Tiffin;

// ── Requests ──────────────────────────────────────────────────

public record CreateSubscriptionRequest(
    Guid PlanId,
    Guid PricingTierId,
    Guid ZoneId,
    string DeliveryAddress,
    decimal DeliveryLat,
    decimal DeliveryLng,
    string? FloorOrUnit,
    string? DeliveryInstructions,
    string? SpicePreference,        // 'mild', 'medium', 'spicy'
    DateOnly StartDate
);

public record PauseSubscriptionRequest(
    DateOnly PauseUntil,
    string? Reason
);

public record CancelSubscriptionRequest(
    string Reason
);

// ── Responses ─────────────────────────────────────────────────

public record SubscriptionResponse(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    PlanSummary Plan,
    PricingTierSummary PricingTier,
    ZoneSummary Zone,
    string? SpicePreference,
    string DeliveryAddress,
    string? FloorOrUnit,
    string? DeliveryInstructions,
    decimal DeliveryLat,
    decimal DeliveryLng,
    DateOnly StartDate,
    DateOnly? EndDate,
    string Status,
    DateOnly? PausedUntil,
    decimal LockedPricePerDay,
    decimal LockedTotalAmount,
    decimal LockedDeliveryCharge,
    decimal TotalDueNow,
    DateTime CreatedAt
);

public record SubscriptionListResponse(
    Guid Id,
    string CustomerName,
    string PlanName,
    string DurationType,
    string ZoneName,
    string Status,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal LockedTotalAmount,
    decimal LockedDeliveryCharge,
    DateTime CreatedAt
);

// ── Nested Summaries ──────────────────────────────────────────

public record PlanSummary(
    Guid Id,
    string Name,
    string DietaryType,
    string? BoxType
);

public record PricingTierSummary(
    Guid Id,
    string DurationType,
    decimal PricePerDay,
    int BillingCycleDays,
    decimal TotalAmount
);

public record ZoneSummary(
    Guid Id,
    string Name,
    string ZoneCode
);