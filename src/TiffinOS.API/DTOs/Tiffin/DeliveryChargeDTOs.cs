namespace TiffinOS.API.DTOs.Tiffin;

public record CreateDeliveryChargeRuleRequest(
    Guid? ZoneId,               // null = applies to all zones
    Guid? PricingTierId,        // null = applies to all tiers
    string ChargeType,          // 'free', 'flat', 'per_delivery'
    decimal Amount,             // 0 when free
    decimal? MinPlanPricePerDay,// null = no minimum
    int Priority,               // higher = evaluated first
    string? Description
);

public record UpdateDeliveryChargeRuleRequest(
    string ChargeType,
    decimal Amount,
    decimal? MinPlanPricePerDay,
    int Priority,
    bool IsActive,
    string? Description
);

public record DeliveryChargeRuleResponse(
    Guid Id,
    Guid? ZoneId,
    string? ZoneName,
    Guid? PricingTierId,
    string? PricingTierDuration,
    string ChargeType,
    decimal Amount,
    decimal? MinPlanPricePerDay,
    int Priority,
    bool IsActive,
    string? Description,
    DateTime CreatedAt
);

// Used internally by subscription service to evaluate rules
public record ResolvedDeliveryCharge(
    Guid? RuleId,
    decimal Amount
);

public record PreviewChargeRequest(
    Guid ZoneId,
    Guid PricingTierId,
    decimal PricePerDay
);