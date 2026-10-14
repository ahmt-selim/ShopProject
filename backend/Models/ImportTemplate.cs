namespace Backend.Models;

// Excel toplu ürün aktarımı için kayıtlı şablon.
// Mappings: ürün alanı -> excel sütun harfi (A, B, C...), JSON olarak saklanır.
public class ImportTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MappingsJson { get; set; } = "{}";
    public int StartRow { get; set; } = 2;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
