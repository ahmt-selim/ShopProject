using Backend.Auth;
using Backend.Data;
using Backend.Models;
using Backend.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(
    AppDbContext db,
    IPaymentService paymentService,
    IOptions<IyzicoOptions> iyzicoOpts,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    private string UserId => ClaimsHelper.UserId(User);
    private static readonly string[] PaymentStatusesAll = PaymentStatuses.All;

    [Authorize]
    [HttpPost("initialize")]
    public async Task<ActionResult<object>> Initialize([FromBody] InitDto dto)
    {
        if (dto.AddressId <= 0) return BadRequest(new { error = "Teslimat adresi seçilmedi." });

        var user = db.Users.Find(UserId);
        if (user is null) return Unauthorized();
        if (!user.EmailConfirmed)
            return StatusCode(403, new { error = "E-posta dogrulanmamis. Lutfen e-postanizi dogrulayin." });

        var addr = db.Addresses.FirstOrDefault(a => a.UserId == UserId && a.Id == dto.AddressId);
        if (addr is null) return BadRequest(new { error = "Teslimat adresi bulunamadı." });

        var cart = db.Carts.FirstOrDefault(c => c.UserId == UserId);
        if (cart is null) return BadRequest(new { error = "Sepet boş." });
        var cartItems = db.CartItems.Where(i => i.CartId == cart.Id).ToList();
        if (cartItems.Count == 0) return BadRequest(new { error = "Sepet boş." });

        var lines = new List<(Product p, int qty)>();
        foreach (var ci in cartItems)
        {
            var p = db.Products.Find(ci.ProductId);
            if (p is null) return BadRequest(new { error = $"Ürün bulunamadı: {ci.ProductId}" });
            if (ci.Qty <= 0) return BadRequest(new { error = "Adet pozitif olmalı." });
            if (p.Stock < ci.Qty) return BadRequest(new { error = $"'{p.Name}' için yeterli stok yok (kalan {p.Stock})." });
            lines.Add((p, ci.Qty));
        }

        var subtotal = lines.Sum(x => x.p.Price * x.qty);
        var limit = GetDecimal("freeShippingLimit", 1500m);
        var shipping = subtotal >= limit ? 0m : GetDecimal("shippingFee", 49.99m);
        var total = subtotal + shipping;

        // Sipariş oluştur (Pending) + stok rezerve
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
            Total = total,
            Status = OrderStatuses.Preparing,
            PaymentStatus = PaymentStatuses.Pending,
            CreatedAt = DateTime.UtcNow,
        };
        db.Orders.Add(order);
        db.SaveChanges();
        order.OrderNo = $"SP-{order.CreatedAt.Year}-{order.Id:0000}";
        order.ConversationId = order.Id.ToString();

        foreach (var (p, qty) in lines)
        {
            p.Stock -= qty; // rezervasyon
            db.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = p.Id,
                ProductName = p.Name,
                Price = p.Price,
                Qty = qty,
                ImageUrl = db.PrimaryImageUrl(p.Id),
            });
        }
        db.SaveChanges();

        // Buyer bilgileri
        var nameParts = (user.Name ?? "").Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts.Length > 0 ? nameParts[0] : (user.Email.Split('@')[0]);
        var lastName = nameParts.Length > 1 ? nameParts[1] : "Müşteri";
        var ip = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        if (ip == "::1") ip = "127.0.0.1";

        var frontendUrl = (HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration)?["Frontend:Url"] ?? "http://localhost:5173";
        var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/payments/callback";

        var req = new PaymentInitializeRequest(
            ConversationId: order.ConversationId,
            BasketId: order.Id.ToString(),
            Price: total,
            PaidPrice: total,
            Buyer: new PaymentBuyer(
                Id: user.Id,
                Name: firstName,
                Surname: lastName,
                Email: user.Email,
                Phone: string.IsNullOrWhiteSpace(addr.Phone) ? (user.Phone ?? "") : addr.Phone,
                IdentityNumber: iyzicoOpts.Value.TestIdentityNumber,
                Ip: ip,
                City: addr.City,
                Country: "Turkey",
                Address: addr.Detail),
            ShippingAddressTitle: addr.Title,
            ShippingAddress: addr.Detail,
            ShippingCity: addr.City,
            ShippingCountry: "Turkey",
            ShippingZip: "00000",
            BillingAddressTitle: addr.Title,
            BillingAddress: addr.Detail,
            BillingCity: addr.City,
            BillingCountry: "Turkey",
            BillingZip: "00000",
            Items: (lines.Select((x, idx) => new PaymentItem(x.p.Id.ToString(), x.p.Name, "Genel", x.p.Price, x.qty))
                .Concat(shipping > 0 ? new[] { new PaymentItem("shipping", "Kargo", "Kargo", shipping, 1) } : Array.Empty<PaymentItem>())
                .ToList()),
            CallbackUrl: callbackUrl);

        var result = await paymentService.InitializeAsync(req);
        if (!result.Success)
        {
            // rezervasyonu geri al
            foreach (var (p, qty) in lines) p.Stock += qty;
            order.PaymentStatus = PaymentStatuses.Failed;
            db.SaveChanges();
            return BadRequest(new { error = result.ErrorMessage ?? "Ödeme başlatılamadı." });
        }

        order.IyzicoToken = result.Token ?? "";
        db.SaveChanges();

        return Ok(new
        {
            orderId = order.Id,
            token = result.Token,
            checkoutFormContent = result.CheckoutFormContent,
            paymentPageUrl = result.PaymentPageUrl,
        });
    }

    [AllowAnonymous]
    [HttpPost("callback")]
    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    public async Task<IActionResult> Callback([FromForm] string? token)
    {
        token = token ?? Request.Form["token"].ToString();
        if (string.IsNullOrWhiteSpace(token))
            return Redirect(FrontendUrl() + "/?payment=error");

        var order = db.Orders.FirstOrDefault(o => o.IyzicoToken == token);
        if (order is null)
            return Redirect(FrontendUrl() + "/?payment=error");

        // Idempotent: zaten Paid ise tekrar işleme
        if (order.PaymentStatus == PaymentStatuses.Paid)
            return Redirect($"{FrontendUrl()}/account/orders/{order.Id}/payment-result?status=paid");

        var result = await paymentService.RetrieveAsync(token);
        if (result.Success)
        {
            // stok zaten rezerve edildi; sadece durum güncelle
            var fromPay = order.PaymentStatus;
            order.PaymentStatus = PaymentStatuses.Paid;
            order.PaymentId = result.PaymentId ?? order.PaymentId;
            order.PaidAt = DateTime.UtcNow;
            order.CardFamily = result.CardFamily ?? order.CardFamily;
            order.CardAssociation = result.CardAssociation ?? order.CardAssociation;
            order.CardLast4 = result.CardLast4 ?? order.CardLast4;
            order.InstallmentCount = result.InstallmentCount > 0 ? result.InstallmentCount : order.InstallmentCount;
            db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = order.Status,
                ToStatus = order.Status,
                FromPaymentStatus = fromPay,
                ToPaymentStatus = order.PaymentStatus,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = order.UserId,
                Note = "Odeme basarili"
            });
            // sepeti temizle
            var cart = db.Carts.FirstOrDefault(c => c.UserId == order.UserId);
            if (cart is not null)
            {
                db.CartItems.RemoveRange(db.CartItems.Where(i => i.CartId == cart.Id));
            }
            db.SaveChanges();
            return Redirect($"{FrontendUrl()}/account/orders/{order.Id}/payment-result?status=paid");
        }
        else
        {
            // başarısız: stok iade
            var items = db.OrderItems.Where(i => i.OrderId == order.Id).ToList();
            foreach (var it in items)
            {
                var p = db.Products.Find(it.ProductId);
                if (p is not null) p.Stock += it.Qty;
            }
            var fromPay2 = order.PaymentStatus;
            order.PaymentStatus = PaymentStatuses.Failed;
            order.PaymentId = result.PaymentId ?? order.PaymentId;
            db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = order.Status,
                ToStatus = order.Status,
                FromPaymentStatus = fromPay2,
                ToPaymentStatus = order.PaymentStatus,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = order.UserId,
                Note = "Odeme basarisiz"
            });
            db.SaveChanges();
            return Redirect($"{FrontendUrl()}/account/orders/{order.Id}/payment-result?status=failed");
        }
    }

    private decimal GetDecimal(string key, decimal fallback) =>
        db.Settings.Where(s => s.Key == key).Select(s => s.Value).AsEnumerable()
            .Select(v => decimal.TryParse(v, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : fallback)
            .FirstOrDefault(fallback);

    private string FrontendUrl() =>
        (HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration)?["Frontend:Url"] ?? "http://localhost:5173";

    public record InitDto(int AddressId);
}