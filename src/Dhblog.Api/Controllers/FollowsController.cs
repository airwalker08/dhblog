using Dhblog.Api.Authorization;
using Dhblog.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dhblog.Api.Controllers;

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
