using EnterpriseAI.Application.Models;

namespace EnterpriseAI.Application.Validation;

public static class ChatRequestValidator
{
    public static string? Validate(ChatRequest request)
    {
        if (request == null)
        {
            return "Request is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return "Message is required.";
        }

        if (request.Message.Length > 4000)
        {
            return "Message cannot exceed 4000 characters.";
        }

        return null;
    }
}