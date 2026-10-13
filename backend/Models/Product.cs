namespace Backend.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal VatRate { get; set; } = 20;
    public int CategoryId { get; set; }
    public int? BrandId { get; set; }
    public int Stock { get; set; }
}
