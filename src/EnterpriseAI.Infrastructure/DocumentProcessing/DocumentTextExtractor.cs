using DocumentFormat.OpenXml.Packaging;
using EnterpriseAI.Application.Interfaces;
using UglyToad.PdfPig;

namespace EnterpriseAI.Infrastructure.DocumentProcessing;

public class DocumentTextExtractor : IDocumentTextExtractor
{
    public async Task<string> ExtractTextAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        string extractedText;

        switch (extension)
        {
            case ".pdf":
                extractedText = ExtractPdfText(fileStream);
                break;

            case ".docx":
                extractedText = ExtractDocxText(fileStream);
                break;

            default:
                throw new NotSupportedException(
                    $"File type {extension} is not supported.");
        }

        return await Task.FromResult(extractedText);
    }

    private static string ExtractPdfText(Stream stream)
    {
        using var document = PdfDocument.Open(stream);

        return string.Join(
            Environment.NewLine,
            document.GetPages().Select(page => page.Text));
    }

    private static string ExtractDocxText(Stream stream)
    {
        using var document = WordprocessingDocument.Open(stream, false);

        return document.MainDocumentPart?
            .Document.Body?
            .InnerText ?? string.Empty;
    }
}