using Dhblog.Api.Services;
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

[ApiController]
[Route("api/diagnostics")]
[Authorize(Policy = "Feature:DIAGNOSTICS")]
public class DiagnosticsController : ControllerBase
{
    private readonly DiagnosticsService _diagnostics;

    public DiagnosticsController(DiagnosticsService diagnostics) => _diagnostics = diagnostics;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        Ok(await _diagnostics.GetDiagnosticsAsync(ct));
}

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() => Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
}
