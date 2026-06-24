using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Dhblog.Database;
using Dhblog.Database.Entities;

namespace Dhblog.DataAccess;

public class BlogRepository : IBlogRepository
{
    private readonly IAmazonDynamoDB _client;
    private readonly TableNameResolver _tables;

    public BlogRepository(IAmazonDynamoDB client, TableNameResolver tables)
    {
        _client = client;
        _tables = tables;
    }

    private string EntriesTable => _tables.Resolve(TableNames.BlogEntries);
    private string ImagesTable => _tables.Resolve(TableNames.BlogImages);

    public async Task<BlogEntry?> GetEntryByIdAsync(string entryId, CancellationToken ct = default)
    {
        var response = await _client.GetItemAsync(EntriesTable, new Dictionary<string, AttributeValue>
        {
            ["EntryId"] = new(entryId)
        }, ct);
        return response.Item.Count == 0 ? null : DynamoMapping.MapBlogEntry(response.Item);
    }

    public async Task<IReadOnlyList<BlogEntry>> GetEntriesByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var response = await _client.QueryAsync(new QueryRequest
        {
            TableName = EntriesTable,
            IndexName = "UserIdCreatedAtIndex",
            KeyConditionExpression = "UserId = :u",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":u"] = new(userId) },
            ScanIndexForward = false
        }, ct);
        return response.Items.Select(DynamoMapping.MapBlogEntry).ToList();
    }

    public async Task<IReadOnlyList<BlogEntry>> GetEntriesByUserIdsAsync(IEnumerable<string> userIds, CancellationToken ct = default)
    {
        var all = new List<BlogEntry>();
        foreach (var userId in userIds.Distinct())
        {
            all.AddRange(await GetEntriesByUserIdAsync(userId, ct));
        }
        return all.OrderByDescending(e => e.CreatedAt).ToList();
    }

    public async Task CreateEntryAsync(BlogEntry entry, CancellationToken ct = default)
    {
        await _client.PutItemAsync(EntriesTable, DynamoMapping.ToItem(entry), ct);
    }

    public async Task UpdateEntryAsync(BlogEntry entry, CancellationToken ct = default)
    {
        await _client.PutItemAsync(EntriesTable, DynamoMapping.ToItem(entry), ct);
    }

    public async Task DeleteEntryAsync(string entryId, CancellationToken ct = default)
    {
        await _client.DeleteItemAsync(EntriesTable, new Dictionary<string, AttributeValue>
        {
            ["EntryId"] = new(entryId)
        }, ct);
    }

    public async Task<IReadOnlyList<BlogImage>> GetImagesByEntryIdAsync(string entryId, CancellationToken ct = default)
    {
        var response = await _client.QueryAsync(new QueryRequest
        {
            TableName = ImagesTable,
            IndexName = "EntryIdIndex",
            KeyConditionExpression = "EntryId = :e",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":e"] = new(entryId) }
        }, ct);
        return response.Items.Select(DynamoMapping.MapBlogImage).OrderBy(i => i.SortOrder).ToList();
    }

    public async Task CreateImageAsync(BlogImage image, CancellationToken ct = default)
    {
        await _client.PutItemAsync(ImagesTable, DynamoMapping.ToItem(image), ct);
    }

    public async Task DeleteImageAsync(string imageId, CancellationToken ct = default)
    {
        await _client.DeleteItemAsync(ImagesTable, new Dictionary<string, AttributeValue>
        {
            ["ImageId"] = new(imageId)
        }, ct);
    }
}
