namespace EnterpriseAI.Application.Models;

public class ChatResponse
{
    public string Answer { get; set; } = string.Empty;

    public List<string> Sources { get; set; } = new List<string>();

    public string ConversationId { get; set; } = string.Empty;

    public int? InputTokens { get; set; }

    public int? OutputTokens { get; set; }

    public string Model { get; set; } = string.Empty;
}