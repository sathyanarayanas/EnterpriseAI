using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Application.Models;

namespace EnterpriseAI.Infrastructure.DocumentProcessing;

public class DocumentIngestionService : IDocumentIngestionService
{
    private readonly IDocumentStorageService _storageService;
    private readonly IDocumentTextExtractor _textExtractor;
    private readonly IDocumentChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IDocumentSearchIndexService _searchIndexService;
    public DocumentIngestionService(
        IDocumentStorageService storageService,
        IDocumentTextExtractor textExtractor,
        IDocumentChunkingService chunkingService,
        IEmbeddingService embeddingService,
        IDocumentSearchIndexService searchIndexService)
    {
        _storageService = storageService;
        _textExtractor = textExtractor;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _searchIndexService = searchIndexService;
    }

    public async Task<DocumentIngestionResult> IngestDocumentAsync(
    string blobName,
    string originalFileName,
    CancellationToken cancellationToken = default)
    {
        await using var documentStream =
            await _storageService.DownloadAsync(
                blobName,
                cancellationToken);

        var extractedText =
            await _textExtractor.ExtractTextAsync(
                documentStream,
                originalFileName,
                cancellationToken);

        if (string.IsNullOrWhiteSpace(extractedText))
        {
            throw new InvalidOperationException(
                "No readable text was extracted from the document.");
        }

        var documentId = Guid.NewGuid();

        var chunks = _chunkingService.CreateChunks(
            documentId,
            extractedText);

        foreach (var chunk in chunks)
        {
            chunk.Embedding =
                await _embeddingService.GenerateEmbeddingAsync(
                    chunk.Content,
                    cancellationToken);
        }

        await _searchIndexService.IndexChunksAsync(
            chunks,
            originalFileName,
            cancellationToken);
        return new DocumentIngestionResult
        {
            DocumentId = documentId,
            OriginalFileName = originalFileName,
            TotalCharacters = extractedText.Length,
            TotalChunks = chunks.Count,
            Chunks = chunks
        };
    }
}