using Amazon.DynamoDBv2.Model;

namespace Dhblog.Database;

public record GsiDefinition(string IndexName, string PartitionKey, string? SortKey = null);

public record TableDefinition(
    string BaseName,
    string PartitionKey,
    string? SortKey,
    IReadOnlyList<GsiDefinition> Gsis);

public static class TableDefinitions
{
    public static IReadOnlyList<TableDefinition> All { get; } =
    [
        new(TableNames.Users, "UserId", null,
        [
            new GsiDefinition("UsernameIndex", "Username"),
            new GsiDefinition("EmailIndex", "Email")
        ]),
        new(TableNames.Roles, "RoleId", null, []),
        new(TableNames.Features, "FeatureId", null,
        [
            new GsiDefinition("FeatureCodeIndex", "Code")
        ]),
        new(TableNames.FeatureRoles, "RoleId", "FeatureId", []),
        new(TableNames.BlogEntries, "EntryId", null,
        [
            new GsiDefinition("UserIdCreatedAtIndex", "UserId", "CreatedAt")
        ]),
        new(TableNames.BlogImages, "ImageId", null,
        [
            new GsiDefinition("EntryIdIndex", "EntryId")
        ]),
        new(TableNames.Topics, "TopicId", null,
        [
            new GsiDefinition("NormalizedKeyIndex", "NormalizedKey")
        ]),
        new(TableNames.BlogEntryTopics, "EntryId", "TopicId",
        [
            new GsiDefinition("TopicIdIndex", "TopicId", "EntryId")
        ]),
        new(TableNames.UserFollows, "FollowerId", "FollowingId",
        [
            new GsiDefinition("FollowingIdIndex", "FollowingId", "FollowerId")
        ]),
        new(TableNames.PasswordResetTokens, "TokenId", null,
        [
            new GsiDefinition("TokenIndex", "Token")
        ])
    ];

    public static string ResolveTableName(string baseName, string env) => TableNames.WithEnv(baseName, env);

    public static AttributeDefinition Attr(string name) => new(name, Amazon.DynamoDBv2.ScalarAttributeType.S);

    public static KeySchemaElement Key(string name, Amazon.DynamoDBv2.KeyType type) => new(name, type);
}
