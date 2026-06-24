using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Dhblog.Database;
using Dhblog.Database.Entities;

namespace Dhblog.DataAccess;

public class RoleRepository : IRoleRepository
{
    private readonly IAmazonDynamoDB _client;
    private readonly TableNameResolver _tables;

    public RoleRepository(IAmazonDynamoDB client, TableNameResolver tables)
    {
        _client = client;
        _tables = tables;
    }

    public async Task<Role?> GetByIdAsync(string roleId, CancellationToken ct = default)
    {
        var response = await _client.GetItemAsync(_tables.Resolve(TableNames.Roles), new Dictionary<string, AttributeValue>
        {
            ["RoleId"] = new(roleId)
        }, ct);
        return response.Item.Count == 0 ? null : DynamoMapping.MapRole(response.Item);
    }

    public async Task<IReadOnlyList<Role>> ListAsync(CancellationToken ct = default)
    {
        var response = await _client.ScanAsync(new ScanRequest { TableName = _tables.Resolve(TableNames.Roles) }, ct);
        return response.Items.Select(DynamoMapping.MapRole).ToList();
    }

    public async Task CreateAsync(Role role, CancellationToken ct = default)
    {
        await _client.PutItemAsync(_tables.Resolve(TableNames.Roles), new Dictionary<string, AttributeValue>
        {
            ["RoleId"] = new(role.RoleId),
            ["Name"] = new(role.Name),
            ["Description"] = new(role.Description)
        }, ct);
    }

    public async Task UpdateAsync(Role role, CancellationToken ct = default) => await CreateAsync(role, ct);

    public async Task DeleteAsync(string roleId, CancellationToken ct = default)
    {
        await _client.DeleteItemAsync(_tables.Resolve(TableNames.Roles), new Dictionary<string, AttributeValue>
        {
            ["RoleId"] = new(roleId)
        }, ct);
    }
}

public class FeatureRepository : IFeatureRepository
{
    private readonly IAmazonDynamoDB _client;
    private readonly TableNameResolver _tables;

    public FeatureRepository(IAmazonDynamoDB client, TableNameResolver tables)
    {
        _client = client;
        _tables = tables;
    }

    private string Table => _tables.Resolve(TableNames.Features);

    public async Task<Feature?> GetByIdAsync(string featureId, CancellationToken ct = default)
    {
        var response = await _client.GetItemAsync(Table, new Dictionary<string, AttributeValue>
        {
            ["FeatureId"] = new(featureId)
        }, ct);
        return response.Item.Count == 0 ? null : DynamoMapping.MapFeature(response.Item);
    }

    public async Task<Feature?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var response = await _client.QueryAsync(new QueryRequest
        {
            TableName = Table,
            IndexName = "FeatureCodeIndex",
            KeyConditionExpression = "Code = :c",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":c"] = new(code) },
            Limit = 1
        }, ct);
        return response.Items.Count == 0 ? null : DynamoMapping.MapFeature(response.Items[0]);
    }

    public async Task<IReadOnlyList<Feature>> ListAsync(CancellationToken ct = default)
    {
        var response = await _client.ScanAsync(new ScanRequest { TableName = Table }, ct);
        return response.Items.Select(DynamoMapping.MapFeature).OrderBy(f => f.SortOrder).ToList();
    }
}

public class FeatureRoleRepository : IFeatureRoleRepository
{
    private readonly IAmazonDynamoDB _client;
    private readonly TableNameResolver _tables;

    public FeatureRoleRepository(IAmazonDynamoDB client, TableNameResolver tables)
    {
        _client = client;
        _tables = tables;
    }

    public async Task<IReadOnlyList<FeatureRole>> GetByRoleIdAsync(string roleId, CancellationToken ct = default)
    {
        var response = await _client.QueryAsync(new QueryRequest
        {
            TableName = _tables.Resolve(TableNames.FeatureRoles),
            KeyConditionExpression = "RoleId = :r",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":r"] = new(roleId) }
        }, ct);
        return response.Items.Select(DynamoMapping.MapFeatureRole).ToList();
    }

    public async Task CreateAsync(FeatureRole featureRole, CancellationToken ct = default)
    {
        await _client.PutItemAsync(_tables.Resolve(TableNames.FeatureRoles), new Dictionary<string, AttributeValue>
        {
            ["RoleId"] = new(featureRole.RoleId),
            ["FeatureId"] = new(featureRole.FeatureId),
            ["Permissions"] = new(featureRole.Permissions)
        }, ct);
    }

    public async Task DeleteAsync(string roleId, string featureId, CancellationToken ct = default)
    {
        await _client.DeleteItemAsync(_tables.Resolve(TableNames.FeatureRoles), new Dictionary<string, AttributeValue>
        {
            ["RoleId"] = new(roleId),
            ["FeatureId"] = new(featureId)
        }, ct);
    }
}
