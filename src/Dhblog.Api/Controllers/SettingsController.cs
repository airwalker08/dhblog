using Dhblog.Api.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dhblog.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(Policy = "Feature:SETTINGS")]
public class SettingsController : ControllerBase
{
    private readonly IAppSettingsProvider _settings;

    public SettingsController(IAppSettingsProvider settings) => _settings = settings;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _settings.GetAllAsync(ct));

    [HttpPut("{key}")]
    [Authorize(Policy = "Feature:SETTINGS:Write")]
    public async Task<IActionResult> Set(string key, [FromBody] SettingValueRequest request, CancellationToken ct)
    {
        if (!AppSettingsKeys.All.Contains(key))
            return BadRequest(new { message = "Unknown setting key." });
        await _settings.SetAsync(key, request.Value, ct);
        return Ok();
    }
}

public record SettingValueRequest(string Value);
