using Dhblog.Api.Authorization;
using Dhblog.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dhblog.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "Feature:ADMIN_USERS")]
public class AdminUsersController : ControllerBase
{
    private readonly AdminService _admin;

    public AdminUsersController(AdminService admin) => _admin = admin;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _admin.ListUsersAsync(ct));

    [HttpGet("{userId}")]
    public async Task<IActionResult> Get(string userId, CancellationToken ct)
    {
        var user = await _admin.GetUserAsync(userId, ct);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpPost]
    [Authorize(Policy = "Feature:ADMIN_USERS:Write")]
    public async Task<IActionResult> Create([FromBody] CreateAdminUserRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _admin.CreateUserAsync(request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{userId}")]
    [Authorize(Policy = "Feature:ADMIN_USERS:Write")]
    public async Task<IActionResult> Update(string userId, [FromBody] UpdateAdminUserRequest request, CancellationToken ct)
    {
        try
        {
            var user = await _admin.UpdateUserAsync(userId, request, ct);
            return user == null ? NotFound() : Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{userId}")]
    [Authorize(Policy = "Feature:ADMIN_USERS:Write")]
    public async Task<IActionResult> Delete(string userId, CancellationToken ct)
    {
        try
        {
            var deleted = await _admin.DeleteUserAsync(userId, User.GetUserId()!, ct);
            return deleted ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
