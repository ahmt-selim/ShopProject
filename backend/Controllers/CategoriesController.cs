using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(AppDbContext db) : ControllerBase
{
    public record CategoryDto(string Name);

    [HttpGet]
    public ActionResult<IEnumerable<object>> GetAll()
    {
        var result = db.Categories.ToList().Select(c => new
        {
            c.Id,
            c.Name,
            c.Slug,
            productCount = db.Products.Count(p => p.CategoryId == c.Id)
        });
        return Ok(result);
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public ActionResult<Category> Create(CategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { error = "Kategori adı zorunlu." });
        var c = new Category { Name = dto.Name.Trim(), Slug = UniqueSlug(dto.Name) };
        db.Categories.Add(c);
        db.SaveChanges();
        return Ok(c);
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id:int}")]
    public ActionResult<Category> Update(int id, CategoryDto dto)
    {
        var c = db.Categories.Find(id);
        if (c is null) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { error = "Kategori adı zorunlu." });
        c.Name = dto.Name.Trim();
        db.SaveChanges();
        return Ok(c);
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var c = db.Categories.Find(id);
        if (c is null) return NotFound();
        if (db.Products.Any(p => p.CategoryId == id))
            return BadRequest(new { error = "Bu kategoride ürünler var, önce ürünleri taşıyın." });
        db.Categories.Remove(c);
        db.SaveChanges();
        return NoContent();
    }

    private string UniqueSlug(string name)
    {
        var slug = ShopDbSeeder.Slugify(name);
        var baseSlug = slug;
        var i = 2;
        while (db.Categories.Any(c => c.Slug == slug))
            slug = $"{baseSlug}-{i++}";
        return slug;
    }
}
