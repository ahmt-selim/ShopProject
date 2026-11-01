using Backend.Data;
using Backend.Dtos.SimilarProducts;
using Backend.Services.SimilarProducts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
public class SimilarProductsController(AppDbContext db, ISimilarProductService service) : ControllerBase
{
    // Herkese açık: manuel + otomatik tamamlanmış liste.
    [HttpGet("api/products/{id:int}/similar")]
    public async Task<ActionResult<object>> GetPublic(int id)
    {
        if (!await db.Products.AnyAsync(p => p.Id == id))
            return NotFound();

        var list = await service.GetSimilarProductsForDisplayAsync(id);
        return Ok(list);
    }

    // Admin: sadece manuel kayıtlı benzer ürünler.
    [Authorize(Roles = "admin")]
    [HttpGet("api/admin/products/{id:int}/similar")]
    public async Task<ActionResult<object>> GetAdmin(int id)
    {
        if (!await db.Products.AnyAsync(p => p.Id == id))
            return NotFound();

        var list = await service.GetManualSimilarForAdminAsync(id);
        return Ok(list);
    }

    // Admin: tüm listeyi tek istekte güncelle.
    [Authorize(Roles = "admin")]
    [HttpPut("api/admin/products/{id:int}/similar")]
    public async Task<IActionResult> UpdateAdmin(int id, [FromBody] UpdateSimilarRequest body)
    {
        try
        {
            await service.UpdateManualSimilarAsync(id, body?.SimilarProductIds ?? new List<int>(), body?.Bidirectional ?? true);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // Admin: benzer ürün seçerken arama.
    [Authorize(Roles = "admin")]
    [HttpGet("api/admin/products/search")]
    public async Task<ActionResult<object>> Search([FromQuery] string? q, [FromQuery] int? excludeProductId)
    {
        var query = db.Products.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Name.Contains(q) || p.ProductCode.Contains(q) || p.Barcode.Contains(q));

        // Kendisi ve (isteğe bağlı) zaten seçilmiş olanlar hariç tutulabilsin:
        // excludeProductId verilirse o ürün hariç. Seçili id'ler query ile de filtrelenebilir.
        if (excludeProductId.HasValue)
            query = query.Where(p => p.Id != excludeProductId.Value);

        var items = await query.OrderBy(p => p.Name).Take(30).ToListAsync();
        var images = await LoadImagesAsync(items.Select(p => p.Id));
        var dto = items.Select(p => new
        {
            p.Id,
            p.Name,
            p.Price,
            p.ListPrice,
            p.Stock,
            image = images.TryGetValue(p.Id, out var u) ? u : null
        });
        return Ok(dto);
    }

    private async Task<Dictionary<int, string>> LoadImagesAsync(IEnumerable<int> ids)
    {
        var set = ids.ToHashSet();
        if (set.Count == 0) return new Dictionary<int, string>();
        var rows = await db.ProductImages.AsNoTracking()
            .Where(i => set.Contains(i.ProductId)).OrderBy(i => i.Id).ToListAsync();
        var map = new Dictionary<int, string>();
        foreach (var g in rows.GroupBy(i => i.ProductId))
            map[g.Key] = g.First().Url;
        return map;
    }
}