using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Dhblog.Database;
using Dhblog.Database.Entities;

namespace Dhblog.DataAccess;

public class UserRepository : IUserRepository
{
    private readonly IAmazonDynamoDB _client;
    private readonly TableNameResolver _tables;

    public UserRepository(IAmazonDynamoDB client, TableNameResolver tables)
    {
        _client = client;
        _tables = tables;
    }

    private string Table => _tables.Resolve(TableNames.Users);

    public async Task<User?> GetByIdAsync(string userId, CancellationToken ct = default)
    {
        var response = await _client.GetItemAsync(Table, new Dictionary<string, AttributeValue>
        {
            ["UserId"] = new(userId)
        }, ct);

        return response.Item.Count == 0 ? null : DynamoMapping.MapUser(response.Item);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var response = await _client.QueryAsync(new QueryRequest
        {
            TableName = Table,
            IndexName = "UsernameIndex",
            KeyConditionExpression = "Username = :u",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":u"] = new(username)
            },
            Limit = 1
        }, ct);

        return response.Items.Count == 0 ? null : DynamoMapping.MapUser(response.Items[0]);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var response = await _client.QueryAsync(new QueryRequest
        {
            TableName = Table,
            IndexName = "EmailIndex",
            KeyConditionExpression = "Email = :e",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":e"] = new(email)
            },
            Limit = 1
        }, ct);

        return response.Items.Count == 0 ? null : DynamoMapping.MapUser(response.Items[0]);
    }

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default)
    {
        var response = await _client.ScanAsync(new ScanRequest { TableName = Table }, ct);
        return response.Items.Select(DynamoMapping.MapUser).ToList();
    }

    public async Task CreateAsync(User user, CancellationToken ct = default)
    {
        await _client.PutItemAsync(Table, DynamoMapping.ToItem(user), ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        await _client.PutItemAsync(Table, DynamoMapping.ToItem(user), ct);
    }

    public async Task DeleteAsync(string userId, CancellationToken ct = default)
    {
        await _client.DeleteItemAsync(Table, new Dictionary<string, AttributeValue>
        {
            ["UserId"] = new(userId)
        }, ct);
    }
}
