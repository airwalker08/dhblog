using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Dhblog.Database;
using Dhblog.Database.Entities;

namespace Dhblog.DataAccess;

public class TopicRepository : ITopicRepository
{
    private readonly IAmazonDynamoDB _client;
    private readonly TableNameResolver _tables;

    public TopicRepository(IAmazonDynamoDB client, TableNameResolver tables)
    {
        _client = client;
        _tables = tables;
    }

    private string TopicsTable => _tables.Resolve(TableNames.Topics);
    private string EntryTopicsTable => _tables.Resolve(TableNames.BlogEntryTopics);

    public async Task<Topic?> GetByIdAsync(string topicId, CancellationToken ct = default)
    {
        var response = await _client.GetItemAsync(TopicsTable, new Dictionary<string, AttributeValue>
        {
            ["TopicId"] = new(topicId)
        }, ct);
        return response.Item.Count == 0 ? null : DynamoMapping.MapTopic(response.Item);
    }

    public async Task<Topic?> GetByNormalizedKeyAsync(string normalizedKey, CancellationToken ct = default)
    {
        var response = await _client.QueryAsync(new QueryRequest
        {
            TableName = TopicsTable,
            IndexName = "NormalizedKeyIndex",
            KeyConditionExpression = "NormalizedKey = :k",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":k"] = new(normalizedKey) },
            Limit = 1
        }, ct);
        return response.Items.Count == 0 ? null : DynamoMapping.MapTopic(response.Items[0]);
    }

    public async Task<IReadOnlyList<Topic>> SuggestAsync(string prefix, int limit = 10, CancellationToken ct = default)
    {
        var normalized = TopicNormalizer.Normalize(prefix);
        var response = await _client.ScanAsync(new ScanRequest
        {
            TableName = TopicsTable,
            FilterExpression = "contains(NormalizedKey, :p)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":p"] = new(normalized) },
            Limit = limit
        }, ct);
        return response.Items.Select(DynamoMapping.MapTopic).ToList();
    }

    public async Task CreateAsync(Topic topic, CancellationToken ct = default)
    {
        await _client.PutItemAsync(TopicsTable, DynamoMapping.ToItem(topic), ct);
    }

    public async Task<IReadOnlyList<Topic>> ListAsync(CancellationToken ct = default)
    {
        var response = await _client.ScanAsync(new ScanRequest { TableName = TopicsTable }, ct);
        return response.Items.Select(DynamoMapping.MapTopic).OrderBy(t => t.DisplayText).ToList();
    }

    public async Task UpdateAsync(Topic topic, CancellationToken ct = default)
    {
        await _client.PutItemAsync(TopicsTable, DynamoMapping.ToItem(topic), ct);
    }

    public async Task DeleteAsync(string topicId, CancellationToken ct = default)
    {
        await _client.DeleteItemAsync(TopicsTable, new Dictionary<string, AttributeValue>
        {
            ["TopicId"] = new(topicId)
        }, ct);
    }

    public async Task DeleteEntryLinksByTopicIdAsync(string topicId, CancellationToken ct = default)
    {
        var entryIds = await GetEntryIdsByTopicIdAsync(topicId, ct);
        foreach (var entryId in entryIds)
        {
            await _client.DeleteItemAsync(EntryTopicsTable, new Dictionary<string, AttributeValue>
            {
                ["EntryId"] = new(entryId),
                ["TopicId"] = new(topicId)
            }, ct);
        }
    }

    public async Task<IReadOnlyList<Topic>> GetByEntryIdAsync(string entryId, CancellationToken ct = default)
    {
        var links = await _client.QueryAsync(new QueryRequest
        {
            TableName = EntryTopicsTable,
            KeyConditionExpression = "EntryId = :e",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":e"] = new(entryId) }
        }, ct);

        var topics = new List<Topic>();
        foreach (var link in links.Items)
        {
            var topic = await GetByIdAsync(link["TopicId"].S, ct);
            if (topic != null) topics.Add(topic);
        }
        return topics;
    }

    public async Task LinkEntryTopicAsync(string entryId, string topicId, CancellationToken ct = default)
    {
        await _client.PutItemAsync(EntryTopicsTable, new Dictionary<string, AttributeValue>
        {
            ["EntryId"] = new(entryId),
            ["TopicId"] = new(topicId)
        }, ct);
    }

    public async Task<IReadOnlyList<string>> GetEntryIdsByTopicIdAsync(string topicId, CancellationToken ct = default)
    {
        var response = await _client.QueryAsync(new QueryRequest
        {
            TableName = EntryTopicsTable,
            IndexName = "TopicIdIndex",
            KeyConditionExpression = "TopicId = :t",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":t"] = new(topicId) }
        }, ct);
        return response.Items.Select(i => i["EntryId"].S).ToList();
    }
}
