namespace Backend.Services.SimilarProducts;

public static class SimilarProductConstants
{
    // Ürün başına manuel benzer ürün üst sınırı (sabit, kolay değiştirilebilir).
    public const int MaxManualSimilar = 12;
    // Müşteri tarafında minimum gösterim sayısı; manuel bu veya üstünde ise otomatik ekleme yapılmaz.
    public const int MinDisplayCount = 6;
    // Müşteri tarafında toplam gösterim üst sınırı.
    public const int MaxDisplayCount = 12;
}