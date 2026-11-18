using EnterpriseAI.Application.Models;

namespace EnterpriseAI.Application.Interfaces;

public interface IConversationHistoryService
{
    Task SaveMessageAsync(
        ConversationMessage message,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatMessageContext>> GetHistoryAsync(
        string conversationId,
        int take = 10,
        CancellationToken cancellationToken = default);
}