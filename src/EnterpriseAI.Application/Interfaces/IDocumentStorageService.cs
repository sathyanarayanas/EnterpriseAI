namespace EnterpriseAI.Application.Interfaces;

public interface IDocumentStorageService
{
    Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(
        string blobName,
        CancellationToken cancellationToken = default);
}