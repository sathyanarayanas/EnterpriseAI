using EnterpriseAI.Application.Models;

namespace EnterpriseAI.Application.Interfaces;

public interface IDocumentService
{
    Task<DocumentInfo> UploadAsync(
        string fileName,
        string contentType,
        Stream fileStream,
        CancellationToken cancellationToken = default);

    IReadOnlyList<DocumentInfo> GetDocuments();

    IReadOnlyList<DocumentChunk> GetChunks(Guid documentId);

    Task<IReadOnlyList<DocumentChunk>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default);
}