
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Application.Models;
using EnterpriseAI.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.ClientModel.Primitives;
namespace EnterpriseAI.Infrastructure.Search;

public class AzureDocumentSearchService : IDocumentSearchService
{
    private readonly SearchClient _searchClient;

    public AzureDocumentSearchService(
        IOptions<AzureAISearchOptions> options)
    {
        var settings = options.Value;

        var clientOptions = new AzureOpenAIClientOptions
        {
            RetryPolicy = new ClientRetryPolicy(maxRetries: 3),
            NetworkTimeout = TimeSpan.FromSeconds(60)
        };
        _searchClient = new SearchClient(
            new Uri(settings.Endpoint),
            settings.IndexName,
            new Azure.AzureKeyCredential(settings.ApiKey));
    }

    public async Task<IReadOnlyList<DocumentChunk>> SearchAsync(
        string searchQuery,
        float[] queryEmbedding,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        if (topK <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(topK),
                "topK must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            throw new ArgumentException(
                "Search query cannot be empty.",
                nameof(searchQuery));
        }

        if (queryEmbedding is null ||
            queryEmbedding.Length != 1536 ||
            queryEmbedding.Any(value =>
                float.IsNaN(value) || float.IsInfinity(value)))
        {
            throw new ArgumentException(
                "Query embedding must contain 1536 valid finite values.",
                nameof(queryEmbedding));
        }

        var vectorQuery = new VectorizedQuery(queryEmbedding)
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

        // Hybrid search: keyword search + vector search.
        var response = await _searchClient.SearchAsync<SearchDocument>(
            searchText: searchQuery,
            searchOptions,
            cancellationToken);

        var results = new List<DocumentChunk>();

        await foreach (var result in response.Value.GetResultsAsync())
        {
            var document = result.Document;

            results.Add(new DocumentChunk
            {
                Id = Guid.Parse(document["id"].ToString()!),
                DocumentId = Guid.Parse(document["documentId"].ToString()!),
                ChunkIndex = Convert.ToInt32(document["chunkIndex"]),
                Content = document["content"].ToString()!,
                FileName = document["fileName"].ToString()!,
                CharacterCount = document["content"].ToString()!.Length,
                RelevanceScore = result.Score ?? 0
            });
        }

        return results;
    }
}
