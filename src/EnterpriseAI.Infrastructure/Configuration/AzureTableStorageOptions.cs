namespace EnterpriseAI.Infrastructure.Configuration;

public class AzureTableStorageOptions
{
    public string ConnectionString { get; set; }
        = string.Empty;

    public string TableName { get; set; }
        = "EPKAConversations";
}