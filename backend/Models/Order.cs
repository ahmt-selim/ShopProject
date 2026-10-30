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
    public string Status { get; set; } = OrderStatuses.Preparing;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Ödeme durumu (iyzico)
    public string PaymentStatus { get; set; } = PaymentStatuses.Pending;
    public string IyzicoToken { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public string CardFamily { get; set; } = string.Empty;
    public string CardAssociation { get; set; } = string.Empty;
    public string CardLast4 { get; set; } = string.Empty;
    public int InstallmentCount { get; set; } = 1;

    // Kargo bilgisi
    public string Carrier { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public string TrackingUrl { get; set; } = string.Empty;
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? EstimatedDelivery { get; set; }

    // Ic not (yalnizca admin)
    public string InternalNote { get; set; } = string.Empty;
}

public class OrderStatusHistory
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string FromPaymentStatus { get; set; } = string.Empty;
    public string ToPaymentStatus { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string ChangedByUserId { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
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
