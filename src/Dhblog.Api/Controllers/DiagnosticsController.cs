using Dhblog.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dhblog.Api.Controllers;

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
