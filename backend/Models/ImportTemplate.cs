namespace Backend.Models;

// Excel toplu ürün aktarımı için kayıtlı şablon.
// Mappings: ürün alanı -> excel sütun harfi (A, B, C...), JSON olarak saklanır.
public class ImportTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MappingsJson { get; set; } = "{}";
    public int StartRow { get; set; } = 2;
    // Ürün eşleştirme alanı: productId | productCode | barcode
    public string MatchBy { get; set; } = "productCode";
    // Son seçilen excel dosyasının adı (tarayıcı tam yolu vermez, dosya adı saklanır).
    public string FileName { get; set; } = string.Empty;
    // Sunucuda saklanan kopyanın dosya adı (App_Data/imports altında).
    public string StoredFile { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
