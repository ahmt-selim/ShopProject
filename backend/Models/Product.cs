namespace Backend.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal ListPrice { get; set; }    public string ProductCode { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal VatRate { get; set; } = 20;
    public int CategoryId { get; set; }
    public int? BrandId { get; set; }
    public int Stock { get; set; }
    public bool IsCustomizable { get; set; }
    public int MaxCustomTextLength { get; set; }
    // Denormalize yorum özeti (yorum yaz/güncelle/sil ile aynı transaction'da güncellenir).
    // SQLite AVG çevirisi için double; tek ondalık Math.Round ile.
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }

    // Benzer ürünler: kaynak (bu ürünün benzer listesi) ve hedef (başka ürünlerin
    // benzer listesinde bu ürün) olmak üzere iki ayrı navigation.
    public ICollection<SimilarProduct> SimilarProductsOut { get; set; } = new List<SimilarProduct>();
    public ICollection<SimilarProduct> SimilarProductsIn { get; set; } = new List<SimilarProduct>();
}
