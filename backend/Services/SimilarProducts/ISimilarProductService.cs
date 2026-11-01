using Backend.Dtos.SimilarProducts;

namespace Backend.Services.SimilarProducts;

public interface ISimilarProductService
{
    Task<List<int>> GetManualSimilarIdsAsync(int productId);
    Task<List<SimilarDisplayDto>> GetManualSimilarForAdminAsync(int productId);
    Task UpdateManualSimilarAsync(int productId, List<int> similarProductIds, bool bidirectional);
    Task<List<SimilarDisplayDto>> GetSimilarProductsForDisplayAsync(int productId);
}