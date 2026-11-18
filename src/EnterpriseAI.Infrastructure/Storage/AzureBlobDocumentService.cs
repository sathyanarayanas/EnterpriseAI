using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace EnterpriseAI.Infrastructure.Storage;

public class AzureBlobDocumentService : IDocumentStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly AzureStorageOptions _options;

    public AzureBlobDocumentService(
        BlobServiceClient blobServiceClient,
        IOptions<AzureStorageOptions> options)
    {
        _blobServiceClient = blobServiceClient;
        _options = options.Value;
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient
            .GetBlobContainerClient(_options.ContainerName);

        await containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        var extension = Path.GetExtension(
            Path.GetFileName(fileName)).ToLowerInvariant();

        var blobName = $"{Guid.NewGuid()}{extension}";

        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(
            fileStream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                },
                Metadata = new Dictionary<string, string>
                {
                    ["originalfilename"] = Path.GetFileName(fileName),
                    ["uploadedatutc"] = DateTime.UtcNow.ToString("O")
                }
            },
            cancellationToken);

        return blobName;
    }

    public async Task<Stream> DownloadAsync(
    string blobName,
    CancellationToken cancellationToken = default)
    {
        var containerClient = _blobServiceClient
            .GetBlobContainerClient(_options.ContainerName);

        var blobClient = containerClient.GetBlobClient(blobName);

        var response = await blobClient.DownloadStreamingAsync(
            cancellationToken: cancellationToken);

        return response.Value.Content;
    }
}