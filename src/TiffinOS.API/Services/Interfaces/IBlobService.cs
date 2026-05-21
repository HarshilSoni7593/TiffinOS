namespace TiffinOS.API.Services.Interfaces;

public interface IBlobService
{
    // Generates a pre-signed URL the driver app uses to upload
    // directly to Azure Blob without going through the API
    Task<BlobUploadResult> GenerateUploadUrlAsync(
        string tenantSlug,
        string fileName);

    // Deletes a blob if POD is disputed and needs resubmission
    Task DeleteAsync(string blobUrl);
}

public record BlobUploadResult(
    string UploadUrl,       // pre-signed URL — expires in 15 minutes
    string BlobUrl,         // permanent URL to access the file after upload
    string BlobName         // internal reference stored in pod_records
);