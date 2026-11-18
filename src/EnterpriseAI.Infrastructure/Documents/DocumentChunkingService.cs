using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Application.Models;

namespace EnterpriseAI.Infrastructure;

public class DocumentChunkingService : IDocumentChunkingService
{
    public List<DocumentChunk> CreateChunks(
        Guid documentId,
        string text)
    {
        const int chunkSize = 1000;
        const int overlap = 100;

        var chunks = new List<DocumentChunk>();

        if (string.IsNullOrWhiteSpace(text))
        {
            return chunks;
        }

        var start = 0;

        while (start < text.Length)
        {
            var remaining = text.Length - start;

            var length = Math.Min(chunkSize, remaining);

            var content = text.Substring(start, length);

            chunks.Add(new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                ChunkIndex = chunks.Count,
                Content = content,
                CharacterCount = content.Length
            });

            if (start + length >= text.Length)
            {
                break;
            }

            start += chunkSize - overlap;
        }

        return chunks;
    }
}