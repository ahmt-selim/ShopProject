using Backend.Auth;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FavoritesController(AppDbContext db) : ControllerBase
{
    public record FavDto(int ProductId);

    private string UserId => ClaimsHelper.UserId(User);

    [HttpGet]
    public ActionResult<object> GetAll()
    {
        var ids = db.Favorites.Where(f => f.UserId == UserId).Select(f => f.ProductId).ToHashSet();
        var products = db.Products.Where(p => ids.Contains(p.Id))
            .ToList().Select(p => new { p.Id, p.Name, p.Price, imageUrl = db.PrimaryImageUrl(p.Id), p.Stock, averageRating = p.AverageRating, reviewCount = p.ReviewCount });
        return Ok(products);
    }

    [HttpPost]
    public IActionResult Add(FavDto dto)
    {
        if (!db.Products.Any(p => p.Id == dto.ProductId)) return NotFound();
        if (!db.Favorites.Any(f => f.UserId == UserId && f.ProductId == dto.ProductId))
        {
            db.Favorites.Add(new Favorite { UserId = UserId, ProductId = dto.ProductId });
            db.SaveChanges();
        }
        return Ok();
    }

    [HttpDelete("{productId:int}")]
    public IActionResult Remove(int productId)
    {
        var f = db.Favorites.FirstOrDefault(x => x.UserId == UserId && x.ProductId == productId);
        if (f is not null)
        {
            db.Favorites.Remove(f);
            db.SaveChanges();
        }
        return NoContent();
    }
}
