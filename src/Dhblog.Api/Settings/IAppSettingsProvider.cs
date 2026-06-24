namespace Dhblog.Api.Settings;

public interface IAppSettingsProvider
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken ct = default);
    Task SetAsync(string key, string value, CancellationToken ct = default);
    Task RefreshAsync(CancellationToken ct = default);
}

public class AppSettingsKeys
{
    public const string BlogEntryTextLen = "blog_entry_text_len";
    public const string BlogEntryMaxImgCount = "blog_entry_max_img_count";
    public const string BlogEntryMaxImgTypes = "blog_entry_max_img_types";
    public const string BlogEntryMaxImgSize = "blog_entry_max_img_size";
    public const string JwtExpiryMinutes = "jwt_expiry_minutes";
    public const string SiteName = "site_name";
    public const string PasswordResetTokenTtlMinutes = "password_reset_token_ttl_minutes";

    public static readonly string[] All =
    [
        BlogEntryTextLen, BlogEntryMaxImgCount, BlogEntryMaxImgTypes, BlogEntryMaxImgSize,
        JwtExpiryMinutes, SiteName, PasswordResetTokenTtlMinutes
    ];

    public static readonly Dictionary<string, string> Defaults = new()
    {
        [BlogEntryTextLen] = "250",
        [BlogEntryMaxImgCount] = "10",
        [BlogEntryMaxImgTypes] = "png,jpg",
        [BlogEntryMaxImgSize] = "1048576",
        [JwtExpiryMinutes] = "60",
        [SiteName] = "dhblog",
        [PasswordResetTokenTtlMinutes] = "30"
    };
}
