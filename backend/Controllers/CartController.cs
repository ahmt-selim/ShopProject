using Backend.Auth;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

// Kullanıcı bazlı kalıcı sepet. Giriş yapan her kullanıcının sepeti DB'de saklanır.
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CartController(AppDbContext db) : ControllerBase
{
    public record ItemDto(int ProductId, int Qty);

    private string UserId => ClaimsHelper.UserId(User);

    private Cart GetOrCreate()
    {
        var cart = db.Carts.FirstOrDefault(c => c.UserId == UserId);
        if (cart is null)
        {
            cart = new Cart { UserId = UserId };
            db.Carts.Add(cart);
            db.SaveChanges();
        }
        return cart;
    }

    [HttpGet]
    public ActionResult<object> Get()
    {
        var cart = GetOrCreate();
        return Ok(ToDto(cart));
    }

    [HttpPost("items")]
    public ActionResult<object> Add(ItemDto dto)
    {
        if (!db.Products.Any(p => p.Id == dto.ProductId)) return NotFound();
        if (dto.Qty <= 0) return BadRequest(new { error = "Adet pozitif olmalı." });
        var cart = GetOrCreate();
        var item = db.CartItems.FirstOrDefault(i => i.CartId == cart.Id && i.ProductId == dto.ProductId);
        if (item is null) db.CartItems.Add(new CartItem { CartId = cart.Id, ProductId = dto.ProductId, Qty = dto.Qty });
        else item.Qty += dto.Qty;
        db.SaveChanges();
        return Ok(ToDto(cart));
    }

    [HttpPut("items/{productId:int}")]
    public ActionResult<object> SetQty(int productId, ItemDto dto)
    {
        var cart = GetOrCreate();
        var item = db.CartItems.FirstOrDefault(i => i.CartId == cart.Id && i.ProductId == productId);
        if (dto.Qty <= 0)
        {
            if (item is not null)
            {
                db.CartItems.Remove(item);
                db.SaveChanges();
            }
            return Ok(ToDto(cart));
        }
        if (!db.Products.Any(p => p.Id == productId)) return NotFound();
        if (item is null) db.CartItems.Add(new CartItem { CartId = cart.Id, ProductId = productId, Qty = dto.Qty });
        else item.Qty = dto.Qty;
        db.SaveChanges();
        return Ok(ToDto(cart));
    }

    [HttpDelete("items/{productId:int}")]
    public ActionResult<object> Remove(int productId)
    {
        var cart = GetOrCreate();
        var item = db.CartItems.FirstOrDefault(i => i.CartId == cart.Id && i.ProductId == productId);
        if (item is not null)
        {
            db.CartItems.Remove(item);
            db.SaveChanges();
        }
        return Ok(ToDto(cart));
    }

    [HttpDelete]
    public ActionResult<object> Clear()
    {
        var cart = GetOrCreate();
        foreach (var item in db.CartItems.Where(i => i.CartId == cart.Id).ToList())
            db.CartItems.Remove(item);
        db.SaveChanges();
        return Ok(ToDto(cart));
    }

    private object ToDto(Cart cart)
    {
        var items = db.CartItems.Where(i => i.CartId == cart.Id).ToList();
        var productIds = items.Select(i => i.ProductId).ToHashSet();
        var products = db.Products.Where(p => productIds.Contains(p.Id))
            .ToDictionary(p => p.Id);
        return new
        {
            cart.Id,
            items = items
                .Where(i => products.ContainsKey(i.ProductId))
                .Select(i => new
                {
                    id = i.Id,
                    productId = i.ProductId,
                    name = products[i.ProductId].Name,
                    price = products[i.ProductId].Price,
                    imageUrl = db.PrimaryImageUrl(i.ProductId),
                    qty = i.Qty
                })
        };
    }
}
