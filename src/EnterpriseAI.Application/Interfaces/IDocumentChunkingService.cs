using EnterpriseAI.Application.Models;

namespace EnterpriseAI.Application.Interfaces;

public interface IDocumentChunkingService
{
    List<DocumentChunk> CreateChunks(
        Guid documentId,
        string text);
}