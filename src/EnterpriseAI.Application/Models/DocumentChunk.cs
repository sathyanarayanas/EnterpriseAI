namespace EnterpriseAI.Application.Models;

public class DocumentChunk
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public int CharacterCount { get; set; }

    public float[] Embedding { get; set; } = [];
    public double? RelevanceScore { get; set; }
}