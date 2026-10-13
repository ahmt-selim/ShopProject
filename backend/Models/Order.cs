namespace Backend.Models;

public class Order
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string AddressTitle { get; set; } = string.Empty;
    public string AddressFullName { get; set; } = string.Empty;
    public string AddressPhone { get; set; } = string.Empty;
    public string AddressCity { get; set; } = string.Empty;
    public string AddressDistrict { get; set; } = string.Empty;
    public string AddressDetail { get; set; } = string.Empty;
    public int ItemsCount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Shipping { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "Hazırlanıyor";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Qty { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}

public class Setting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
