namespace TiffinOS.API.DTOs.Tiffin;

// ── Payout Policy Requests ────────────────────────────────────

public record CreatePayoutPolicyRequest(
    string Name,
    string PayoutType,          // 'per_delivery', 'per_day', 'per_km', 'per_zone', 'hybrid'
    decimal BaseRate,
    decimal? BonusPerDelivery,  // hybrid only
    int? BonusThreshold,        // hybrid only — deliveries above this get bonus
    decimal? MinGuaranteed,
    string Currency
);

public record UpdatePayoutPolicyRequest(
    string Name,
    decimal BaseRate,
    decimal? BonusPerDelivery,
    int? BonusThreshold,
    decimal? MinGuaranteed,
    bool IsActive
);

// ── Driver Requests ───────────────────────────────────────────

public record CreateDriverRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Phone,
    string VehicleType,         // 'bike', 'scooter', 'car', 'cycle'
    string? LicenceNumber,
    int MaxDeliveriesPerDay,
    Guid? PayoutPolicyId
);

public record UpdateDriverRequest(
    string FirstName,
    string LastName,
    string Phone,
    string VehicleType,
    string? LicenceNumber,
    int MaxDeliveriesPerDay,
    Guid? PayoutPolicyId,
    bool IsAvailable
);

// ── Responses ─────────────────────────────────────────────────

public record PayoutPolicyResponse(
    Guid Id,
    string Name,
    string PayoutType,
    decimal BaseRate,
    decimal? BonusPerDelivery,
    int? BonusThreshold,
    decimal? MinGuaranteed,
    string Currency,
    bool IsActive,
    int DriverCount
);

public record DriverResponse(
    Guid Id,
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string VehicleType,
    string? LicenceNumber,
    int MaxDeliveriesPerDay,
    bool IsAvailable,
    bool IsActive,
    PayoutPolicySummary? PayoutPolicy,
    DateTime CreatedAt
);

public record DriverListResponse(
    Guid Id,
    Guid UserId,
    string FullName,
    string VehicleType,
    bool IsAvailable,
    bool IsActive,
    string? PayoutPolicyName,
    int TodayDeliveries
);

public record PayoutPolicySummary(
    Guid Id,
    string Name,
    string PayoutType,
    decimal BaseRate
);