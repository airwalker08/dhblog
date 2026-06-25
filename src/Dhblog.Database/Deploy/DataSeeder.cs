using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Dhblog.Database.Entities;
using Microsoft.AspNetCore.Identity;

namespace Dhblog.Database.Deploy;

public class DataSeeder
{
    private readonly IAmazonDynamoDB _client;
    private readonly string _env;
    private readonly PasswordHasher<User> _hasher = new();

    public DataSeeder(IAmazonDynamoDB client, string env)
    {
        _client = client;
        _env = env;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedRolesAsync(ct);
        await SeedFeaturesAsync(ct);
        await SeedFeatureRolesAsync(ct);
        await SeedAdminUserAsync(ct);
        await EnsureAdminFeaturesAsync(ct);
        Console.WriteLine("Seed completed.");
    }

    /// <summary>Upserts feature nav metadata and administrator permissions (safe to run on every dev startup).</summary>
    public async Task EnsureAdminFeaturesAsync(CancellationToken ct = default)
    {
        foreach (var f in NavFeatures())
        {
            var item = new Dictionary<string, AttributeValue>
            {
                ["FeatureId"] = S(f.FeatureId),
                ["Code"] = S(f.Code),
                ["Name"] = S(f.Name),
                ["Description"] = S(f.Description),
                ["NavPath"] = S(f.NavPath),
                ["SortOrder"] = N(f.SortOrder)
            };
            if (!string.IsNullOrEmpty(f.ParentFeatureId))
                item["ParentFeatureId"] = S(f.ParentFeatureId);

            await _client.PutItemAsync(new PutItemRequest
            {
                TableName = T(TableNames.Features),
                Item = item
            }, ct);
        }

        var adminOnlyFeatureIds = NavFeatures()
            .Where(f => f.Code is "ADMIN" or "ADMIN_USERS" or "ADMIN_ROLES" or "ADMIN_TOPICS" or "SETTINGS" or "DIAGNOSTICS")
            .Select(f => f.FeatureId);

        foreach (var featureId in adminOnlyFeatureIds)
        {
            await _client.PutItemAsync(new PutItemRequest
            {
                TableName = T(TableNames.FeatureRoles),
                Item = new Dictionary<string, AttributeValue>
                {
                    ["RoleId"] = S(SeedIds.RoleAdministrator),
                    ["FeatureId"] = S(featureId),
                    ["Permissions"] = S("R,W")
                }
            }, ct);
        }
    }

    private static IEnumerable<Feature> NavFeatures() =>
    [
        new Feature { FeatureId = SeedIds.FeatureFeed, Code = "FEED", Name = "Feed", Description = "Followed users feed", NavPath = "/feed", SortOrder = 10 },
        new Feature { FeatureId = SeedIds.FeatureBlog, Code = "BLOG", Name = "Blog", Description = "Blog entries", NavPath = "/blog", SortOrder = 20 },
        new Feature { FeatureId = SeedIds.FeatureProfile, Code = "PROFILE", Name = "Profile", Description = "User profile", NavPath = "", SortOrder = 0 },
        new Feature { FeatureId = SeedIds.FeatureAdmin, Code = "ADMIN", Name = "Admin", Description = "Administration", NavPath = "/admin", SortOrder = 30 },
        new Feature { FeatureId = SeedIds.FeatureAdminUsers, Code = "ADMIN_USERS", Name = "Users", Description = "Manage users", NavPath = "/admin/users", ParentFeatureId = SeedIds.FeatureAdmin, SortOrder = 31 },
        new Feature { FeatureId = SeedIds.FeatureAdminRoles, Code = "ADMIN_ROLES", Name = "Roles", Description = "Manage roles", NavPath = "/admin/roles", ParentFeatureId = SeedIds.FeatureAdmin, SortOrder = 32 },
        new Feature { FeatureId = SeedIds.FeatureAdminTopics, Code = "ADMIN_TOPICS", Name = "Topics", Description = "Manage topics", NavPath = "/admin/topics", ParentFeatureId = SeedIds.FeatureAdmin, SortOrder = 33 },
        new Feature { FeatureId = SeedIds.FeatureSettings, Code = "SETTINGS", Name = "Settings", Description = "System settings", NavPath = "/settings", ParentFeatureId = SeedIds.FeatureAdmin, SortOrder = 34 },
        new Feature { FeatureId = SeedIds.FeatureDiagnostics, Code = "DIAGNOSTICS", Name = "Diagnostics", Description = "System diagnostics", NavPath = "/diagnostics", ParentFeatureId = SeedIds.FeatureAdmin, SortOrder = 35 }
    ];

    private string T(string baseName) => TableDefinitions.ResolveTableName(baseName, _env);

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        var roles = new[]
        {
            new Role { RoleId = SeedIds.RoleAdministrator, Name = "Administrator", Description = "Full access to all features" },
            new Role { RoleId = SeedIds.RoleStandardUser, Name = "Standard User", Description = "Read/write access to most non-admin features" },
            new Role { RoleId = SeedIds.RoleReadOnly, Name = "Read-only user", Description = "Read-only access to most non-admin features" }
        };

