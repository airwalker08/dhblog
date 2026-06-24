using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Dhblog.Api.Settings;
using Dhblog.Database;
using Dhblog.Database.Entities;
using Dhblog.DataAccess;

namespace Dhblog.Api.Services;

public class BlogService
{
    private const int MaxTitleLength = 200;

    private readonly IBlogRepository _blog;
    private readonly ITopicRepository _topics;
    private readonly IUserRepository _users;
    private readonly IAppSettingsProvider _settings;
    private readonly IAmazonS3? _s3;
    private readonly string? _bucket;
    private readonly string? _cdnDomain;

    public BlogService(
        IBlogRepository blog,
        ITopicRepository topics,
        IUserRepository users,
        IAppSettingsProvider settings,
        IConfiguration configuration,
        IAmazonS3? s3 = null)
    {
        _blog = blog;
        _topics = topics;
        _users = users;
        _settings = settings;
        _s3 = s3;
        _bucket = configuration["MEDIA_BUCKET_NAME"];
        _cdnDomain = configuration["CLOUDFRONT_MEDIA_DOMAIN"];
    }

    public async Task<BlogEntryDto?> GetEntryAsync(string entryId, CancellationToken ct = default)
    {
        var entry = await _blog.GetEntryByIdAsync(entryId, ct);
        if (entry == null) return null;
        return await ToDtoAsync(entry, ct);
    }

    public async Task<IReadOnlyList<BlogEntryDto>> GetUserEntriesAsync(string userId, CancellationToken ct = default)
    {
        var entries = await _blog.GetEntriesByUserIdAsync(userId, ct);
        var result = new List<BlogEntryDto>();
        foreach (var e in entries)
            result.Add(await ToDtoAsync(e, ct));
        return result;
    }

    public async Task<PagedBlogEntriesResponse> GetUserEntriesPagedAsync(
        string userId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var all = await GetUserEntriesAsync(userId, ct);
        var totalCount = all.Count;
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedBlogEntriesResponse(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<BlogEntryDto> CreateEntryAsync(string userId, CreateBlogEntryRequest request, CancellationToken ct = default)
    {
        var title = request.Title.Trim();
        if (string.IsNullOrEmpty(title))
            throw new InvalidOperationException("Blog title is required.");
        if (title.Length > MaxTitleLength)
            throw new InvalidOperationException($"Blog title exceeds maximum length of {MaxTitleLength}.");

        var maxLen = int.Parse(await _settings.GetAsync(AppSettingsKeys.BlogEntryTextLen, ct) ?? "250");
        if (request.Text.Length > maxLen)
            throw new InvalidOperationException($"Blog text exceeds maximum length of {maxLen}.");

        var entry = new BlogEntry
        {
            EntryId = Guid.NewGuid().ToString(),
            UserId = userId,
            Title = title,
            Text = request.Text,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _blog.CreateEntryAsync(entry, ct);

        foreach (var topicText in request.Topics.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var topic = await GetOrCreateTopicAsync(userId, topicText, ct);
            await _topics.LinkEntryTopicAsync(entry.EntryId, topic.TopicId, ct);
        }

        return await ToDtoAsync(entry, ct);
    }

    public async Task<PresignedUploadDto?> CreateImageUploadAsync(string userId, string entryId, string fileName, string contentType, long sizeBytes, CancellationToken ct = default)
    {
        var entry = await _blog.GetEntryByIdAsync(entryId, ct);
        if (entry == null || entry.UserId != userId) return null;

        var maxCount = int.Parse(await _settings.GetAsync(AppSettingsKeys.BlogEntryMaxImgCount, ct) ?? "10");
        var existing = await _blog.GetImagesByEntryIdAsync(entryId, ct);
        if (existing.Count >= maxCount)
            throw new InvalidOperationException($"Maximum image count of {maxCount} reached.");

        var maxSize = long.Parse(await _settings.GetAsync(AppSettingsKeys.BlogEntryMaxImgSize, ct) ?? "1048576");
        if (sizeBytes > maxSize)
            throw new InvalidOperationException($"Image exceeds maximum size of {maxSize} bytes.");

        var allowed = (await _settings.GetAsync(AppSettingsKeys.BlogEntryMaxImgTypes, ct) ?? "png,jpg")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant()).ToHashSet();
        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        if (!allowed.Contains(ext))
            throw new InvalidOperationException($"Image type .{ext} is not allowed.");

        var imageId = Guid.NewGuid().ToString();
        var s3Key = $"posts/{entryId}/{imageId}.{ext}";

        if (_s3 == null || string.IsNullOrEmpty(_bucket))
        {
            await _blog.CreateImageAsync(new BlogImage
            {
                ImageId = imageId,
                EntryId = entryId,
                S3Key = s3Key,
                ContentType = contentType,
                SizeBytes = sizeBytes,
                SortOrder = existing.Count,
                CreatedAt = DateTime.UtcNow
            }, ct);
            return new PresignedUploadDto(imageId, s3Key, $"http://localhost:8080/api/blog/images/{imageId}/placeholder", DateTime.UtcNow.AddHours(1));
        }

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = s3Key,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = DateTime.UtcNow.AddMinutes(15)
        };
        var url = _s3.GetPreSignedURL(request);

        await _blog.CreateImageAsync(new BlogImage
        {
            ImageId = imageId,
            EntryId = entryId,
            S3Key = s3Key,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            SortOrder = existing.Count,
            CreatedAt = DateTime.UtcNow
        }, ct);

        return new PresignedUploadDto(imageId, s3Key, url, request.Expires);
    }

    private async Task<Topic> GetOrCreateTopicAsync(string userId, string displayText, CancellationToken ct)
    {
        var normalized = TopicNormalizer.Normalize(displayText);
        var existing = await _topics.GetByNormalizedKeyAsync(normalized, ct);
        if (existing != null) return existing;

        var topic = new Topic
        {
            TopicId = Guid.NewGuid().ToString(),
            NormalizedKey = normalized,
            DisplayText = displayText.Trim(),
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        await _topics.CreateAsync(topic, ct);
        return topic;
    }

    private async Task<BlogEntryDto> ToDtoAsync(BlogEntry entry, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(entry.UserId, ct);
        var topicList = await _topics.GetByEntryIdAsync(entry.EntryId, ct);
        var images = await _blog.GetImagesByEntryIdAsync(entry.EntryId, ct);
        var imageDtos = images.Select(i => new BlogImageDto(
            i.ImageId,
            string.IsNullOrEmpty(_cdnDomain) ? $"/api/blog/images/{i.ImageId}" : $"https://{_cdnDomain}/{i.S3Key}",
            i.ContentType)).ToList();

        return new BlogEntryDto(
            entry.EntryId, entry.UserId, user?.Username ?? entry.UserId,
            entry.Title, entry.Text, entry.CreatedAt, entry.UpdatedAt,
            topicList.Select(t => t.DisplayText).ToList(), imageDtos);
    }
}

public record CreateBlogEntryRequest(string Title, string Text, IReadOnlyList<string> Topics);
public record BlogEntryDto(
    string EntryId, string UserId, string Username, string Title, string Text,
    DateTime CreatedAt, DateTime UpdatedAt,
    IReadOnlyList<string> Topics, IReadOnlyList<BlogImageDto> Images);
public record PagedBlogEntriesResponse(
    IReadOnlyList<BlogEntryDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
public record BlogImageDto(string ImageId, string Url, string ContentType);
public record PresignedUploadDto(string ImageId, string S3Key, string UploadUrl, DateTime ExpiresAt);
