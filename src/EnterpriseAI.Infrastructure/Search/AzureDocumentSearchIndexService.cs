
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Application.Models;
using EnterpriseAI.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace EnterpriseAI.Infrastructure.Search;

public class AzureDocumentSearchIndexService
    : IDocumentSearchIndexService
{
    private readonly SearchClient _searchClient;

    public AzureDocumentSearchIndexService(
        IOptions<AzureAISearchOptions> options)
    {
        var settings = options.Value;

        _searchClient = new SearchClient(
            new Uri(settings.Endpoint),
            settings.IndexName,
            new AzureKeyCredential(settings.ApiKey));
    }

    public async Task IndexChunksAsync(
        IReadOnlyList<DocumentChunk> chunks,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0)
            return;

        var documents = chunks.Select(chunk =>
            new SearchDocument
            {
                ["id"] = chunk.Id.ToString(),
                ["documentId"] = chunk.DocumentId.ToString(),
                ["fileName"] = fileName,
                ["chunkIndex"] = chunk.ChunkIndex,
                ["content"] = chunk.Content,
                ["contentVector"] =
                    new ReadOnlyMemory<float>(chunk.Embedding)
            }).ToList();

        var batch = IndexDocumentsBatch.Upload(documents);

        await _searchClient.IndexDocumentsAsync(
            batch,
            cancellationToken: cancellationToken);
    }
}
