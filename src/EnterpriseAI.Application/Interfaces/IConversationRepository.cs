using EnterpriseAI.Domain.Entities;

namespace EnterpriseAI.Application.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Conversation conversation,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}