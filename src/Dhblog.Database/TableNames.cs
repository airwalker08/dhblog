namespace Dhblog.Database;

public static class TableNames
{
    public const string Users = "dhblog-users";
    public const string Roles = "dhblog-roles";
    public const string Features = "dhblog-features";
    public const string FeatureRoles = "dhblog-feature-roles";
    public const string BlogEntries = "dhblog-blog-entries";
    public const string BlogImages = "dhblog-blog-images";
    public const string Topics = "dhblog-topics";
    public const string BlogEntryTopics = "dhblog-blog-entry-topics";
    public const string UserFollows = "dhblog-user-follows";
    public const string PasswordResetTokens = "dhblog-password-reset-tokens";

    public static string WithEnv(string baseName, string env) =>
        env is "local" or "dev" or "prod" ? $"{baseName}-{env}" : baseName;
}
