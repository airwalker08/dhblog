namespace Dhblog.Database.Entities;

public class PasswordResetToken
{
    public string TokenId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool Used { get; set; }
}
