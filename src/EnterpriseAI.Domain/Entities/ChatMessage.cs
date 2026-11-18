namespace EnterpriseAI.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Prevent back-reference cycle during JSON serialization / OpenAPI schema generation
    [System.Text.Json.Serialization.JsonIgnore]
    public Conversation? Conversation { get; set; }
}