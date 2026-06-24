using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

namespace Dhblog.Database.Deploy;

public class DynamoDbClientFactory
{
    public static IAmazonDynamoDB Create(string? endpoint, string region)
    {
        var config = new AmazonDynamoDBConfig
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(region)
        };

        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            config.ServiceURL = endpoint;
            return new AmazonDynamoDBClient(new BasicAWSCredentials("local", "local"), config);
        }

        return new AmazonDynamoDBClient(config);
    }
}

public class DynamoDbTableDeployer
{
    private readonly IAmazonDynamoDB _client;
    private readonly string _env;

    public DynamoDbTableDeployer(IAmazonDynamoDB client, string env)
    {
        _client = client;
        _env = env;
    }

    public async Task DeployAllAsync(CancellationToken ct = default)
    {
        foreach (var def in TableDefinitions.All)
        {
            await EnsureTableAsync(def, ct);
        }
    }

    private async Task EnsureTableAsync(TableDefinition def, CancellationToken ct)
    {
        var tableName = TableDefinitions.ResolveTableName(def.BaseName, _env);
        var existing = await _client.ListTablesAsync(ct);
        if (existing.TableNames.Contains(tableName))
        {
            Console.WriteLine($"Table exists: {tableName}");
            return;
        }

        var request = new CreateTableRequest
        {
            TableName = tableName,
            BillingMode = BillingMode.PAY_PER_REQUEST,
            AttributeDefinitions = BuildAttributes(def),
            KeySchema = BuildKeySchema(def)
        };

        foreach (var gsi in def.Gsis)
        {
            request.GlobalSecondaryIndexes.Add(new GlobalSecondaryIndex
            {
                IndexName = gsi.IndexName,
                KeySchema = BuildGsiKeySchema(gsi),
                Projection = new Projection { ProjectionType = ProjectionType.ALL }
            });
        }

        await _client.CreateTableAsync(request, ct);
        Console.WriteLine($"Created table: {tableName}");

        await WaitForTableAsync(tableName, ct);
    }

    private static List<AttributeDefinition> BuildAttributes(TableDefinition def)
    {
        var names = new HashSet<string> { def.PartitionKey };
        if (def.SortKey != null) names.Add(def.SortKey);
        foreach (var gsi in def.Gsis)
        {
            names.Add(gsi.PartitionKey);
            if (gsi.SortKey != null) names.Add(gsi.SortKey);
        }

        return names.Select(TableDefinitions.Attr).ToList();
    }

    private static List<KeySchemaElement> BuildKeySchema(TableDefinition def)
    {
        var keys = new List<KeySchemaElement>
        {
            TableDefinitions.Key(def.PartitionKey, Amazon.DynamoDBv2.KeyType.HASH)
        };
        if (def.SortKey != null)
            keys.Add(TableDefinitions.Key(def.SortKey, Amazon.DynamoDBv2.KeyType.RANGE));
        return keys;
    }

    private static List<KeySchemaElement> BuildGsiKeySchema(GsiDefinition gsi)
    {
        var keys = new List<KeySchemaElement>
        {
            TableDefinitions.Key(gsi.PartitionKey, Amazon.DynamoDBv2.KeyType.HASH)
        };
        if (gsi.SortKey != null)
            keys.Add(TableDefinitions.Key(gsi.SortKey, Amazon.DynamoDBv2.KeyType.RANGE));
        return keys;
    }

    private async Task WaitForTableAsync(string tableName, CancellationToken ct)
    {
        for (var i = 0; i < 30; i++)
        {
            var desc = await _client.DescribeTableAsync(tableName, ct);
            if (desc.Table.TableStatus == TableStatus.ACTIVE)
                return;
            await Task.Delay(1000, ct);
        }
        throw new InvalidOperationException($"Table {tableName} did not become active in time.");
    }
}
