using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/products/{productId:int}/images")]
public class ProductImagesController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    public record ImageDto(string Url);

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

    [HttpGet]
    public ActionResult<IEnumerable<object>> GetAll(int productId)
    {
        if (!db.Products.Any(p => p.Id == productId)) return NotFound();
        return Ok(db.ProductImages.Where(i => i.ProductId == productId)
            .OrderBy(i => i.Id).ToList()
            .Select(i => new { i.Id, i.ProductId, i.Url }));
    }

    // JSON { url } ya da multipart form file kabul eder
    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<ActionResult<object>> Add(int productId)
    {
        if (!db.Products.Any(p => p.Id == productId)) return NotFound();

        string? url = null;
        if (Request.HasFormContentType)
        {
            var file = Request.Form.Files.FirstOrDefault();
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
            url = $"/uploads/{name}";
        }
        else
        {
            var dto = await Request.ReadFromJsonAsync<ImageDto>();
            url = dto?.Url;
        }

        if (string.IsNullOrWhiteSpace(url))
            return BadRequest(new { error = "Resim URL'i zorunlu." });

        url = url.Trim();
        if (!IsAllowedImageUrl(url))
            return BadRequest(new { error = "Resim URL'i geçersiz." });

        var img = new ProductImage { ProductId = productId, Url = url };
        db.ProductImages.Add(img);
        db.SaveChanges();
        return Ok(new { img.Id, img.ProductId, img.Url });
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("/api/product-images/{id:int}")]
    public IActionResult Delete(int id)
    {
        var img = db.ProductImages.Find(id);
        if (img is null) return NotFound();
        db.ProductImages.Remove(img);
        db.SaveChanges();

        if (img.Url.StartsWith("/uploads/"))
        {
            try
            {
                var relative = img.Url.TrimStart('/');
                // '..' ile dizin dışına kaçışı engelle.
                if (!relative.Contains(".."))
                {
                    var path = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"),
                        relative.Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
            }
            catch { /* dosya silinemezse kayda devam */ }
        }
        return NoContent();
    }

    // Harici URL yalnızca http(s), yerel path yalnızca /uploads/ altında olmalı.
    // javascript:/data:/vbscript: gibi şemalar reddedilir (stored-XSS zemini).
    private static bool IsAllowedImageUrl(string url)
    {
        if (url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return !url.Contains("..");
        return url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
    }

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
