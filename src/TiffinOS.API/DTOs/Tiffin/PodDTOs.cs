namespace TiffinOS.API.DTOs.Tiffin;

public record RequestUploadUrlRequest(
    string FileName         // e.g. "pod_photo.jpg"
);

public record ConfirmPodUploadRequest(
    string BlobName,        // returned from upload URL request
    string PhotoUrl,        // permanent URL to access the photo
    decimal CaptureLat,     // GPS at shutter fire — NOT at upload
    decimal CaptureLng,     // GPS at shutter fire — NOT at upload
    DateTime CaptureAt      // timestamp at shutter fire — NOT at upload
);

public record PodResponse(
    Guid Id,
    Guid DeliveryId,
    string PhotoUrl,
    decimal CaptureLat,
    decimal CaptureLng,
    DateTime CaptureAt,
    DateTime UploadAt,
    string Status
);