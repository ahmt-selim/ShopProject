using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrandsController(AppDbContext db) : ControllerBase
{
    public record BrandDto(string Name);

    [HttpGet]
    public ActionResult<object> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
    {
        var query = db.Brands.OrderBy(b => b.Id).AsQueryable();
        if (!page.HasValue)
        {
            var all = query.ToList().Select(b => new
            {
                b.Id,
                b.Name,
                b.Slug,
                productCount = db.Products.Count(p => p.BrandId == b.Id)
            });
            return Ok(all);
        }
        var size = Math.Clamp(pageSize ?? 50, 1, 100);
        var p = Math.Max(page.Value, 1);
        var total = query.Count();
        var items = query.Skip((p - 1) * size).Take(size).ToList().Select(b => new
        {
            b.Id,
            b.Name,
            b.Slug,
            productCount = db.Products.Count(p => p.BrandId == b.Id)
        });
        return Ok(new
        {
            items,
            total,
            page = p,
            pageSize = size,
            totalPages = (int)Math.Ceiling(total / (double)size)
        });
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public ActionResult<Brand> Create(BrandDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { error = "Marka adı zorunlu." });
        var b = new Brand { Name = dto.Name.Trim(), Slug = UniqueSlug(dto.Name) };
        db.Brands.Add(b);
        db.SaveChanges();
        return Ok(b);
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id:int}")]
    public ActionResult<Brand> Update(int id, BrandDto dto)
    {
        var b = db.Brands.Find(id);
        if (b is null) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { error = "Marka adı zorunlu." });
        b.Name = dto.Name.Trim();
        db.SaveChanges();
        return Ok(b);
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var b = db.Brands.Find(id);
        if (b is null) return NotFound();
        db.Brands.Remove(b);
        foreach (var p in db.Products.Where(p => p.BrandId == id))
            p.BrandId = null;
        db.SaveChanges();
        return NoContent();
    }

    private string UniqueSlug(string name)
    {
        var slug = ShopDbSeeder.Slugify(name);
        var baseSlug = slug;
        var i = 2;
        while (db.Brands.Any(b => b.Slug == slug))
            slug = $"{baseSlug}-{i++}";
        return slug;
    }
}
