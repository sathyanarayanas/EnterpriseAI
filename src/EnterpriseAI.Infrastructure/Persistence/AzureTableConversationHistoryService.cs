using Azure;
using Azure.Data.Tables;
using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Application.Models;
using EnterpriseAI.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace EnterpriseAI.Infrastructure.Persistence;

public class AzureTableConversationHistoryService
    : IConversationHistoryService
{
    private readonly TableClient _tableClient;

    public AzureTableConversationHistoryService(
        IOptions<AzureTableStorageOptions> options)
    {
        var settings = options.Value;

        var serviceClient = new TableServiceClient(
            settings.ConnectionString);

        _tableClient = serviceClient.GetTableClient(
            settings.TableName);
    }

    public async Task SaveMessageAsync(
        ConversationMessage message,
        CancellationToken cancellationToken = default)
    {
        var entity = new TableEntity(
            message.ConversationId,
            message.MessageId)
        {
            ["Role"] = message.Role,
            ["Content"] = message.Content,
            ["CreatedAtUtc"] = message.CreatedAtUtc
        };

        await _tableClient.AddEntityAsync(
            entity,
            cancellationToken);
    }


    public async Task<IReadOnlyList<ChatMessageContext>>
        GetHistoryAsync(
            string conversationId,
            int take = 10,
            CancellationToken cancellationToken = default)
    {
        var history = new List<(string RowKey, ChatMessageContext Message)>();

        var query = _tableClient.QueryAsync<TableEntity>(
            filter: TableClient.CreateQueryFilter(
                $"PartitionKey eq {conversationId}"),
            cancellationToken: cancellationToken);

        await foreach (var entity in query.WithCancellation(cancellationToken))
        {
            history.Add((
                entity.RowKey,
                new ChatMessageContext
                {
                    Role = entity.GetString("Role") ?? string.Empty,
                    Content = entity.GetString("Content") ?? string.Empty
                }));
        }

        return history
            .OrderByDescending(x => x.RowKey)
            .Take(take)
            .OrderBy(x => x.RowKey)
            .Select(x => x.Message)
            .ToList();
    }

}