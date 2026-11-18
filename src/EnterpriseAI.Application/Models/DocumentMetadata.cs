namespace EnterpriseAI.Application.Models;

public class DocumentMetadata
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string OriginalFileName { get; set; } = string.Empty;

    public string BlobName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = "Uploaded";
}