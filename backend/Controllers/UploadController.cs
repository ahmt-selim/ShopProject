using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Authorize(Roles = "admin")]
[ApiController]
[Route("api/[controller]")]
public class UploadController(IWebHostEnvironment env) : ControllerBase
{
    private static readonly string[] Allowed = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private const long MaxBytes = 5 * 1024 * 1024;

    [HttpPost]
    [RequestSizeLimit(MaxBytes)]
    public async Task<ActionResult<object>> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Dosya seçilmedi." });
        if (file.Length > MaxBytes)
            return BadRequest(new { error = "Dosya en fazla 5MB olmalı." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Allowed.Contains(ext))
            return BadRequest(new { error = "Sadece jpg, png, webp, gif yüklenebilir." });

        var dir = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
        Directory.CreateDirectory(dir);

        var name = $"{Guid.NewGuid():N}{ext}";
        await using var stream = System.IO.File.Create(Path.Combine(dir, name));
        await file.CopyToAsync(stream);

        return Ok(new { url = $"/uploads/{name}" });
    }
}
