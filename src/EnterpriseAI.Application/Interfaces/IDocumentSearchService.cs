
using EnterpriseAI.Application.Models;

namespace EnterpriseAI.Application.Interfaces;

public interface IDocumentSearchService
{
    Task<IReadOnlyList<DocumentChunk>> SearchAsync(
        float[] queryEmbedding,
        int topK = 5,
        CancellationToken cancellationToken = default);
}
