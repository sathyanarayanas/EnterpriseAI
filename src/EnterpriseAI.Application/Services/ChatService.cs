
using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Application.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
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
        cancellationToken.ThrowIfCancellationRequested();

        var correlationId =
            Activity.Current?.TraceId.ToString()
            ?? Guid.NewGuid().ToString("N");

        _logger.LogInformation(
            "Chat request started. CorrelationId: {CorrelationId}",
            correlationId);

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
            "Conversation history retrieval completed. CorrelationId: {CorrelationId}, ConversationId: {ConversationId}, MessagesRetrieved: {MessagesRetrieved}, LatencyMs: {LatencyMs}",
            correlationId,
            conversationId,
            history.Count,
            historyStopwatch.ElapsedMilliseconds);

        // Step 2: Save the current user message.
        var userSaveStopwatch = Stopwatch.StartNew();

        await _conversationHistoryService.SaveMessageAsync(
            new ConversationMessage
            {
                ConversationId = conversationId,
                MessageId = $"{DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks:D19}_{Guid.NewGuid():N}",
                Role = "user",
                Content = request.Message,
                CreatedAtUtc = DateTime.UtcNow
            },
            cancellationToken);

        userSaveStopwatch.Stop();

        _logger.LogInformation(
            "User message storage completed. CorrelationId: {CorrelationId}, ConversationId: {ConversationId}, LatencyMs: {LatencyMs}",
            correlationId,
            conversationId,
            userSaveStopwatch.ElapsedMilliseconds);

        // Step 3: Rewrite the question only when it appears to be a follow-up.
        string searchQuery;

        if (history.Count == 0)
        {
            searchQuery = request.Message;

            _logger.LogInformation(
                "Query rewriting skipped because conversation history is empty. CorrelationId: {CorrelationId}, ConversationId: {ConversationId}",
                correlationId,
                conversationId);
        }
        else if (!RequiresQueryRewriting(request.Message))
        {
            searchQuery = request.Message;

            _logger.LogInformation(
                "Query rewriting skipped because the message appears to be standalone. CorrelationId: {CorrelationId}, ConversationId: {ConversationId}",
                correlationId,
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
                "Query rewriting completed. CorrelationId: {CorrelationId}, ConversationId: {ConversationId}, LatencyMs: {LatencyMs}",
                correlationId,
                conversationId,
                rewriteStopwatch.ElapsedMilliseconds);
        }

        _logger.LogInformation(
            "Query processed. CorrelationId: {CorrelationId}, ConversationId: {ConversationId}, OriginalQueryLength: {OriginalQueryLength}, SearchQueryLength: {SearchQueryLength}",
            correlationId,
            conversationId,
            request.Message.Length,
            searchQuery.Length);

        // Step 4: Generate embedding from the search query.
        var embeddingStopwatch = Stopwatch.StartNew();

        var queryEmbedding =
            await _embeddingService.GenerateEmbeddingAsync(
                searchQuery,
                cancellationToken);

        embeddingStopwatch.Stop();

        _logger.LogInformation(
            "Query embedding generation completed. CorrelationId: {CorrelationId}, ConversationId: {ConversationId}, LatencyMs: {LatencyMs}",
            correlationId,
            conversationId,
            embeddingStopwatch.ElapsedMilliseconds);

        // Step 5: Retrieve relevant document chunks.
        var searchStopwatch = Stopwatch.StartNew();

        var retrievedChunks =
            await _documentSearchService.SearchAsync(
                searchQuery,
                queryEmbedding,
                topK: 5,
                cancellationToken);

        searchStopwatch.Stop();

        _logger.LogInformation(
            "Document search completed. CorrelationId: {CorrelationId}, ConversationId: {ConversationId}, RetrievedChunks: {RetrievedChunks}, LatencyMs: {LatencyMs}",
            correlationId,
            conversationId,
            retrievedChunks.Count,
            searchStopwatch.ElapsedMilliseconds);

        foreach (var source in retrievedChunks
            .Select(x => x.FileName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct())
        {
            _logger.LogInformation(
                "Retrieved source document. CorrelationId: {CorrelationId}, ConversationId: {ConversationId}, Source: {Source}",
                correlationId,
                conversationId,
                source);
        }

        foreach (var chunk in retrievedChunks)
        {
            _logger.LogInformation(
                "Retrieved chunk. CorrelationId: {CorrelationId}, ConversationId: {ConversationId}, FileName: {FileName}, ChunkIndex: {ChunkIndex}, RelevanceScore: {RelevanceScore}",
                correlationId,
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
                "Retrieval quality metrics. CorrelationId: {CorrelationId}, ConversationId: {ConversationId}, AverageScore: {AverageScore}, MaxScore: {MaxScore}, MinScore: {MinScore}",
                correlationId,
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
                    MessageId = $"{DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks:D19}_{Guid.NewGuid():N}",
                    Role = "assistant",
                    Content = fallbackAnswer,
                    CreatedAtUtc = DateTime.UtcNow
                },
                cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "RAG pipeline completed without relevant documents. CorrelationId: {CorrelationId}, ConversationId: {ConversationId}, TotalLatencyMs: {TotalLatencyMs}",
                correlationId,
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
        cancellationToken.ThrowIfCancellationRequested();

        var answerStopwatch = Stopwatch.StartNew();

        var answer =
            await _aiService.GenerateResponseAsync(
                request.Message,
                context,
                history,
                cancellationToken);

        answerStopwatch.Stop();

        _logger.LogInformation(
            "AI answer generation completed. CorrelationId: {CorrelationId}, ConversationId: {ConversationId}, LatencyMs: {LatencyMs}",
            correlationId,
            conversationId,
            answerStopwatch.ElapsedMilliseconds);

        // Step 9: Save assistant response.
        var assistantSaveStopwatch = Stopwatch.StartNew();

        await _conversationHistoryService.SaveMessageAsync(
            new ConversationMessage
            {
                ConversationId = conversationId,
                MessageId = $"{DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks:D19}_{Guid.NewGuid():N}",
                Role = "assistant",
                Content = answer,
                CreatedAtUtc = DateTime.UtcNow
            },
            cancellationToken);

        assistantSaveStopwatch.Stop();

        _logger.LogInformation(
            "Assistant message storage completed. CorrelationId: {CorrelationId}, ConversationId: {ConversationId}, LatencyMs: {LatencyMs}",
            correlationId,
            conversationId,
            assistantSaveStopwatch.ElapsedMilliseconds);

        // Step 10: Log end-to-end RAG latency.
        stopwatch.Stop();

        _logger.LogInformation(
            "RAG pipeline completed successfully. CorrelationId: {CorrelationId}, ConversationId: {ConversationId}, RetrievedChunks: {RetrievedChunks}, TotalLatencyMs: {TotalLatencyMs}",
            correlationId,
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


    // Identifies common contextual follow-up question patterns.
    private static bool RequiresQueryRewriting(string message)
    {
        var followUpIndicators = new[]
        {
            "who should i contact",
            "what about",
            "how about",
            "does that",
            "is that",
            "can they",
            "should i",
            "what if",
            "and what",
            "it ",
            "that ",
            "those ",
            "them ",
            "this ",
            "these ",
            "they ",
            "he ",
            "she "
        };

        var normalizedMessage = message
            .Trim()
            .ToLowerInvariant();

        return followUpIndicators.Any(indicator =>
            normalizedMessage.StartsWith(indicator));
    }
}
