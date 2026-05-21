using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using TiffinOS.API.Services.Interfaces;

namespace TiffinOS.API.Services;

public class AzureBlobService : IBlobService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    public AzureBlobService(IConfiguration config)
    {
        _blobServiceClient = new BlobServiceClient(
            config["Azure:StorageConnectionString"]);
        _containerName = config["Azure:BlobContainerName"]
            ?? "tiffinos-pod";
    }

    public async Task<BlobUploadResult> GenerateUploadUrlAsync(
        string tenantSlug,
        string fileName)
    {
        // Ensure container exists
        var container = _blobServiceClient
            .GetBlobContainerClient(_containerName);

        await container.CreateIfNotExistsAsync(
            PublicAccessType.None);

        // Build blob path: tenantslug/year/month/filename
        var now = DateTime.UtcNow;
        var blobName = $"{tenantSlug}/{now:yyyy/MM}/{Guid.NewGuid()}" +
                       $"_{Path.GetFileName(fileName)}";

        var blobClient = container.GetBlobClient(blobName);

        // Generate pre-signed URL valid for 15 minutes
        // Driver must upload within this window
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(15)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Write |
                                  BlobSasPermissions.Create);

        var uploadUrl = blobClient.GenerateSasUri(sasBuilder).ToString();
        var blobUrl = blobClient.Uri.ToString();

        return new BlobUploadResult(uploadUrl, blobUrl, blobName);
    }

    public async Task DeleteAsync(string blobUrl)
    {
        var uri = new Uri(blobUrl);
        var blobName = string.Join("/",
            uri.Segments.Skip(2).Select(s => s.Trim('/')));
        var container = _blobServiceClient
            .GetBlobContainerClient(_containerName);
        var blobClient = container.GetBlobClient(blobName);

        await blobClient.DeleteIfExistsAsync();
    }
}