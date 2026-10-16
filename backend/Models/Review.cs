namespace Backend.Models;

public enum ReviewStatus
{
    Pending,
    Approved,
    Rejected
}

public class Review
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string UserId { get; set; } = string.Empty;
    // Satın alma kanıtı: yoruma izin veren teslim edilmiş sipariş + satır.
    public int OrderId { get; set; }
    public int? OrderItemId { get; set; }
    public int Rating { get; set; } // 1-5
    public string? Title { get; set; } // max 100
    // Ham saklanır (sadece trim + uzunluk kontrolü). React çıktıda escape eder,
    // bu yüzden HtmlEncode YOK.
    public string? Comment { get; set; } // max 2000
    public ReviewStatus Status { get; set; } = ReviewStatus.Approved;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ReviewImage
{
    public int Id { get; set; }
    public int ReviewId { get; set; }
    public string Url { get; set; } = string.Empty; // "/uploads/reviews/<guid>.webp"
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
