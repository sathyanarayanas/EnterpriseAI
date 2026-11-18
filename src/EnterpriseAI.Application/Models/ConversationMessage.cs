namespace EnterpriseAI.Application.Models;

public class ConversationMessage
{
    public string ConversationId { get; set; }
        = string.Empty;

    public string MessageId { get; set; }
        = string.Empty;

    public string Role { get; set; }
        = string.Empty;

    public string Content { get; set; }
        = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}