using Dhblog.Api.Authorization;
using Dhblog.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dhblog.Api.Controllers;

[ApiController]
[Route("api/feed")]
[Authorize(Policy = "Feature:FEED")]
public class FeedController : ControllerBase
{
    private readonly FeedService _feed;

    public FeedController(FeedService feed) => _feed = feed;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        Ok(await _feed.GetFeedAsync(User.GetUserId()!, ct));
}

[ApiController]
[Route("api/follows")]
[Authorize]
public class FollowsController : ControllerBase
{
    private readonly FollowService _follows;

    public FollowsController(FollowService follows) => _follows = follows;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _follows.GetFollowingUsernamesAsync(User.GetUserId()!, ct));

    [HttpPost("{username}")]
    public async Task<IActionResult> Follow(string username, CancellationToken ct)
    {
        try
        {
            await _follows.FollowAsync(User.GetUserId()!, username, ct);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{username}")]
    public async Task<IActionResult> Unfollow(string username, CancellationToken ct)
    {
        try
        {
            await _follows.UnfollowAsync(User.GetUserId()!, username, ct);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

[ApiController]
[Route("api/topics")]
[Authorize]
public class TopicsController : ControllerBase
{
    private readonly TopicService _topics;

    public TopicsController(TopicService topics) => _topics = topics;

    [HttpGet("suggest")]
    public async Task<IActionResult> Suggest([FromQuery] string q, CancellationToken ct)
    {
        var topics = await _topics.SuggestAsync(q ?? "", ct);
        return Ok(topics.Select(t => new { t.TopicId, t.DisplayText, t.NormalizedKey }));
    }
}
