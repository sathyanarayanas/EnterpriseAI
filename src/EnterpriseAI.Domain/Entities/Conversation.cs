namespace EnterpriseAI.Domain.Entities;

public class Conversation
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<ChatMessage> Messages { get; set; }
        = new List<ChatMessage>();
}