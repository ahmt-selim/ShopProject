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

            var dir = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
            Directory.CreateDirectory(dir);
            var name = $"{Guid.NewGuid():N}{ext}";
            await using var stream = System.IO.File.Create(Path.Combine(dir, name));
            await file.CopyToAsync(stream);
            url = $"/uploads/{name}";
        }
        else
        {
            var dto = await Request.ReadFromJsonAsync<ImageDto>();
            url = dto?.Url;
        }

        if (string.IsNullOrWhiteSpace(url))
            return BadRequest(new { error = "Resim URL'i zorunlu." });

        var img = new ProductImage { ProductId = productId, Url = url.Trim() };
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
                var path = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"),
                    img.Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            catch { /* dosya silinemezse kayda devam */ }
        }
        return NoContent();
    }
}
