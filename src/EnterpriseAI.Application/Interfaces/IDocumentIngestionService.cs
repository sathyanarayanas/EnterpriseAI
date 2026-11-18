using EnterpriseAI.Application.Models;

namespace EnterpriseAI.Application.Interfaces;

public interface IDocumentIngestionService
{
    Task<DocumentIngestionResult> IngestDocumentAsync(
    string blobName,
    string originalFileName,
    CancellationToken cancellationToken = default);
}