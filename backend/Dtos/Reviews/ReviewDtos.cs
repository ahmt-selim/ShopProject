namespace Backend.Dtos.Reviews;

public record ReviewImageDto(int Id, string Url, int SortOrder);

public record ReviewDto(
    int Id,
    int ProductId,
    int Rating,
    string? Title,
    string? Comment,
    string DisplayName,
    bool VerifiedPurchase,
    bool IsMine,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<ReviewImageDto> Images);

public record ReviewListResponse(
    List<ReviewDto> Items,
    int Total,
    int Page,
    int PageSize,
    int TotalPages);

public record ReviewSummaryDto(
    double AverageRating,
    int ReviewCount,
    Dictionary<string, int> Distribution);

public record EligibilityDto(bool CanReview, string? Reason, int? ExistingReviewId);
