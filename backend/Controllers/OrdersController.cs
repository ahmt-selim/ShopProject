using Backend.Auth;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController(AppDbContext db) : ControllerBase
{
    public record CheckoutItem(int ProductId, int Qty);
    public record CheckoutDto(int AddressId, List<CheckoutItem> Items);
    public record StatusDto(string Status);

    private static readonly string[] AllowedStatuses = OrderStatuses.All;

    private string UserId => ClaimsHelper.UserId(User);

    [HttpPost("checkout")]
    public ActionResult<object> Checkout(CheckoutDto dto)
    {
        if (dto.Items is null || dto.Items.Count == 0)
            return BadRequest(new { error = "Sepet boş." });

        var user = db.Users.Find(UserId);
        if (user is null) return Unauthorized();
        var addr = db.Addresses.FirstOrDefault(a => a.UserId == UserId && a.Id == dto.AddressId);
        if (addr is null)
            return BadRequest(new { error = "Teslimat adresi seçilmedi." });

        var lines = new List<(Product p, int qty)>();
        foreach (var item in dto.Items)
        {
            if (item.Qty <= 0)
                return BadRequest(new { error = "Adet pozitif olmalı." });
            var p = db.Products.Find(item.ProductId);
            if (p is null)
                return BadRequest(new { error = $"Ürün bulunamadı: {item.ProductId}" });
            if (p.Stock < item.Qty)
                return BadRequest(new { error = $"'{p.Name}' için yeterli stok yok (kalan {p.Stock})." });
            lines.Add((p, item.Qty));
        }

        var subtotal = lines.Sum(x => x.p.Price * x.qty);
        var limit = GetDecimal("freeShippingLimit", 1500);
        var shipping = subtotal >= limit ? 0 : GetDecimal("shippingFee", 49.99m);

        var order = new Order
        {
            UserId = UserId,
            CustomerName = string.IsNullOrWhiteSpace(user.Name) ? user.Email : user.Name,
            CustomerEmail = user.Email,
            CustomerPhone = string.IsNullOrWhiteSpace(user.Phone) ? addr.Phone : user.Phone,
            AddressTitle = addr.Title,
            AddressFullName = addr.FullName,
            AddressPhone = addr.Phone,
            AddressCity = addr.City,
            AddressDistrict = addr.District,
            AddressDetail = addr.Detail,
            ItemsCount = lines.Sum(x => x.qty),
            Subtotal = subtotal,
            Shipping = shipping,
            Total = subtotal + shipping,
            Status = OrderStatuses.Preparing,
            CreatedAt = DateTime.UtcNow,
        };
        db.Orders.Add(order);
        db.SaveChanges();

        order.OrderNo = $"SP-{order.CreatedAt.Year}-{order.Id:0000}";
        foreach (var (p, qty) in lines)
        {
            p.Stock -= qty;
            db.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = p.Id,
                ProductName = p.Name,
                Price = p.Price,
                Qty = qty,
                ImageUrl = db.PrimaryImageUrl(p.Id)
            });
        }
        db.SaveChanges();

        return Ok(ToDto(order, db.OrderItems.Where(i => i.OrderId == order.Id).ToList(), user.Email));
    }

    [HttpGet("mine")]
    public ActionResult<object> Mine()
    {
        var orders = db.Orders.Where(o => o.UserId == UserId)
            .OrderByDescending(o => o.Id).ToList();
        return Ok(orders.Select(o => ToDto(o,
            db.OrderItems.Where(i => i.OrderId == o.Id).ToList(), null)));
    }

    [HttpGet("mine/{id:int}")]
    public ActionResult<object> MineById(int id)
    {
        var o = db.Orders.FirstOrDefault(x => x.Id == id && x.UserId == UserId);
        if (o is null) return NotFound();
        return Ok(ToDto(o, db.OrderItems.Where(i => i.OrderId == o.Id).ToList(), null));
    }

    // Üye girişi olmadan sipariş takibi: sipariş no + e-posta eşleşirse kısıtlı bilgi döner.
    [AllowAnonymous]
    [HttpGet("track")]
    [EnableRateLimiting("orders-track")]
    public ActionResult<object> Track([FromQuery] string? orderNo, [FromQuery] string? email)
    {
        orderNo = (orderNo ?? "").Trim();
        email = (email ?? "").Trim();
        if (orderNo.Length == 0 || email.Length == 0)
            return BadRequest(new { error = "Sipariş numarası ve e-posta gerekli." });
        var o = db.Orders.FirstOrDefault(x => x.OrderNo == orderNo);
        if (o is null || !string.Equals((o.CustomerEmail ?? "").Trim(), email,
                StringComparison.OrdinalIgnoreCase))
            return NotFound(new { error = "Sipariş bulunamadı. Bilgileri kontrol edin." });
        var items = db.OrderItems.Where(i => i.OrderId == o.Id).ToList();
        return Ok(new
        {
            o.OrderNo,
            o.Status,
            o.CreatedAt,
            o.ItemsCount,
            o.Subtotal,
            o.Shipping,
            o.Total,
            city = o.AddressCity,
            items = items.Select(i => new { i.ProductName, i.Price, i.Qty, i.ImageUrl })
        });
    }

    [Authorize(Roles = "admin")]
    [HttpGet]
    public ActionResult<object> All([FromQuery] int? page, [FromQuery] int? pageSize)
    {
        var query = db.Orders.OrderByDescending(o => o.Id).AsQueryable();
        var emails = db.Users.ToDictionary(u => u.Id, u => u.Email);
        object ToItem(Order o) => ToDto(o,
            db.OrderItems.Where(i => i.OrderId == o.Id).ToList(),
            emails.TryGetValue(o.UserId, out var e) ? e : o.CustomerEmail);
        if (!page.HasValue)
            return Ok(query.ToList().Select(ToItem));
        var size = Math.Clamp(pageSize ?? 50, 1, 100);
        var p = Math.Max(page.Value, 1);
        var total = query.Count();
        var items = query.Skip((p - 1) * size).Take(size).ToList().Select(ToItem);
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
    [HttpGet("{id:int}")]
    public ActionResult<object> GetById(int id)
    {
        var o = db.Orders.Find(id);
        if (o is null) return NotFound();
        var user = db.Users.Find(o.UserId);
        return Ok(ToDto(o, db.OrderItems.Where(i => i.OrderId == o.Id).ToList(), user?.Email));
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id:int}/status")]
    public ActionResult<object> SetStatus(int id, StatusDto dto)
    {
        var o = db.Orders.Find(id);
        if (o is null) return NotFound();
        if (!AllowedStatuses.Contains(dto.Status))
            return BadRequest(new { error = "Geçersiz durum." });
        o.Status = dto.Status;
        db.SaveChanges();
        return Ok(ToDto(o, db.OrderItems.Where(i => i.OrderId == o.Id).ToList(), null));
    }

    private decimal GetDecimal(string key, decimal fallback) =>
        db.Settings.Where(s => s.Key == key).Select(s => s.Value).AsEnumerable()
            .Select(v => decimal.TryParse(v, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : fallback)
            .FirstOrDefault(fallback);

    private static object ToDto(Order o, List<OrderItem> items, string? email) => new
    {
        o.Id,
        o.OrderNo,
        o.CustomerName,
        customerEmail = email ?? o.CustomerEmail,
        o.CustomerPhone,
        address = new
        {
            o.AddressTitle,
            o.AddressFullName,
            o.AddressPhone,
            o.AddressCity,
            o.AddressDistrict,
            o.AddressDetail
        },
        o.ItemsCount,
        o.Subtotal,
        o.Shipping,
        o.Total,
        o.Status,
        o.CreatedAt,
        items = items.Select(i => new { i.Id, i.ProductId, i.ProductName, i.Price, i.Qty, i.ImageUrl })
    };
}
