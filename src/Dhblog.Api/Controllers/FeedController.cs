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
