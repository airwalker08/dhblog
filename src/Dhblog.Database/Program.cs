using Dhblog.Database.Deploy;

var command = args.Length > 0 ? args[0].ToLowerInvariant() : "help";
var env = GetArg(args, "--env") ?? Environment.GetEnvironmentVariable("DHBLOG_ENV") ?? "local";
var endpoint = Environment.GetEnvironmentVariable("DYNAMODB_ENDPOINT");
var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";

using var client = DynamoDbClientFactory.Create(endpoint, region);

switch (command)
{
    case "deploy-tables":
        await new DynamoDbTableDeployer(client, env).DeployAllAsync();
        break;
    case "seed":
        await new DataSeeder(client, env).SeedAsync();
        break;
    case "ensure-admin-features":
        await new DataSeeder(client, env).EnsureAdminFeaturesAsync();
        Console.WriteLine("Admin features and administrator permissions updated.");
        break;
    default:
        Console.WriteLine("Usage: dotnet run --project src/Dhblog.Database -- <deploy-tables|seed|ensure-admin-features> [--env local|dev|prod]");
        return command == "help" ? 0 : 1;
}

return 0;

static string? GetArg(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    }
    return null;
}
