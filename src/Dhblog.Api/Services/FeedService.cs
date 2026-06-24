using Amazon.SecurityToken;
using Dhblog.DataAccess;

namespace Dhblog.Api.Services;

public class FeedService
{
    private readonly IUserFollowRepository _follows;
    private readonly IBlogRepository _blog;
    private readonly BlogService _blogService;

    public FeedService(IUserFollowRepository follows, IBlogRepository blog, BlogService blogService)
    {
        _follows = follows;
        _blog = blog;
        _blogService = blogService;
    }

    public async Task<IReadOnlyList<BlogEntryDto>> GetFeedAsync(string userId, CancellationToken ct = default)
    {
        var following = await _follows.GetFollowingIdsAsync(userId, ct);
        if (following.Count == 0) return [];

        var entries = await _blog.GetEntriesByUserIdsAsync(following, ct);
        var result = new List<BlogEntryDto>();
        foreach (var entry in entries.Take(50))
        {
            var dto = await _blogService.GetEntryAsync(entry.EntryId, ct);
            if (dto != null) result.Add(dto);
        }
        return result;
    }
}

public class FollowService
{
    private readonly IUserFollowRepository _follows;
    private readonly IUserRepository _users;

    public FollowService(IUserFollowRepository follows, IUserRepository users)
    {
        _follows = follows;
        _users = users;
    }

    public async Task FollowAsync(string followerId, string followingUsername, CancellationToken ct = default)
    {
        var target = await _users.GetByUsernameAsync(followingUsername, ct)
            ?? throw new KeyNotFoundException("User not found");
        if (target.UserId == followerId)
            throw new InvalidOperationException("Cannot follow yourself.");

        if (!await _follows.IsFollowingAsync(followerId, target.UserId, ct))
        {
            await _follows.FollowAsync(new Database.Entities.UserFollow
            {
                FollowerId = followerId,
                FollowingId = target.UserId,
                CreatedAt = DateTime.UtcNow
            }, ct);
        }
    }

    public async Task UnfollowAsync(string followerId, string followingUsername, CancellationToken ct = default)
    {
        var target = await _users.GetByUsernameAsync(followingUsername, ct)
            ?? throw new KeyNotFoundException("User not found");
        await _follows.UnfollowAsync(followerId, target.UserId, ct);
    }

    public async Task<IReadOnlyList<string>> GetFollowingUsernamesAsync(string followerId, CancellationToken ct = default)
    {
        var ids = await _follows.GetFollowingIdsAsync(followerId, ct);
        var names = new List<string>();
        foreach (var id in ids)
        {
            var user = await _users.GetByIdAsync(id, ct);
            if (user != null) names.Add(user.Username);
        }
        return names;
    }
}

public class TopicService
{
    private readonly ITopicRepository _topics;

    public TopicService(ITopicRepository topics) => _topics = topics;

    public Task<IReadOnlyList<Database.Entities.Topic>> SuggestAsync(string query, CancellationToken ct = default) =>
        _topics.SuggestAsync(query, 10, ct);
}

public class DiagnosticsService
{
    private readonly IAmazonSecurityTokenService? _sts;
    private readonly IConfiguration _config;

    public DiagnosticsService(IConfiguration config, IAmazonSecurityTokenService? sts = null)
    {
        _config = config;
        _sts = sts;
    }

    public async Task<object> GetDiagnosticsAsync(CancellationToken ct = default)
    {
        var env = new
        {
            MachineName = Environment.MachineName,
            OS = Environment.OSVersion.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            DotNetVersion = Environment.Version.ToString(),
            AspNetEnvironment = _config["ASPNETCORE_ENVIRONMENT"] ?? "Unknown"
        };

        object aws;
        if (_sts != null)
        {
            try
            {
                var identity = await _sts.GetCallerIdentityAsync(new Amazon.SecurityToken.Model.GetCallerIdentityRequest(), ct);
                aws = new
                {
                    Account = identity.Account,
                    Arn = identity.Arn,
                    UserId = identity.UserId,
                    Region = _config["AWS_REGION"] ?? "unknown"
                };
            }
            catch (Exception ex)
            {
                aws = new { Error = ex.Message };
            }
        }
        else
        {
            aws = new { Message = "STS not configured (local development)" };
        }

        return new { Environment = env, Aws = aws };
    }
}
