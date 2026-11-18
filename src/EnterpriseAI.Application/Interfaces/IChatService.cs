using EnterpriseAI.Application.Models;

namespace EnterpriseAI.Application.Interfaces;

public interface IChatService
{
    Task<ChatResponse> SendMessageAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);
}