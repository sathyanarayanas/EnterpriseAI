
using EnterpriseAI.Application.Models;

namespace EnterpriseAI.Application.Interfaces;

public interface IAiService
{
    Task<string> GenerateResponseAsync(
        string message,
        string context,
        IReadOnlyList<ChatMessageContext> history,
        CancellationToken cancellationToken = default);

    Task<string> RewriteQueryAsync(
        string message,
        IReadOnlyList<ChatMessageContext> history,
        CancellationToken cancellationToken = default);
}
