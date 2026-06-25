using Dhblog.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dhblog.Api.Controllers;

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
