namespace EnterpriseAI.Application.Models;

public class DocumentIngestionResult
{
    public Guid DocumentId { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public int TotalCharacters { get; set; }

    public int TotalChunks { get; set; }

    public List<DocumentChunk> Chunks { get; set; } = [];
}