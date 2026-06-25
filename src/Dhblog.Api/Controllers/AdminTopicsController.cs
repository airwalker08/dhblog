using Dhblog.Api.Authorization;
using Dhblog.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dhblog.Api.Controllers;

[ApiController]
[Route("api/admin/topics")]
[Authorize(Policy = "Feature:ADMIN_TOPICS")]
public class AdminTopicsController : ControllerBase
{
    private readonly AdminService _admin;

    public AdminTopicsController(AdminService admin) => _admin = admin;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _admin.ListTopicsAsync(ct));

    [HttpGet("{topicId}")]
    public async Task<IActionResult> Get(string topicId, CancellationToken ct)
    {
        var topic = await _admin.GetTopicAsync(topicId, ct);
        return topic == null ? NotFound() : Ok(topic);
    }

    [HttpPost]
    [Authorize(Policy = "Feature:ADMIN_TOPICS:Write")]
    public async Task<IActionResult> Create([FromBody] CreateAdminTopicRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _admin.CreateTopicAsync(User.GetUserId()!, request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{topicId}")]
    [Authorize(Policy = "Feature:ADMIN_TOPICS:Write")]
    public async Task<IActionResult> Update(string topicId, [FromBody] UpdateAdminTopicRequest request, CancellationToken ct)
    {
        try
        {
            var topic = await _admin.UpdateTopicAsync(topicId, request, ct);
            return topic == null ? NotFound() : Ok(topic);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{topicId}")]
    [Authorize(Policy = "Feature:ADMIN_TOPICS:Write")]
    public async Task<IActionResult> Delete(string topicId, CancellationToken ct)
    {
        var deleted = await _admin.DeleteTopicAsync(topicId, ct);
        return deleted ? NoContent() : NotFound();
    }
}
