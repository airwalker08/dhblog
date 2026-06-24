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

[ApiController]
[Route("api/admin/roles")]
[Authorize(Policy = "Feature:ADMIN_ROLES")]
public class AdminRolesController : ControllerBase
{
    private readonly AdminService _admin;

    public AdminRolesController(AdminService admin) => _admin = admin;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _admin.ListRolesAsync(ct));

    [HttpGet("{roleId}")]
    public async Task<IActionResult> Get(string roleId, CancellationToken ct)
    {
        var role = await _admin.GetRoleAsync(roleId, ct);
        return role == null ? NotFound() : Ok(role);
    }

    [HttpPost]
    [Authorize(Policy = "Feature:ADMIN_ROLES:Write")]
    public async Task<IActionResult> Create([FromBody] CreateAdminRoleRequest request, CancellationToken ct) =>
        Ok(await _admin.CreateRoleAsync(request, ct));

    [HttpPut("{roleId}")]
    [Authorize(Policy = "Feature:ADMIN_ROLES:Write")]
    public async Task<IActionResult> Update(string roleId, [FromBody] UpdateAdminRoleRequest request, CancellationToken ct)
    {
        var role = await _admin.UpdateRoleAsync(roleId, request, ct);
        return role == null ? NotFound() : Ok(role);
    }

    [HttpDelete("{roleId}")]
    [Authorize(Policy = "Feature:ADMIN_ROLES:Write")]
    public async Task<IActionResult> Delete(string roleId, CancellationToken ct)
    {
        try
        {
            var deleted = await _admin.DeleteRoleAsync(roleId, ct);
            return deleted ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

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
