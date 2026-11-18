
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Application.Models;
using EnterpriseAI.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace EnterpriseAI.Infrastructure.Search;

public class AzureDocumentSearchService : IDocumentSearchService
{
    private readonly SearchClient _searchClient;
    private readonly double _minimumRelevanceScore;
    public AzureDocumentSearchService(
        IOptions<AzureAISearchOptions> options)
    {
        var settings = options.Value;
        _minimumRelevanceScore = settings.MinimumRelevanceScore;
        _searchClient = new SearchClient(
            new Uri(settings.Endpoint),
            settings.IndexName,
            new Azure.AzureKeyCredential(settings.ApiKey));
    }

    public async Task<IReadOnlyList<DocumentChunk>> SearchAsync(
        float[] queryEmbedding,
        int topK = 5,
        CancellationToken cancellationToken = default)
            {
                if (queryEmbedding is null || queryEmbedding.Length != 1536 || queryEmbedding.Any(value => float.IsNaN(value) 
                || float.IsInfinity(value)))
                {
                    throw new ArgumentException(
                        "Query embedding must contain 1536 valid finite values.",
                        nameof(queryEmbedding));
        }       

        var vectorQuery = new VectorizedQuery(
            queryEmbedding)
        {
            KNearestNeighborsCount = topK,
            Fields = { "contentVector" }
        };

        var searchOptions = new SearchOptions
        {
            Size = topK
        };

        searchOptions.VectorSearch = new VectorSearchOptions();
        searchOptions.VectorSearch.Queries.Add(vectorQuery);

        searchOptions.Select.Add("id");
        searchOptions.Select.Add("documentId");
        searchOptions.Select.Add("fileName");
        searchOptions.Select.Add("chunkIndex");
        searchOptions.Select.Add("content");

        var response = await _searchClient.SearchAsync<SearchDocument>(
            searchText: null,
            searchOptions,
            cancellationToken);

        var results = new List<DocumentChunk>();

        await foreach (var result in response.Value.GetResultsAsync())
        {
            var relevanceScore = result.Score;

            if (relevanceScore < _minimumRelevanceScore)
                continue;

            var document = result.Document;

            results.Add(new DocumentChunk
            {
                Id = Guid.Parse(document["id"].ToString()!),
                DocumentId = Guid.Parse(document["documentId"].ToString()!),
                ChunkIndex = Convert.ToInt32(document["chunkIndex"]),
                Content = document["content"].ToString()!,
                FileName = document["fileName"].ToString()!,
                CharacterCount = document["content"].ToString()!.Length,
                RelevanceScore = relevanceScore.Value
            });
        }

        return results;

    }
}
