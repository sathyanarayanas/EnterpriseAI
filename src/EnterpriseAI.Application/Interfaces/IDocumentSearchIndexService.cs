
using EnterpriseAI.Application.Models;

namespace EnterpriseAI.Application.Interfaces;

public interface IDocumentSearchIndexService
{
    Task IndexChunksAsync(
        IReadOnlyList<DocumentChunk> chunks,
        string fileName,
        CancellationToken cancellationToken = default);
}
