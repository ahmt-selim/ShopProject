using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    public record UpsertDto(
        string Name, string Description, decimal Price,
        decimal? ListPrice, string? ProductCode, string? Barcode, decimal? VatRate,
        int CategoryId, int? BrandId, int Stock,
        bool? IsCustomizable, int? MaxCustomTextLength);

    [HttpGet]
    public ActionResult<object> GetAll(
        [FromQuery] int? categoryId, [FromQuery] string? q, [FromQuery] int? brandId,
        [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? sort)
    {
        var baseQuery = db.Products.AsQueryable();

        if (categoryId.HasValue)
            baseQuery = baseQuery.Where(p => p.CategoryId == categoryId.Value);

        if (brandId.HasValue)
            baseQuery = baseQuery.Where(p => p.BrandId == brandId.Value);

        var list = baseQuery.ToList();

        if (!string.IsNullOrWhiteSpace(q))
        {
            list = list.Where(p =>
                (p.Name ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (p.ProductCode ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (p.Barcode ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var soldCounts = db.OrderItems
            .GroupBy(oi => oi.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(oi => oi.Qty));

        list = sort switch
        {
            "bestseller" or "mostsold" => list
                .OrderByDescending(p => soldCounts.TryGetValue(p.Id, out var sc) ? sc : 0)
                .ThenByDescending(p => p.Id).ToList(),
            "priceAsc" => list.OrderBy(p => p.Price).ThenBy(p => p.Id).ToList(),
            "priceDesc" => list.OrderByDescending(p => p.Price).ThenByDescending(p => p.Id).ToList(),
            "nameAsc" => list.OrderBy(p => p.Name).ThenBy(p => p.Id).ToList(),
            "nameDesc" => list.OrderByDescending(p => p.Name).ThenByDescending(p => p.Id).ToList(),
            _ => list.OrderByDescending(p => p.Id).ToList()
        };

        if (page.HasValue)
        {
            var size = Math.Clamp(pageSize ?? 20, 1, 100);
            var p = Math.Max(page.Value, 1);
            var total = list.Count;
            var items = list.Skip((p - 1) * size).Take(size).ToList();
            return Ok(new
            {
                items = items.Select(x => ToDto(x, CatNames(), BrandNames(), ImagesMap(items.Select(x => x.Id)))),
                total,
                page = p,
                pageSize = size,
                totalPages = (int)Math.Ceiling(total / (double)size)
            });
        }

        return Ok(list.Select(p => ToDto(p, CatNames(), BrandNames(), ImagesMap(list.Select(x => x.Id)))));
    }

    [HttpGet("{id:int}")]
    public ActionResult<object> GetById(int id)
    {
        var p = db.Products.Find(id);
        if (p is null) return NotFound();
        return Ok(ToDto(p, CatNames(), BrandNames(),
            ImagesMap([id])));
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public ActionResult<object> Create(UpsertDto dto)
    {
        var err = Validate(dto);
        if (err is not null) return BadRequest(new { error = err });

        var p = new Product
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim() ?? "",
            Price = dto.Price,
            ListPrice = dto.ListPrice ?? 0,
            ProductCode = dto.ProductCode?.Trim() ?? "",
            Barcode = dto.Barcode?.Trim() ?? "",
            VatRate = dto.VatRate ?? 20,
            CategoryId = dto.CategoryId,
            BrandId = dto.BrandId,
            Stock = dto.Stock,
            IsCustomizable = dto.IsCustomizable ?? false,
            MaxCustomTextLength = dto.MaxCustomTextLength ?? 0
        };
        db.Products.Add(p);
        db.SaveChanges();
        return CreatedAtAction(nameof(GetById), new { id = p.Id }, ToDto(p, CatNames(), BrandNames(), ImagesMap([p.Id])));
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id:int}")]
    public ActionResult<object> Update(int id, UpsertDto dto)
    {
        var p = db.Products.Find(id);
        if (p is null) return NotFound();
        var err = Validate(dto);
        if (err is not null) return BadRequest(new { error = err });

        p.Name = dto.Name.Trim();
        p.Description = dto.Description?.Trim() ?? "";
        p.Price = dto.Price;
        p.ListPrice = dto.ListPrice ?? 0;
        p.ProductCode = dto.ProductCode?.Trim() ?? "";
        p.Barcode = dto.Barcode?.Trim() ?? "";
        p.VatRate = dto.VatRate ?? 20;
        p.CategoryId = dto.CategoryId;
        p.BrandId = dto.BrandId;
        p.Stock = dto.Stock;
        p.IsCustomizable = dto.IsCustomizable ?? false;
        p.MaxCustomTextLength = dto.MaxCustomTextLength ?? 0;
        db.SaveChanges();
        return Ok(ToDto(p, CatNames(), BrandNames(), ImagesMap([p.Id])));
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var p = db.Products.Find(id);
        if (p is null) return NotFound();
        // Benzer ürün ilişkilerini her iki yönde temizle (hedef taraf Restrict olduğu için
        // manuel silinmeli; aksi halde silme başarısız olur).
        db.SimilarProducts.Where(s => s.ProductId == id || s.SimilarProductId == id).ExecuteDelete();
        db.Products.Remove(p);
        db.ProductImages.Where(i => i.ProductId == id).ExecuteDelete();
        db.Favorites.Where(f => f.ProductId == id).ExecuteDelete();
        db.SaveChanges();
        return NoContent();
    }

    private Dictionary<int, string> CatNames() =>
        db.Categories.ToDictionary(c => c.Id, c => c.Name);

    private Dictionary<int, string> BrandNames() =>
        db.Brands.ToDictionary(b => b.Id, b => b.Name);

    private Dictionary<int, List<object>> ImagesMap(IEnumerable<int> ids)
    {
        var idSet = ids.ToHashSet();
        return db.ProductImages.Where(i => idSet.Contains(i.ProductId))
            .OrderBy(i => i.Id).ToList()
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key,
                g => g.Select(i => (object)new { i.Id, i.Url }).ToList());
    }

    private string? Validate(UpsertDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return "Ürün adı zorunlu.";
        if (dto.Price < 0) return "Fiyat negatif olamaz.";
        if (dto.ListPrice.HasValue && dto.ListPrice.Value < 0) return "Liste fiyatı negatif olamaz.";
        if (dto.ListPrice.HasValue && dto.ListPrice.Value > 0 && dto.ListPrice.Value < dto.Price) return "Liste fiyatı normal fiyattan düşük olamaz.";
        if (dto.Stock < 0) return "Adet negatif olamaz.";
        if (dto.VatRate is < 0 or > 100) return "KDV 0-100 arasında olmalı.";
        if (!db.Categories.Any(c => c.Id == dto.CategoryId)) return "Geçersiz kategori.";
        if (dto.BrandId.HasValue && !db.Brands.Any(b => b.Id == dto.BrandId.Value)) return "Geçersiz marka.";
        return null;
    }

    private static object ToDto(Product p, Dictionary<int, string> names,
        Dictionary<int, string> brands, Dictionary<int, List<object>> images) => new
    {
        p.Id,
        p.Name,
        p.Description,
        p.Price,
        p.ListPrice,
        p.ProductCode,
        p.Barcode,
        p.VatRate,
        p.CategoryId,
        categoryName = names.TryGetValue(p.CategoryId, out var n) ? n : null,
        p.BrandId,
        brandName = p.BrandId.HasValue && brands.TryGetValue(p.BrandId.Value, out var b) ? b : null,
        p.Stock,
        isCustomizable = p.IsCustomizable,
        maxCustomTextLength = p.MaxCustomTextLength,
        averageRating = p.AverageRating,
        reviewCount = p.ReviewCount,
        images = images.TryGetValue(p.Id, out var list) ? list : new List<object>()
    };
}
