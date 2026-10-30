using Backend.Auth;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController(AppDbContext db, IConfiguration cfg) : ControllerBase
{
    public record CheckoutItem(int ProductId, int Qty);
    public record CheckoutDto(int AddressId, List<CheckoutItem> Items);
    public record StatusDto(string Status, string? Note);
    public record ShippingDto(string Carrier, string TrackingNumber, DateTime? EstimatedDelivery);
    public record NoteDto(string Note);

    private static readonly string[] AllowedStatuses = OrderStatuses.All;

    private string UserId => ClaimsHelper.UserId(User);
    private bool IsAdmin => User.IsInRole("admin");

    // Izinli gecisler
    private static bool IsAllowedTransition(string from, string to)
    {
        if (from == to) return true;
        return (from, to) switch
        {
            (OrderStatuses.Preparing, OrderStatuses.Shipping) => true,
            (OrderStatuses.Shipping, OrderStatuses.Delivered) => true,
            (OrderStatuses.Preparing, OrderStatuses.Cancelled) => true,
            (OrderStatuses.Shipping, OrderStatuses.Cancelled) => true,
            _ => false
        };
    }

    [HttpPost("checkout")]
    public ActionResult<object> Checkout(CheckoutDto dto)
    {
        if (dto.Items is null || dto.Items.Count == 0)
            return BadRequest(new { error = "Sepet boş." });

        var user = db.Users.Find(UserId);
        if (user is null) return Unauthorized();
        if (!user.EmailConfirmed)
            return StatusCode(403, new { error = "E-posta dogrulanmamis. Lutfen e-postanizi dogrulayin." });
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

        return Ok(ToCustomerDto(order, db.OrderItems.Where(i => i.OrderId == order.Id).ToList(), user.Email, db.OrderStatusHistories.Where(h => h.OrderId == order.Id).ToList()));
    }

    [HttpGet("mine")]
    public ActionResult<object> Mine()
    {
        var orders = db.Orders.Where(o => o.UserId == UserId)
            .OrderByDescending(o => o.Id).ToList();
        return Ok(orders.Select(o => ToCustomerDto(o,
            db.OrderItems.Where(i => i.OrderId == o.Id).ToList(), null,
            db.OrderStatusHistories.Where(h => h.OrderId == o.Id).ToList())));
    }

    [HttpGet("mine/{id:int}")]
    public ActionResult<object> MineById(int id)
    {
        var o = db.Orders.FirstOrDefault(x => x.Id == id && x.UserId == UserId);
        if (o is null) return NotFound();
        return Ok(ToCustomerDto(o, db.OrderItems.Where(i => i.OrderId == o.Id).ToList(), null, db.OrderStatusHistories.Where(h => h.OrderId == o.Id).ToList()));
    }

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
            o.PaymentStatus,
            o.CreatedAt,
            o.ItemsCount,
            o.Subtotal,
            o.Shipping,
            o.Total,
            o.Carrier,
            o.TrackingNumber,
            o.TrackingUrl,
            o.ShippedAt,
            city = o.AddressCity,
            items = items.Select(i => new { i.ProductName, i.Price, i.Qty, i.ImageUrl, i.ProductId })
        });
    }

    [Authorize(Roles = "admin")]
    [HttpGet]
    public ActionResult<object> All([FromQuery] int? page, [FromQuery] int? pageSize)
    {
        var query = db.Orders.OrderByDescending(o => o.Id).AsQueryable();
        var emails = db.Users.ToDictionary(u => u.Id, u => u.Email);
        object ToItem(Order o) => ToAdminDto(o,
            db.OrderItems.Where(i => i.OrderId == o.Id).ToList(),
            emails.TryGetValue(o.UserId, out var e) ? e : o.CustomerEmail,
            db.OrderStatusHistories.Where(h => h.OrderId == o.Id).ToList());
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
    [HttpGet("carriers")]
    public ActionResult<object> Carriers()
    {
        var carriers = cfg.GetSection("Shipping:Carriers").Get<List<CarrierConfig>>() ?? new();
        return Ok(carriers);
    }

    [Authorize(Roles = "admin")]
    [HttpGet("{id:int}")]
    public ActionResult<object> GetById(int id)
    {
        var o = db.Orders.Find(id);
        if (o is null) return NotFound();
        var user = db.Users.Find(o.UserId);
        return Ok(ToAdminDto(o, db.OrderItems.Where(i => i.OrderId == o.Id).ToList(), user?.Email, db.OrderStatusHistories.Where(h => h.OrderId == o.Id).ToList()));
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id:int}/status")]
    public ActionResult<object> SetStatus(int id, StatusDto dto)
    {
        var o = db.Orders.Find(id);
        if (o is null) return NotFound();
        if (!AllowedStatuses.Contains(dto.Status))
            return BadRequest(new { error = "Geçersiz durum." });

        if (!IsAllowedTransition(o.Status, dto.Status))
            return BadRequest(new { error = $"'{o.Status}' → '{dto.Status}' geçişi izinli değil." });

        // Odeme kontrolu
        if ((dto.Status == OrderStatuses.Preparing || dto.Status == OrderStatuses.Shipping) && o.PaymentStatus != PaymentStatuses.Paid)
            return BadRequest(new { error = "Ödemesi tamamlanmamış sipariş hazırlanıyor/kargoya verilemez." });

        if (dto.Status == OrderStatuses.Shipping && string.IsNullOrWhiteSpace(o.TrackingNumber))
            return BadRequest(new { error = "Takip numarası olmadan kargoya verilemez." });

        var fromStatus = o.Status;
        var fromPay = o.PaymentStatus;
        o.Status = dto.Status;
        if (dto.Status == OrderStatuses.Delivered && o.DeliveredAt is null)
            o.DeliveredAt = DateTime.UtcNow;
        if (dto.Status == OrderStatuses.Shipping && o.ShippedAt is null)
            o.ShippedAt = DateTime.UtcNow;

        db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = o.Id,
            FromStatus = fromStatus,
            ToStatus = o.Status,
            FromPaymentStatus = fromPay,
            ToPaymentStatus = o.PaymentStatus,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = UserId,
            Note = dto.Note ?? ""
        });
        db.SaveChanges();

        var user = db.Users.Find(o.UserId);
        return Ok(ToAdminDto(o, db.OrderItems.Where(i => i.OrderId == o.Id).ToList(), user?.Email, db.OrderStatusHistories.Where(h => h.OrderId == o.Id).ToList()));
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id:int}/shipping")]
    public ActionResult<object> SetShipping(int id, ShippingDto dto)
    {
        var o = db.Orders.Find(id);
        if (o is null) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.TrackingNumber))
            return BadRequest(new { error = "Takip numarası zorunlu." });
        if (string.IsNullOrWhiteSpace(dto.Carrier))
            return BadRequest(new { error = "Kargo firması seçiniz." });

        if (o.PaymentStatus != PaymentStatuses.Paid)
            return BadRequest(new { error = "Ödemesi tamamlanmamış sipariş kargoya verilemez." });

        var carrierCfg = (cfg.GetSection("Shipping:Carriers").Get<List<CarrierConfig>>() ?? new())
            .FirstOrDefault(c => c.Name == dto.Carrier);
        var fromStatus = o.Status;
        var fromPay = o.PaymentStatus;

        o.Carrier = dto.Carrier;
        o.TrackingNumber = dto.TrackingNumber.Trim();
        o.TrackingUrl = carrierCfg is not null
            ? carrierCfg.TrackingUrlTemplate.Replace("{tracking}", Uri.EscapeDataString(o.TrackingNumber))
            : "";
        if (dto.EstimatedDelivery.HasValue) o.EstimatedDelivery = dto.EstimatedDelivery;
        if (o.ShippedAt is null) o.ShippedAt = DateTime.UtcNow;
        o.Status = OrderStatuses.Shipping;

        db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = o.Id,
            FromStatus = fromStatus,
            ToStatus = o.Status,
            FromPaymentStatus = fromPay,
            ToPaymentStatus = o.PaymentStatus,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = UserId,
            Note = $"Kargoya verildi: {dto.Carrier} {o.TrackingNumber}"
        });
        db.SaveChanges();

        var user = db.Users.Find(o.UserId);
        return Ok(ToAdminDto(o, db.OrderItems.Where(i => i.OrderId == o.Id).ToList(), user?.Email, db.OrderStatusHistories.Where(h => h.OrderId == o.Id).ToList()));
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id:int}/note")]
    public ActionResult<object> SetNote(int id, NoteDto dto)
    {
        var o = db.Orders.Find(id);
        if (o is null) return NotFound();
        o.InternalNote = dto.Note ?? "";
        db.SaveChanges();
        var user = db.Users.Find(o.UserId);
        return Ok(ToAdminDto(o, db.OrderItems.Where(i => i.OrderId == o.Id).ToList(), user?.Email, db.OrderStatusHistories.Where(h => h.OrderId == o.Id).ToList()));
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id:int}")]
    public ActionResult Delete(int id)
    {
        var o = db.Orders.Find(id);
        if (o is null) return NotFound();
        var items = db.OrderItems.Where(i => i.OrderId == id).ToList();
        foreach (var it in items)
        {
            var p = db.Products.Find(it.ProductId);
            if (p is not null) p.Stock += it.Qty;
        }
        db.OrderStatusHistories.RemoveRange(db.OrderStatusHistories.Where(h => h.OrderId == id));
        db.OrderItems.RemoveRange(items);
        db.Orders.Remove(o);
        db.SaveChanges();
        return NoContent();
    }

    [Authorize(Roles = "admin")]
    [HttpGet("{id:int}/history")]
    public ActionResult<object> History(int id)
    {
        var list = db.OrderStatusHistories.Where(h => h.OrderId == id)
            .OrderByDescending(h => h.ChangedAt).ToList();
        var users = db.Users.ToDictionary(u => u.Id, u => u.Email);
        return Ok(list.Select(h => new
        {
            h.Id,
            h.FromStatus,
            h.ToStatus,
            h.FromPaymentStatus,
            h.ToPaymentStatus,
            h.ChangedAt,
            changedBy = users.TryGetValue(h.ChangedByUserId, out var e) ? e : h.ChangedByUserId,
            h.Note
        }));
    }

    private decimal GetDecimal(string key, decimal fallback) =>
        db.Settings.Where(s => s.Key == key).Select(s => s.Value).AsEnumerable()
            .Select(v => decimal.TryParse(v, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : fallback)
            .FirstOrDefault(fallback);

    private static object ToCustomerDto(Order o, List<OrderItem> items, string? email, List<OrderStatusHistory> history) => new
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
        payment = new
        {
            status = o.PaymentStatus,
            cardFamily = o.CardFamily,
            cardAssociation = o.CardAssociation,
            cardLast4 = o.CardLast4,
            installmentCount = o.InstallmentCount,
            paidAt = o.PaidAt
        },
        shippingInfo = new
        {
            o.Carrier,
            o.TrackingNumber,
            o.TrackingUrl,
            o.ShippedAt,
            o.DeliveredAt,
            o.EstimatedDelivery
        },
        o.CreatedAt,
        items = items.Select(i => new { i.Id, i.ProductId, i.ProductName, i.Price, i.Qty, i.ImageUrl }),
        history = history.OrderBy(h => h.ChangedAt).Select(h => new { h.FromStatus, h.ToStatus, h.ChangedAt }).ToList()
    };

    private static object ToAdminDto(Order o, List<OrderItem> items, string? email, List<OrderStatusHistory> history) => new
    {
        o.Id,
        o.OrderNo,
        o.CustomerName,
        customerEmail = email ?? o.CustomerEmail,
        o.CustomerPhone,
        o.UserId,
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
        payment = new
        {
            status = o.PaymentStatus,
            paymentId = o.PaymentId,
            cardFamily = o.CardFamily,
            cardAssociation = o.CardAssociation,
            cardLast4 = o.CardLast4,
            installmentCount = o.InstallmentCount,
            paidAt = o.PaidAt,
            conversationId = o.ConversationId
        },
        shippingInfo = new
        {
            o.Carrier,
            o.TrackingNumber,
            o.TrackingUrl,
            o.ShippedAt,
            o.DeliveredAt,
            o.EstimatedDelivery
        },
        o.InternalNote,
        o.CreatedAt,
        items = items.Select(i => new { i.Id, i.ProductId, i.ProductName, i.Price, i.Qty, i.ImageUrl }),
        history = history.OrderByDescending(h => h.ChangedAt).Select(h => new { h.FromStatus, h.ToStatus, h.ChangedAt, h.ChangedByUserId, h.Note }).ToList()
    };
}

public class CarrierConfig
{
    public string Name { get; set; } = "";
    public string TrackingUrlTemplate { get; set; } = "";
    // Kargo servisi API bilgileri (admin ayarlarindan/appsettings'ten girilebilir)
    public string ApiBaseUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string ApiSecret { get; set; } = "";
}