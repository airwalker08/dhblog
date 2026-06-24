using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Dhblog.Database;
using Microsoft.Extensions.Options;

namespace Dhblog.DataAccess;

public class DynamoDbOptions
{
    public string Env { get; set; } = "local";
    public string? Endpoint { get; set; }
    public string Region { get; set; } = "us-east-1";
}

public static class DynamoDbClientExtensions
{
    public static IAmazonDynamoDB CreateClient(DynamoDbOptions options)
    {
        var config = new AmazonDynamoDBConfig
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region)
        };

        if (!string.IsNullOrWhiteSpace(options.Endpoint))
        {
            config.ServiceURL = options.Endpoint;
            return new AmazonDynamoDBClient(new BasicAWSCredentials("local", "local"), config);
        }

        return new AmazonDynamoDBClient(config);
    }
}

public class TableNameResolver
{
    private readonly DynamoDbOptions _options;

    public TableNameResolver(IOptions<DynamoDbOptions> options) => _options = options.Value;

    public string Resolve(string baseName) => TableNames.WithEnv(baseName, _options.Env);
}
