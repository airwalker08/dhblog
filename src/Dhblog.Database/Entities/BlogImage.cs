namespace Dhblog.Database.Entities;

public class BlogImage
{
    public string ImageId { get; set; } = string.Empty;
    public string EntryId { get; set; } = string.Empty;
    public string S3Key { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int SortOrder { get; set; }
    public string AttachmentType { get; set; } = "image";
    public DateTime CreatedAt { get; set; }
}
