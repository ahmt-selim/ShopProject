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
        string? ProductCode, string? Barcode, decimal? VatRate,
        int CategoryId, int? BrandId, int Stock);

    [HttpGet]
    public ActionResult<object> GetAll(
        [FromQuery] int? categoryId, [FromQuery] string? q, [FromQuery] int? brandId,
        [FromQuery] int? page, [FromQuery] int? pageSize)
    {
        var query = db.Products.AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (brandId.HasValue)
            query = query.Where(p => p.BrandId == brandId.Value);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Name.Contains(q) || p.ProductCode.Contains(q) || p.Barcode.Contains(q));

        query = query.OrderByDescending(p => p.Id);

        if (page.HasValue)
        {
            var size = Math.Clamp(pageSize ?? 20, 1, 100);
            var p = Math.Max(page.Value, 1);
            var total = query.Count();
            var items = query.Skip((p - 1) * size).Take(size).ToList();
            return Ok(new
            {
                items = items.Select(x => ToDto(x, CatNames(), BrandNames(), ImagesMap(items.Select(x => x.Id)))),
                total,
                page = p,
                pageSize = size,
                totalPages = (int)Math.Ceiling(total / (double)size)
            });
        }

        var products = query.ToList();
        return Ok(products.Select(p => ToDto(p, CatNames(), BrandNames(), ImagesMap(products.Select(x => x.Id)))));
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
            ProductCode = dto.ProductCode?.Trim() ?? "",
            Barcode = dto.Barcode?.Trim() ?? "",
            VatRate = dto.VatRate ?? 20,
            CategoryId = dto.CategoryId,
            BrandId = dto.BrandId,
            Stock = dto.Stock
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
        p.ProductCode = dto.ProductCode?.Trim() ?? "";
        p.Barcode = dto.Barcode?.Trim() ?? "";
        p.VatRate = dto.VatRate ?? 20;
        p.CategoryId = dto.CategoryId;
        p.BrandId = dto.BrandId;
        p.Stock = dto.Stock;
        db.SaveChanges();
        return Ok(ToDto(p, CatNames(), BrandNames(), ImagesMap([p.Id])));
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var p = db.Products.Find(id);
        if (p is null) return NotFound();
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
        p.ProductCode,
        p.Barcode,
        p.VatRate,
        p.CategoryId,
        categoryName = names.TryGetValue(p.CategoryId, out var n) ? n : null,
        p.BrandId,
        brandName = p.BrandId.HasValue && brands.TryGetValue(p.BrandId.Value, out var b) ? b : null,
        p.Stock,
        averageRating = p.AverageRating,
        reviewCount = p.ReviewCount,
        images = images.TryGetValue(p.Id, out var list) ? list : new List<object>()
    };
}
