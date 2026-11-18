using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Application.Models;

namespace EnterpriseAI.Infrastructure.AI;

public class MockAiService : IAiService
{
    public Task<string> GenerateResponseAsync(
        string message,
    string context,
    IReadOnlyList<ChatMessageContext> history,
    CancellationToken cancellationToken = default)
    {
        var response =
            GenerateMockResponse(message, context, history);

        return Task.FromResult(response);
    }

    private static string GenerateMockResponse(
        string message,
        string context,
        IReadOnlyList<ChatMessageContext> history)
    {
        var lowerMessage =
            message.ToLowerInvariant();

        if (lowerMessage.Contains("dependency injection"))
        {
            return """
                Dependency Injection (DI) is a design pattern
                where a class receives the dependencies it needs
                instead of creating them itself.

                In ASP.NET Core, DI is built into the framework.

                Example:

                public class OrderService
                {
                    private readonly IRepository _repository;

                    public OrderService(
                        IRepository repository)
                    {
                        _repository = repository;
                    }
                }

                This improves testability, loose coupling and
                maintainability.
                """;
        }

        if (lowerMessage.Contains("cqrs"))
        {
            return """
                CQRS stands for Command Query Responsibility
                Segregation.

                It separates operations that change data
                (commands) from operations that read data
                (queries).

                It can be useful when read and write workloads
                have different scalability or modeling needs.
                """;
        }

        if (lowerMessage.Contains("rag"))
        {
            return """
                RAG stands for Retrieval-Augmented Generation.

                A typical RAG pipeline is:

                Document
                  ↓
                Text extraction
                  ↓
                Chunking
                  ↓
                Embeddings
                  ↓
                Vector search
                  ↓
                Relevant context
                  ↓
                LLM
                  ↓
                Grounded answer
                """;
        }

        return $"""
            I am currently running in local development mode.

            You asked:

            "{message}"

            Azure OpenAI is not connected yet.

            Once Azure access is available, this mock provider
            will be replaced by the Azure OpenAI implementation.

            Conversation messages received:
            {history.Count}
            """;
    }

    public Task<string> RewriteQueryAsync(string message, IReadOnlyList<ChatMessageContext> history, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}