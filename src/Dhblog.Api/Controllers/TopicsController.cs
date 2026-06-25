using Dhblog.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dhblog.Api.Controllers;

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
