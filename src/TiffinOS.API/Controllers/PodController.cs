using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiffinOS.API.Data;
using TiffinOS.API.DTOs.Tiffin;
using TiffinOS.API.Middleware;
using TiffinOS.API.Models.Tiffin;
using TiffinOS.API.Services;
using TiffinOS.API.Services.Interfaces;

namespace TiffinOS.API.Controllers;

[ApiController]
[Route("api/pod")]
[Authorize]
[RequireModule("tiffin")]
public class PodController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;
    private readonly CurrentUserContext _user;
    private readonly IBlobService _blob;

    public PodController(
        AppDbContext db,
        TenantContext tenant,
        CurrentUserContext user,
        IBlobService blob)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
        _blob = blob;
    }

    // ── REQUEST UPLOAD URL ────────────────────────────────────
    // Step 1 of POD flow.
    // Driver calls this BEFORE taking the photo.
    // Gets back a pre-signed URL valid for 15 minutes.
    // Driver app uploads photo directly to Azure Blob using this URL.
    // Your API server never handles the raw photo bytes.
    [HttpPost("upload-url")]
    [RequirePermission("tiffin:deliveries:pod:upload")]
    public async Task<IActionResult> RequestUploadUrl(
        [FromBody] RequestUploadUrlRequest request)
    {
        var result = await _blob.GenerateUploadUrlAsync(
            _tenant.TenantSlug,
            request.FileName);

        return Ok(new
        {
            uploadUrl = result.UploadUrl,
            blobUrl = result.BlobUrl,
            blobName = result.BlobName,
            expiresIn = "15 minutes",
            instructions = "PUT the photo binary directly to " +
                          "uploadUrl. Then call /api/pod/confirm " +
                          "with the blobName, photoUrl, and GPS " +
                          "coordinates captured at shutter fire."
        });
    }

    // ── CONFIRM POD UPLOAD ────────────────────────────────────
    // Step 2 of POD flow.
    // Called AFTER the photo has been uploaded to Azure.
    // Driver provides GPS coordinates and timestamp from the
    // moment the photo was taken — not from upload time.
    // This is the critical integrity check:
    // capture_at must match shutter time, not upload time.
    [HttpPost("{deliveryId:guid}/confirm")]
    [RequirePermission("tiffin:deliveries:pod:upload")]
    public async Task<IActionResult> ConfirmPodUpload(
        Guid deliveryId,
        [FromBody] ConfirmPodUploadRequest request)
    {
        // Validate delivery exists and belongs to this tenant
        var delivery = await _db.DeliverySchedules
            .FirstOrDefaultAsync(ds =>
                ds.TenantId == _tenant.TenantId &&
                ds.Id == deliveryId);

        if (delivery == null)
            return NotFound(new
            {
                error = "Delivery not found.",
                code = "DELIVERY_NOT_FOUND"
            });

        if (delivery.Status != "delivered" &&
            delivery.Status != "in_progress")
            return BadRequest(new
            {
                error = "POD can only be uploaded for deliveries " +
                        "in progress or marked delivered.",
                code = "INVALID_DELIVERY_STATUS"
            });

        // Check POD doesn't already exist
        var existingPod = await _db.PODRecords
            .AnyAsync(p => p.DeliveryId == deliveryId);

        if (existingPod)
            return BadRequest(new
            {
                error = "A POD already exists for this delivery.",
                code = "POD_ALREADY_EXISTS"
            });

        // Validate capture time is not in the future
        if (request.CaptureAt > DateTime.UtcNow.AddMinutes(2))
            return BadRequest(new
            {
                error = "Capture timestamp cannot be in the future.",
                code = "INVALID_CAPTURE_TIME"
            });

        // Validate capture time is not too old
        // (within last 2 hours — prevents backdating)
        if (request.CaptureAt < DateTime.UtcNow.AddHours(-2))
            return BadRequest(new
            {
                error = "Capture timestamp is too old. " +
                        "Photo must be taken within 2 hours.",
                code = "CAPTURE_TIME_TOO_OLD"
            });

        var pod = new PODRecord
        {
            Id = Guid.NewGuid(),
            DeliveryId = deliveryId,
            PhotoUrl = request.PhotoUrl,
            CaptureLat = request.CaptureLat,
            CaptureLng = request.CaptureLng,
            CaptureAt = request.CaptureAt,
            UploadAt = DateTime.UtcNow,
            Status = "uploaded"
        };

        _db.PODRecords.Add(pod);

        // Mark delivery as delivered if not already
        if (delivery.Status == "in_progress")
        {
            delivery.Status = "delivered";
            delivery.DeliveredAt = DateTime.UtcNow;
            delivery.UpdatedBy = _user.UserId;
            delivery.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "POD uploaded and confirmed successfully.",
            podId = pod.Id,
            deliveryId,
            photoUrl = pod.PhotoUrl,
            capturedAt = pod.CaptureAt,
            uploadedAt = pod.UploadAt
        });
    }

    // ── GET POD FOR DELIVERY ──────────────────────────────────
    // Admin or customer can view POD for a specific delivery.
    // Customer uses this to see proof their tiffin was delivered.
    [HttpGet("{deliveryId:guid}")]
    [RequirePermission("tiffin:deliveries:read")]
    public async Task<IActionResult> GetPod(Guid deliveryId)
    {
        var pod = await _db.PODRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.DeliveryId == deliveryId);

        if (pod == null)
            return NotFound(new
            {
                error = "No POD found for this delivery.",
                code = "POD_NOT_FOUND"
            });

        // Verify delivery belongs to this tenant
        var delivery = await _db.DeliverySchedules
            .AnyAsync(ds =>
                ds.TenantId == _tenant.TenantId &&
                ds.Id == deliveryId);

        if (!delivery)
            return NotFound(new
            {
                error = "Delivery not found.",
                code = "DELIVERY_NOT_FOUND"
            });

        return Ok(new PodResponse(
            pod.Id,
            pod.DeliveryId,
            pod.PhotoUrl,
            pod.CaptureLat,
            pod.CaptureLng,
            pod.CaptureAt,
            pod.UploadAt,
            pod.Status
        ));
    }

    // ── GET ALL PODS FOR DATE ─────────────────────────────────
    // Admin dashboard — see all PODs for today at a glance.
    // Shows delivered with POD vs delivered without POD.
    [HttpGet("summary")]
    [RequirePermission("tiffin:reports:delivery")]
    public async Task<IActionResult> GetPodSummary(
        [FromQuery] DateOnly? date = null)
    {
        var targetDate = date ??
            DateOnly.FromDateTime(DateTime.UtcNow);

        var deliveries = await _db.DeliverySchedules
            .AsNoTracking()
            .Where(ds =>
                ds.TenantId == _tenant.TenantId &&
                ds.ScheduledDate == targetDate &&
                ds.Status == "delivered")
            .ToListAsync();

        var deliveryIds = deliveries.Select(d => d.Id).ToList();

        var pods = await _db.PODRecords
            .AsNoTracking()
            .Where(p => deliveryIds.Contains(p.DeliveryId))
            .ToListAsync();

        return Ok(new
        {
            date = targetDate,
            totalDelivered = deliveries.Count,
            withPod = pods.Count,
            withoutPod = deliveries.Count - pods.Count,
            podCompletionRate = deliveries.Count == 0
                ? 0
                : Math.Round(
                    (double)pods.Count / deliveries.Count * 100, 1),
            pods = pods.Select(p => new
            {
                p.Id,
                p.DeliveryId,
                p.PhotoUrl,
                p.CaptureAt,
                p.Status
            })
        });
    }
}