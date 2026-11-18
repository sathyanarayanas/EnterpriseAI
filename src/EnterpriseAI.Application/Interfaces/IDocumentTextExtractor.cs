namespace EnterpriseAI.Application.Interfaces;

public interface IDocumentTextExtractor
{
    Task<string> ExtractTextAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);
}