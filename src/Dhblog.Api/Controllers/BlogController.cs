using Dhblog.Api.Authorization;
using Dhblog.Api.Services;
using Dhblog.Api.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dhblog.Api.Controllers;

[ApiController]
[Route("api/blog")]
[Authorize]
public class BlogController : ControllerBase
{
    private readonly BlogService _blog;

    public BlogController(BlogService blog) => _blog = blog;

    [HttpGet("{entryId}")]
    [Authorize(Policy = "Feature:BLOG")]
    public async Task<IActionResult> Get(string entryId, CancellationToken ct)
    {
        var entry = await _blog.GetEntryAsync(entryId, ct);
        return entry == null ? NotFound() : Ok(entry);
    }

    [HttpGet("user/{userId}")]
    [Authorize(Policy = "Feature:BLOG")]
    public async Task<IActionResult> GetByUser(
        string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default) =>
        Ok(await _blog.GetUserEntriesPagedAsync(userId, page, pageSize, ct));

    [HttpPost]
    [Authorize(Policy = "Feature:BLOG:Write")]
    public async Task<IActionResult> Create([FromBody] CreateBlogEntryRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        try
        {
            return Ok(await _blog.CreateEntryAsync(userId, request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{entryId}/images/presign")]
    [Authorize(Policy = "Feature:BLOG:Write")]
    public async Task<IActionResult> PresignImage(string entryId, [FromBody] PresignImageRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId()!;
        try
        {
            var result = await _blog.CreateImageUploadAsync(userId, entryId, request.FileName, request.ContentType, request.SizeBytes, ct);
            return result == null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("images/{imageId}/placeholder")]
    [AllowAnonymous]
    public IActionResult ImagePlaceholder(string imageId) =>
        Ok(new { imageId, message = "Local dev placeholder — configure S3 for production uploads." });
}

public record PresignImageRequest(string FileName, string ContentType, long SizeBytes);
