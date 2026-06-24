using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Dhblog.Database;
using Dhblog.Database.Entities;

namespace Dhblog.DataAccess;

public class UserFollowRepository : IUserFollowRepository
{
    private readonly IAmazonDynamoDB _client;
    private readonly TableNameResolver _tables;

    public UserFollowRepository(IAmazonDynamoDB client, TableNameResolver tables)
    {
        _client = client;
        _tables = tables;
    }

    private string Table => _tables.Resolve(TableNames.UserFollows);

    public async Task FollowAsync(UserFollow follow, CancellationToken ct = default)
    {
        await _client.PutItemAsync(Table, new Dictionary<string, AttributeValue>
        {
            ["FollowerId"] = new(follow.FollowerId),
            ["FollowingId"] = new(follow.FollowingId),
            ["CreatedAt"] = new(follow.CreatedAt.ToString("O"))
        }, ct);
    }

    public async Task UnfollowAsync(string followerId, string followingId, CancellationToken ct = default)
    {
        await _client.DeleteItemAsync(Table, new Dictionary<string, AttributeValue>
        {
            ["FollowerId"] = new(followerId),
            ["FollowingId"] = new(followingId)
        }, ct);
    }

    public async Task<bool> IsFollowingAsync(string followerId, string followingId, CancellationToken ct = default)
    {
        var response = await _client.GetItemAsync(Table, new Dictionary<string, AttributeValue>
        {
            ["FollowerId"] = new(followerId),
            ["FollowingId"] = new(followingId)
        }, ct);
        return response.Item.Count > 0;
    }

    public async Task<IReadOnlyList<string>> GetFollowingIdsAsync(string followerId, CancellationToken ct = default)
    {
        var response = await _client.QueryAsync(new QueryRequest
        {
            TableName = Table,
            KeyConditionExpression = "FollowerId = :f",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":f"] = new(followerId) }
        }, ct);
        return response.Items.Select(i => i["FollowingId"].S).ToList();
    }

    public async Task<IReadOnlyList<string>> GetFollowerIdsAsync(string followingId, CancellationToken ct = default)
    {
        var response = await _client.QueryAsync(new QueryRequest
        {
            TableName = Table,
            IndexName = "FollowingIdIndex",
            KeyConditionExpression = "FollowingId = :f",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":f"] = new(followingId) }
        }, ct);
        return response.Items.Select(i => i["FollowerId"].S).ToList();
    }
}

public class PasswordResetRepository : IPasswordResetRepository
{
    private readonly IAmazonDynamoDB _client;
    private readonly TableNameResolver _tables;

    public PasswordResetRepository(IAmazonDynamoDB client, TableNameResolver tables)
    {
        _client = client;
        _tables = tables;
    }

    private string Table => _tables.Resolve(TableNames.PasswordResetTokens);

    public async Task CreateAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        await _client.PutItemAsync(Table, DynamoMapping.ToItem(token), ct);
    }

    public async Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        var response = await _client.QueryAsync(new QueryRequest
        {
            TableName = Table,
            IndexName = "TokenIndex",
            KeyConditionExpression = "Token = :t",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":t"] = new(token) },
            Limit = 1
        }, ct);
        return response.Items.Count == 0 ? null : DynamoMapping.MapPasswordResetToken(response.Items[0]);
    }

    public async Task MarkUsedAsync(string tokenId, CancellationToken ct = default)
    {
        await _client.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = Table,
            Key = new Dictionary<string, AttributeValue> { ["TokenId"] = new(tokenId) },
            UpdateExpression = "SET #u = :true",
            ExpressionAttributeNames = new Dictionary<string, string> { ["#u"] = "Used" },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":true"] = new AttributeValue { BOOL = true } }
        }, ct);
    }
}