        foreach (var role in roles)
        {
            await PutIfNotExistsAsync(T(TableNames.Roles), new Dictionary<string, AttributeValue>
            {
                ["RoleId"] = S(role.RoleId),
                ["Name"] = S(role.Name),
                ["Description"] = S(role.Description)
            }, "RoleId", role.RoleId, ct);
        }
    }

    private async Task SeedFeaturesAsync(CancellationToken ct)
    {
        foreach (var f in NavFeatures())
        {
            var item = new Dictionary<string, AttributeValue>
            {
                ["FeatureId"] = S(f.FeatureId),
                ["Code"] = S(f.Code),
                ["Name"] = S(f.Name),
                ["Description"] = S(f.Description),
                ["NavPath"] = S(f.NavPath),
                ["SortOrder"] = N(f.SortOrder)
            };
            if (!string.IsNullOrEmpty(f.ParentFeatureId))
                item["ParentFeatureId"] = S(f.ParentFeatureId);

            await PutIfNotExistsAsync(T(TableNames.Features), item, "FeatureId", f.FeatureId, ct);
        }
    }

    private async Task SeedFeatureRolesAsync(CancellationToken ct)
    {
        var allFeatures = new[]
        {
            SeedIds.FeatureSettings, SeedIds.FeatureDiagnostics, SeedIds.FeatureBlog,
            SeedIds.FeatureFeed, SeedIds.FeatureProfile,
            SeedIds.FeatureAdmin, SeedIds.FeatureAdminUsers, SeedIds.FeatureAdminRoles, SeedIds.FeatureAdminTopics
        };
        var userFeatures = new[]
        {
            SeedIds.FeatureBlog, SeedIds.FeatureFeed, SeedIds.FeatureProfile
        };

        foreach (var featureId in allFeatures)
        {
            await PutFeatureRoleIfNotExistsAsync(SeedIds.RoleAdministrator, featureId, "R,W", ct);
        }

        foreach (var featureId in userFeatures)
        {
            await PutFeatureRoleIfNotExistsAsync(SeedIds.RoleStandardUser, featureId, "R,W", ct);
            await PutFeatureRoleIfNotExistsAsync(SeedIds.RoleReadOnly, featureId, "R", ct);
        }
    }

    private async Task PutFeatureRoleIfNotExistsAsync(string roleId, string featureId, string permissions, CancellationToken ct)
    {
        await PutIfNotExistsAsync(T(TableNames.FeatureRoles), new Dictionary<string, AttributeValue>
        {
            ["RoleId"] = S(roleId),
            ["FeatureId"] = S(featureId),
            ["Permissions"] = S(permissions)
        }, "RoleId", roleId, ct, "FeatureId", featureId);
    }

    private async Task SeedAdminUserAsync(CancellationToken ct)
    {
        var user = new User
        {
            UserId = SeedIds.AdminUserId,
            Username = "Coulson",
            Email = "coulson@dhblog.local",
            FirstName = "Phil",
            LastName = "Coulson",
            Locale = "en-US",
            TimeZone = "America/New_York",
            Language = "en",
            RoleId = SeedIds.RoleAdministrator,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _hasher.HashPassword(user, "SecretPwd(42)");

        await PutIfNotExistsAsync(T(TableNames.Users), new Dictionary<string, AttributeValue>
        {
            ["UserId"] = S(user.UserId),
            ["Username"] = S(user.Username),
            ["Email"] = S(user.Email),
            ["PasswordHash"] = S(user.PasswordHash),
            ["FirstName"] = S(user.FirstName),
            ["LastName"] = S(user.LastName),
            ["Locale"] = S(user.Locale),
            ["TimeZone"] = S(user.TimeZone),
            ["Language"] = S(user.Language),
            ["RoleId"] = S(user.RoleId),
            ["CreatedAt"] = S(user.CreatedAt.ToString("O")),
            ["UpdatedAt"] = S(user.UpdatedAt.ToString("O"))
        }, "UserId", user.UserId, ct);
    }

    private async Task PutIfNotExistsAsync(
        string tableName,
        Dictionary<string, AttributeValue> item,
        string pkName,
        string pkValue,
        CancellationToken ct,
        string? skName = null,
        string? skValue = null)
    {
        try
        {
            var request = new PutItemRequest
            {
                TableName = tableName,
                Item = item,
                ConditionExpression = $"attribute_not_exists({pkName})"
            };
            if (skName != null)
                request.ConditionExpression += $" AND attribute_not_exists({skName})";

            await _client.PutItemAsync(request, ct);
            Console.WriteLine($"Seeded: {tableName} {pkValue}");
        }
        catch (ConditionalCheckFailedException)
        {
            Console.WriteLine($"Already exists: {tableName} {pkValue}");
        }
    }

    private static AttributeValue S(string v) => new(v);
    private static AttributeValue N(int v) => new() { N = v.ToString() };
}
