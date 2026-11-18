
using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Application.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EnterpriseAI.Application.Services;

public class ChatService : IChatService
{
    private readonly IAiService _aiService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IDocumentSearchService _documentSearchService;
    private readonly IConversationHistoryService _conversationHistoryService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IAiService aiService,
        IEmbeddingService embeddingService,
        IDocumentSearchService documentSearchService,
        IConversationHistoryService conversationHistoryService,
        ILogger<ChatService> logger)
    {
        _aiService = aiService;
        _embeddingService = embeddingService;
        _documentSearchService = documentSearchService;
        _conversationHistoryService = conversationHistoryService;
        _logger = logger;
    }

    public async Task<ChatResponse> SendMessageAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException(
                "Message cannot be empty.",
                nameof(request.Message));
        }

        // Start end-to-end RAG pipeline timer.
        var stopwatch = Stopwatch.StartNew();

        var conversationId =
            string.IsNullOrWhiteSpace(request.ConversationId)
                ? Guid.NewGuid().ToString()
                : request.ConversationId;

        // Step 1: Retrieve previous conversation history.
        var historyStopwatch = Stopwatch.StartNew();

        var history =
            await _conversationHistoryService.GetHistoryAsync(
                conversationId,
                take: 6,
                cancellationToken);

        historyStopwatch.Stop();

        _logger.LogInformation(
            "Conversation history retrieval completed. ConversationId: {ConversationId}, MessagesRetrieved: {MessagesRetrieved}, LatencyMs: {LatencyMs}",
            conversationId,
            history.Count,
            historyStopwatch.ElapsedMilliseconds);
        // Step 2: Save the current user message.
        var userSaveStopwatch = Stopwatch.StartNew();

        await _conversationHistoryService.SaveMessageAsync(
            new ConversationMessage
            {
                ConversationId = conversationId,
                MessageId = Guid.NewGuid().ToString(),
                Role = "user",
                Content = request.Message,
                CreatedAtUtc = DateTime.UtcNow
            },
            cancellationToken);

        userSaveStopwatch.Stop();

        _logger.LogInformation(
            "User message storage completed. ConversationId: {ConversationId}, LatencyMs: {LatencyMs}",
            conversationId,
            userSaveStopwatch.ElapsedMilliseconds);

        // Step 3: Rewrite the question using conversation history.
        string searchQuery;

        if (history.Count == 0)
        {
            searchQuery = request.Message;

            _logger.LogInformation(
                "Query rewriting skipped because conversation history is empty. ConversationId: {ConversationId}",
                conversationId);
        }
        else
        {
            var rewriteStopwatch = Stopwatch.StartNew();

            searchQuery =
                await _aiService.RewriteQueryAsync(
                    request.Message,
                    history,
                    cancellationToken);

            rewriteStopwatch.Stop();

            _logger.LogInformation(
                "Query rewriting completed. ConversationId: {ConversationId}, LatencyMs: {LatencyMs}",
                conversationId,
                rewriteStopwatch.ElapsedMilliseconds);
        }

        _logger.LogInformation(
            "Query rewritten. ConversationId: {ConversationId}, OriginalQuery: {OriginalQuery}, RewrittenQuery: {RewrittenQuery}",
            conversationId,
            request.Message,
            searchQuery);

        // Step 4: Generate embedding from rewritten query.
        var embeddingStopwatch = Stopwatch.StartNew();

        var queryEmbedding =
            await _embeddingService.GenerateEmbeddingAsync(
                searchQuery,
                cancellationToken);

        embeddingStopwatch.Stop();

        _logger.LogInformation(
            "Query embedding generation completed. ConversationId: {ConversationId}, LatencyMs: {LatencyMs}",
            conversationId,
            embeddingStopwatch.ElapsedMilliseconds);

        // Step 5: Retrieve relevant document chunks.
        var searchStopwatch = Stopwatch.StartNew();

        var retrievedChunks =
            await _documentSearchService.SearchAsync(
                queryEmbedding,
                topK: 5,
                cancellationToken);

        searchStopwatch.Stop();

        _logger.LogInformation(
            "Document search completed. ConversationId: {ConversationId}, RetrievedChunks: {RetrievedChunks}, LatencyMs: {LatencyMs}",
            conversationId,
            retrievedChunks.Count,
            searchStopwatch.ElapsedMilliseconds);

        _logger.LogInformation(
            "Document retrieval completed. ConversationId: {ConversationId}, RetrievedChunks: {RetrievedChunks}",
            conversationId,
            retrievedChunks.Count);

        foreach (var source in retrievedChunks
            .Select(x => x.FileName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct())
        {
            _logger.LogInformation(
                "Retrieved source document: {Source}",
                source);
        }

        foreach (var chunk in retrievedChunks)
        {
            _logger.LogInformation(
                "Retrieved chunk. ConversationId: {ConversationId}, FileName: {FileName}, ChunkIndex: {ChunkIndex}, RelevanceScore: {RelevanceScore}",
                conversationId,
                chunk.FileName,
                chunk.ChunkIndex,
                chunk.RelevanceScore);
        }

        if (retrievedChunks.Count > 0)
        {
            var scores = retrievedChunks
                .Select(x => x.RelevanceScore)
                .ToList();

            _logger.LogInformation(
                "Retrieval quality metrics. ConversationId: {ConversationId}, AverageScore: {AverageScore}, MaxScore: {MaxScore}, MinScore: {MinScore}",
                conversationId,
                scores.Average(),
                scores.Max(),
                scores.Min());
        }

        // Step 6: Handle missing relevant documents.
        if (retrievedChunks.Count == 0)
        {
            const string fallbackAnswer =
                "I couldn't find relevant information in the available enterprise documents.";

            await _conversationHistoryService.SaveMessageAsync(
                new ConversationMessage
                {
                    ConversationId = conversationId,
                    MessageId = $"{DateTime.UtcNow:yyyyMMddHHmmssfffffff}_{Guid.NewGuid():N}",
                    Role = "assistant",
                    Content = fallbackAnswer,
                    CreatedAtUtc = DateTime.UtcNow
                },
                cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "RAG pipeline completed without relevant documents. ConversationId: {ConversationId}, TotalLatencyMs: {TotalLatencyMs}",
                conversationId,
                stopwatch.ElapsedMilliseconds);

            return new ChatResponse
            {
                Answer = fallbackAnswer,
                ConversationId = conversationId,
                Sources = []
            };
        }

        // Step 7: Build context from retrieved chunks.
        var context = string.Join(
            "\n\n",
            retrievedChunks.Select(chunk =>
                $"""
                Source: {chunk.FileName}
                Content:
                {chunk.Content}
                """));

        // Step 8: Generate grounded AI response.
        var answerStopwatch = Stopwatch.StartNew();

        var answer =
            await _aiService.GenerateResponseAsync(
                request.Message,
                context,
                history,
                cancellationToken);

        answerStopwatch.Stop();

        _logger.LogInformation(
            "AI answer generation completed. ConversationId: {ConversationId}, LatencyMs: {LatencyMs}",
            conversationId,
            answerStopwatch.ElapsedMilliseconds);

        // Step 9: Save assistant response.
        // Step 9: Save assistant response and measure storage latency.
        var assistantSaveStopwatch = Stopwatch.StartNew();

        await _conversationHistoryService.SaveMessageAsync(
            new ConversationMessage
            {
                ConversationId = conversationId,
                MessageId = $"{DateTime.UtcNow:yyyyMMddHHmmssfffffff}_{Guid.NewGuid():N}",
                Role = "assistant",
                Content = answer,
                CreatedAtUtc = DateTime.UtcNow
            },
            cancellationToken);

        assistantSaveStopwatch.Stop();

        _logger.LogInformation(
            "Assistant message storage completed. ConversationId: {ConversationId}, LatencyMs: {LatencyMs}",
            conversationId,
            assistantSaveStopwatch.ElapsedMilliseconds);

        // Step 10: Log successful end-to-end RAG latency.
        stopwatch.Stop();

        _logger.LogInformation(
            "RAG pipeline completed successfully. ConversationId: {ConversationId}, RetrievedChunks: {RetrievedChunks}, TotalLatencyMs: {TotalLatencyMs}",
            conversationId,
            retrievedChunks.Count,
            stopwatch.ElapsedMilliseconds);

        return new ChatResponse
        {
            Answer = answer,
            ConversationId = conversationId,
            Sources = retrievedChunks
                .Where(chunk =>
                    !string.IsNullOrWhiteSpace(chunk.FileName))
                .Select(chunk => chunk.FileName)
                .Distinct()
                .ToList()
        };
    }
}
