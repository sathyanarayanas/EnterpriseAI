using EnterpriseAI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using EnterpriseAI.Application.Models;

namespace EnterpriseAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    //private readonly IDocumentService _documentService;
    private readonly IDocumentStorageService _storageService;
    private readonly IDocumentIngestionService _ingestionService;
    public DocumentsController(
        //IDocumentService documentService,
        IDocumentStorageService storageService,
        IDocumentIngestionService ingestionService)
    {
       // _documentService = documentService;
        _storageService = storageService;
        _ingestionService = ingestionService;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(
                "Please provide a file.");
        }

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest("Maximum file size is 10 MB.");

        var extension = Path.GetExtension(file.FileName)
            .ToLowerInvariant();

        if (extension != ".pdf" && extension != ".docx")
            return BadRequest("Only PDF and DOCX files are supported.");

        var contentType = extension == ".pdf"
            ? "application/pdf"
            : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        await using var stream = file.OpenReadStream();

        var blobName = await _storageService.UploadAsync(
            stream,
            file.FileName,
            contentType,
            cancellationToken);

        return Ok(new
        {
            Message = "Document uploaded successfully.",
            BlobName = blobName,
            OriginalFileName = Path.GetFileName(file.FileName)
        });
    }

    //[HttpGet]
    //public IActionResult GetDocuments()
    //{
    //    return Ok(
    //        _documentService.GetDocuments());
    //}

    //[HttpGet("{documentId:guid}/chunks")]
    //public IActionResult GetChunks(
    //    Guid documentId)
    //{
    //    return Ok(
    //        _documentService.GetChunks(
    //            documentId));
    //}

    //[HttpGet("search")]
    //public async Task<IActionResult> Search(
    //[FromQuery] string query,
    //CancellationToken cancellationToken)
    //{
    //    if (string.IsNullOrWhiteSpace(query))
    //    {
    //        return BadRequest("Query is required.");
    //    }

    //    var results =
    //        await _documentService.SearchAsync(
    //            query,
    //            cancellationToken);

    //    return Ok(results);
    //}

    [HttpPost("ingest")]
    public async Task<IActionResult> IngestDocument(
    [FromQuery] string blobName,
    [FromQuery] string originalFileName,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(blobName) ||
            string.IsNullOrWhiteSpace(originalFileName))
        {
            return BadRequest(
                "Blob name and original filename are required.");
        }

        var result =
            await _ingestionService.IngestDocumentAsync(
                blobName,
                originalFileName,
                cancellationToken);

        return Ok(result);
    }

}