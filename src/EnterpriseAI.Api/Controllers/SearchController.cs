using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IDocumentSearchService _documentSearchService;

    public SearchController(
        IEmbeddingService embeddingService,
        IDocumentSearchService documentSearchService)
    {
        _embeddingService = embeddingService;
        _documentSearchService = documentSearchService;
    }

    [HttpPost("vector")]
    public async Task<IActionResult> VectorSearch(
        [FromBody] DocumentSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest("Query is required.");

        var embedding = await _embeddingService.GenerateEmbeddingAsync(
            request.Query,
            cancellationToken);

        var results = await _documentSearchService.SearchAsync(
            request.Query,
            embedding,
            request.TopK,
            cancellationToken);

        return Ok(results);
    }
}