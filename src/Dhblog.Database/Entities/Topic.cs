namespace Dhblog.Database.Entities;

public class Topic
{
    public string TopicId { get; set; } = string.Empty;
    public string NormalizedKey { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
