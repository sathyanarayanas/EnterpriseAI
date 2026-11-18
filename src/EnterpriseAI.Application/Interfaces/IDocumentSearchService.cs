
using EnterpriseAI.Application.Models;

namespace EnterpriseAI.Application.Interfaces;

public interface IDocumentSearchService
{
    Task<IReadOnlyList<DocumentChunk>> SearchAsync(
        string searchQuery,
        float[] queryEmbedding,
        int topK = 5,
        CancellationToken cancellationToken = default);
}
