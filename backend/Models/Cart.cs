namespace Backend.Models;

public class Cart
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
}

public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public int ProductId { get; set; }
    public int Qty { get; set; }
}
