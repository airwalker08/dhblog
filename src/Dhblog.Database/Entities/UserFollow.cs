namespace Dhblog.Database.Entities;

public class UserFollow
{
    public string FollowerId { get; set; } = string.Empty;
    public string FollowingId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
