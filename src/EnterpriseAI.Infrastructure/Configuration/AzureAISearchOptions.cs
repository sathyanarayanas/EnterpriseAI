
namespace EnterpriseAI.Infrastructure.Configuration;

public class AzureAISearchOptions
{
    public string Endpoint { get; set; } = string.Empty;

    public string IndexName { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
    public double MinimumRelevanceScore { get; set; } = 0.62;
}
