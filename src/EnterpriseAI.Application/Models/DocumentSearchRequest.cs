namespace EnterpriseAI.Application.Models;

public class DocumentSearchRequest
{
    public string Query { get; set; } = string.Empty;

    public int TopK { get; set; } = 5;
}