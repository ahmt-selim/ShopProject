namespace Backend.Models;

public class SimilarProduct
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int SimilarProductId { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigations: iki ayrı taraf; hedef tarafta cascade kapalı (multiple cascade path önlemi).
    public Product? SourceProduct { get; set; }
    public Product? TargetProduct { get; set; }
}