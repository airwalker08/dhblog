using Amazon.DynamoDBv2.Model;
using Dhblog.Database.Entities;

namespace Dhblog.DataAccess;

internal static class DynamoMapping
{
    public static User MapUser(Dictionary<string, AttributeValue> item) => new()
    {
        UserId = item["UserId"].S,
        Username = item["Username"].S,
        Email = item["Email"].S,
        PasswordHash = item["PasswordHash"].S,
        FirstName = item.GetValueOrDefault("FirstName")?.S ?? "",
        LastName = item.GetValueOrDefault("LastName")?.S ?? "",
        Locale = item.GetValueOrDefault("Locale")?.S ?? "en-US",
        TimeZone = item.GetValueOrDefault("TimeZone")?.S ?? "UTC",
        Language = item.GetValueOrDefault("Language")?.S ?? "en",
        RoleId = item["RoleId"].S,
        CreatedAt = DateTime.Parse(item.GetValueOrDefault("CreatedAt")?.S ?? DateTime.UtcNow.ToString("O")),
        UpdatedAt = DateTime.Parse(item.GetValueOrDefault("UpdatedAt")?.S ?? DateTime.UtcNow.ToString("O"))
    };

    public static Dictionary<string, AttributeValue> ToItem(User u) => new()
    {
        ["UserId"] = S(u.UserId),
        ["Username"] = S(u.Username),
        ["Email"] = S(u.Email),
        ["PasswordHash"] = S(u.PasswordHash),
        ["FirstName"] = S(u.FirstName),
        ["LastName"] = S(u.LastName),
        ["Locale"] = S(u.Locale),
        ["TimeZone"] = S(u.TimeZone),
        ["Language"] = S(u.Language),
        ["RoleId"] = S(u.RoleId),
        ["CreatedAt"] = S(u.CreatedAt.ToString("O")),
        ["UpdatedAt"] = S(u.UpdatedAt.ToString("O"))
    };

    public static Role MapRole(Dictionary<string, AttributeValue> item) => new()
    {
        RoleId = item["RoleId"].S,
        Name = item["Name"].S,
        Description = item.GetValueOrDefault("Description")?.S ?? ""
    };

    public static Feature MapFeature(Dictionary<string, AttributeValue> item) => new()
    {
        FeatureId = item["FeatureId"].S,
        Code = item["Code"].S,
        Name = item["Name"].S,
        Description = item.GetValueOrDefault("Description")?.S ?? "",
        NavPath = item.GetValueOrDefault("NavPath")?.S ?? "",
        ParentFeatureId = item.GetValueOrDefault("ParentFeatureId")?.S ?? "",
        SortOrder = ParseInt(item.GetValueOrDefault("SortOrder"))
    };

    public static FeatureRole MapFeatureRole(Dictionary<string, AttributeValue> item) => new()
    {
        RoleId = item["RoleId"].S,
        FeatureId = item["FeatureId"].S,
        Permissions = item.GetValueOrDefault("Permissions")?.S ?? ""
    };

    public static BlogEntry MapBlogEntry(Dictionary<string, AttributeValue> item) => new()
    {
        EntryId = item["EntryId"].S,
        UserId = item["UserId"].S,
        Title = item.GetValueOrDefault("Title")?.S ?? "",
        Text = item["Text"].S,
        CreatedAt = DateTime.Parse(item["CreatedAt"].S),
        UpdatedAt = DateTime.Parse(item["UpdatedAt"].S)
    };

    public static Dictionary<string, AttributeValue> ToItem(BlogEntry e) => new()
    {
        ["EntryId"] = S(e.EntryId),
        ["UserId"] = S(e.UserId),
        ["Title"] = S(e.Title),
        ["Text"] = S(e.Text),
        ["CreatedAt"] = S(e.CreatedAt.ToString("O")),
        ["UpdatedAt"] = S(e.UpdatedAt.ToString("O"))
    };

    public static BlogImage MapBlogImage(Dictionary<string, AttributeValue> item) => new()
    {
        ImageId = item["ImageId"].S,
        EntryId = item["EntryId"].S,
        S3Key = item["S3Key"].S,
        ContentType = item.GetValueOrDefault("ContentType")?.S ?? "",
        SizeBytes = long.Parse(item.GetValueOrDefault("SizeBytes")?.N ?? "0"),
        SortOrder = ParseInt(item.GetValueOrDefault("SortOrder")),
        AttachmentType = item.GetValueOrDefault("AttachmentType")?.S ?? "image",
        CreatedAt = DateTime.Parse(item.GetValueOrDefault("CreatedAt")?.S ?? DateTime.UtcNow.ToString("O"))
    };

    public static Dictionary<string, AttributeValue> ToItem(BlogImage i) => new()
    {
        ["ImageId"] = S(i.ImageId),
        ["EntryId"] = S(i.EntryId),
        ["S3Key"] = S(i.S3Key),
        ["ContentType"] = S(i.ContentType),
        ["SizeBytes"] = N(i.SizeBytes),
        ["SortOrder"] = N(i.SortOrder),
        ["AttachmentType"] = S(i.AttachmentType),
        ["CreatedAt"] = S(i.CreatedAt.ToString("O"))
    };

    public static Topic MapTopic(Dictionary<string, AttributeValue> item) => new()
    {
        TopicId = item["TopicId"].S,
        NormalizedKey = item["NormalizedKey"].S,
        DisplayText = item["DisplayText"].S,
        CreatedByUserId = item.GetValueOrDefault("CreatedByUserId")?.S ?? "",
        CreatedAt = DateTime.Parse(item.GetValueOrDefault("CreatedAt")?.S ?? DateTime.UtcNow.ToString("O"))
    };

    public static Dictionary<string, AttributeValue> ToItem(Topic t) => new()
    {
        ["TopicId"] = S(t.TopicId),
        ["NormalizedKey"] = S(t.NormalizedKey),
        ["DisplayText"] = S(t.DisplayText),
        ["CreatedByUserId"] = S(t.CreatedByUserId),
        ["CreatedAt"] = S(t.CreatedAt.ToString("O"))
    };

    public static UserFollow MapUserFollow(Dictionary<string, AttributeValue> item) => new()
    {
        FollowerId = item["FollowerId"].S,
        FollowingId = item["FollowingId"].S,
        CreatedAt = DateTime.Parse(item.GetValueOrDefault("CreatedAt")?.S ?? DateTime.UtcNow.ToString("O"))
    };

    public static PasswordResetToken MapPasswordResetToken(Dictionary<string, AttributeValue> item) => new()
    {
        TokenId = item["TokenId"].S,
        UserId = item["UserId"].S,
        Token = item["Token"].S,
        ExpiresAt = DateTime.Parse(item["ExpiresAt"].S),
        Used = item.GetValueOrDefault("Used")?.BOOL ?? false
    };

    public static Dictionary<string, AttributeValue> ToItem(PasswordResetToken t) => new()
    {
        ["TokenId"] = S(t.TokenId),
        ["UserId"] = S(t.UserId),
        ["Token"] = S(t.Token),
        ["ExpiresAt"] = S(t.ExpiresAt.ToString("O")),
        ["Used"] = new AttributeValue { BOOL = t.Used }
    };

    private static AttributeValue S(string v) => new(v);
    private static AttributeValue N(long v) => new() { N = v.ToString() };
    private static AttributeValue N(int v) => new() { N = v.ToString() };
    private static int ParseInt(AttributeValue? av) => int.Parse(av?.N ?? av?.S ?? "0");
}
