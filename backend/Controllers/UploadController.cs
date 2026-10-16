using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Authorize(Roles = "admin")]
[ApiController]
[Route("api/[controller]")]
public class UploadController(IWebHostEnvironment env) : ControllerBase
{
    private static readonly string[] Allowed = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private static readonly Dictionary<string, string> ContentTypeByExtension = new()
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif",
    };
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
        if (!string.IsNullOrWhiteSpace(file.ContentType) &&
            !file.ContentType.Equals(ContentTypeByExtension[ext], StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Dosya içeriği uzantıyla eşleşmiyor." });

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var bytes = ms.ToArray();
        if (!HasImageSignature(bytes, ext))
            return BadRequest(new { error = "Dosya içeriği resim formatıyla eşleşmiyor." });

        var dir = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
        Directory.CreateDirectory(dir);

        var name = $"{Guid.NewGuid():N}{ext}";
        await System.IO.File.WriteAllBytesAsync(Path.Combine(dir, name), bytes);

        return Ok(new { url = $"/uploads/{name}" });
    }

    // Uzantıya göre dosya imzası (magic bytes). Polyglot dosyalara karşı.
    private static bool HasImageSignature(byte[] content, string extension) => extension switch
    {
        ".jpg" or ".jpeg" => content.Length >= 3 &&
            content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF,
        ".png" => content.Length >= 8 &&
            content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47 &&
            content[4] == 0x0D && content[5] == 0x0A && content[6] == 0x1A && content[7] == 0x0A,
        ".webp" => content.Length >= 12 &&
            content[0] == (byte)'R' && content[1] == (byte)'I' &&
            content[2] == (byte)'F' && content[3] == (byte)'F' &&
            content[8] == (byte)'W' && content[9] == (byte)'E' &&
            content[10] == (byte)'B' && content[11] == (byte)'P',
        ".gif" => content.Length >= 6 &&
            content[0] == (byte)'G' && content[1] == (byte)'I' && content[2] == (byte)'F' &&
            content[3] == (byte)'8' && (content[4] == (byte)'7' || content[4] == (byte)'9') &&
            content[5] == (byte)'a',
        _ => false,
    };
}
