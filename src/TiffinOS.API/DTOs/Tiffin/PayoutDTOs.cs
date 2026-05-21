namespace TiffinOS.API.DTOs.Tiffin;

public record ApprovePayoutRequest(
    string? Notes
);

public record SettlePayoutsRequest(
    List<Guid> PayoutRecordIds,
    string PaymentMethod,       // 'cash', 'bank_transfer', 'upi'
    string? PaymentReference    // e-transfer ref, bank ref, UPI ID
);

public record PayoutRecordResponse(
    Guid Id,
    Guid DriverId,
    string DriverName,
    string PolicyName,
    string PayoutType,
    DateOnly PayoutDate,
    int TotalDeliveries,
    decimal? TotalDistanceKm,
    int? TotalZones,
    decimal BaseAmount,
    decimal BonusAmount,
    decimal TotalAmount,
    string Status,
    string? Notes,
    DateTime CreatedAt
);

public record SettlementResponse(
    Guid Id,
    Guid DriverId,
    string DriverName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal TotalAmount,
    string? PaymentMethod,
    string? PaymentReference,
    string Status,
    DateTime? ProcessedAt
);