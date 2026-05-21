namespace TiffinOS.API.DTOs.Tiffin;

public record AssignDriverRequest(
    Guid DriverId,
    Guid ZoneId,
    DateOnly AssignmentDate
);

public record UpdateDeliveryStatusRequest(
    string Status,      // 'delivered', 'attempted'
    string? FailureReason
);

public record RouteAssignmentResponse(
    Guid Id,
    Guid DriverId,
    string DriverName,
    Guid ZoneId,
    string ZoneName,
    DateOnly AssignmentDate,
    int TotalStops,
    int CompletedStops,
    int AttemptedStops,
    string Status,
    decimal? ActualDistanceKm,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    List<RouteStopResponse> Stops
);

public record RouteStopResponse(
    Guid ScheduleId,
    int SequenceNumber,
    string CustomerName,
    string? CustomerPhone,
    string DeliveryAddress,
    string? FloorOrUnit,
    string? DeliveryInstructions,
    string? SpicePreference,
    string PlanName,
    decimal Lat,
    decimal Lng,
    string Status,
    DateTime? DeliveredAt,
    DateTime? AttemptedAt,
    string? FailureReason,
    bool HasPod
);