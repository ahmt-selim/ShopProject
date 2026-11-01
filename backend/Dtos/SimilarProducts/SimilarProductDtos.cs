namespace Backend.Dtos.SimilarProducts;

public record SimilarDisplayDto(
    int Id,
    string Name,
    string? ImageUrl,
    decimal Price,
    decimal ListPrice,
    int Stock,
    double AverageRating,
    int ReviewCount,
    bool IsAutoSuggested
);

public record ManualSimilarItemDto(
    int Id,
    int SimilarProductId,
    int SortOrder,
    DateTime CreatedAt
);

public record UpdateSimilarRequest(List<int> SimilarProductIds, bool Bidirectional = true);