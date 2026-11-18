using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EnterpriseAI.Application.Interfaces;

namespace EnterpriseAI.Infrastructure.Storage;

public class AzureBlobStorageService 
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobStorageService(
        AzureBlobOptions options)
    {
        var blobServiceClient =
            new BlobServiceClient(
                options.ConnectionString);

        _containerClient =
            blobServiceClient.GetBlobContainerClient(
                options.ContainerName);
    }

    public async Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await _containerClient.CreateIfNotExistsAsync(
            cancellationToken: cancellationToken);

        var blobName =
            $"{Guid.NewGuid()}-{fileName}";

        var blobClient =
            _containerClient.GetBlobClient(blobName);

        var uploadOptions =
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            };

        await blobClient.UploadAsync(
            content,
            uploadOptions,
            cancellationToken);

        return blobName;
    }

}