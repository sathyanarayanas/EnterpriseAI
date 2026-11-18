
using Azure;
using Azure.AI.OpenAI;
using EnterpriseAI.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using OpenAI.Embeddings;

namespace EnterpriseAI.Infrastructure.AI;

public class AzureOpenAIEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _embeddingClient;

    public AzureOpenAIEmbeddingService(IConfiguration configuration)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException(
                "Azure OpenAI endpoint is missing.");

        var apiKey = configuration["AzureOpenAI:ApiKey"]
            ?? throw new InvalidOperationException(
                "Azure OpenAI API key is missing.");

        var deploymentName =
            configuration["AzureOpenAI:EmbeddingDeploymentName"]
            ?? throw new InvalidOperationException(
                "Embedding deployment name is missing.");

        var azureClient = new AzureOpenAIClient(
            new Uri(endpoint),
            new AzureKeyCredential(apiKey));

        _embeddingClient =
            azureClient.GetEmbeddingClient(deploymentName);
    }

    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var result = await _embeddingClient.GenerateEmbeddingAsync(
            text,
            cancellationToken: cancellationToken);

        return result.Value.ToFloats().ToArray();
    }
}
