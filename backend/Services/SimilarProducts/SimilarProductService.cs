using Backend.Data;
using Backend.Dtos.SimilarProducts;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.SimilarProducts;

public class SimilarProductService(AppDbContext db) : ISimilarProductService
{
    public async Task<List<int>> GetManualSimilarIdsAsync(int productId)
    {
        return await db.SimilarProducts
            .AsNoTracking()
            .Where(s => s.ProductId == productId)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .Select(s => s.SimilarProductId)
            .ToListAsync();
    }

    public async Task<List<SimilarDisplayDto>> GetManualSimilarForAdminAsync(int productId)
    {
        var ids = await GetManualSimilarIdsAsync(productId);
        if (ids.Count == 0) return new List<SimilarDisplayDto>();

        var products = await db.Products
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();
        var images = await LoadPrimaryImagesAsync(products.Select(p => p.Id));

        var byId = products.ToDictionary(p => p.Id);
        var result = new List<SimilarDisplayDto>(ids.Count);
        foreach (var id in ids)
        {
            if (!byId.TryGetValue(id, out var p)) continue;
            result.Add(ToDisplay(p, images, isAuto: false));
        }
        return result;
    }

    public async Task UpdateManualSimilarAsync(int productId, List<int> similarProductIds, bool bidirectional)
    {
        // Temel doğrulamalar.
        if (!await db.Products.AnyAsync(p => p.Id == productId))
            throw new InvalidOperationException("Ürün bulunamadı.");

        if (similarProductIds is null)
            throw new InvalidOperationException("Benzer ürün listesi boş olamaz.");

        if (similarProductIds.Count > SimilarProductConstants.MaxManualSimilar)
            throw new InvalidOperationException($"Manuel benzer ürün sayısı en fazla {SimilarProductConstants.MaxManualSimilar} olabilir.");

        if (similarProductIds.Any(id => id == productId))
            throw new InvalidOperationException("Bir ürün kendisinin benzeri olamaz.");

        if (similarProductIds.Distinct().Count() != similarProductIds.Count)
            throw new InvalidOperationException("Aynı ürün birden fazla kez eklenemez.");

        var existingProductIds = await db.Products
            .Where(p => similarProductIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();
        if (existingProductIds.Count != similarProductIds.Count)
            throw new InvalidOperationException("Listedeki bazı ürünler bulunamadı.");

        // Karşılıklı eklemede karşı tarafın manuel limiti kontrolü (atomik iptal).
        if (bidirectional)
        {
            var addedIds = similarProductIds.Except(
                await db.SimilarProducts.Where(s => s.ProductId == productId)
                    .Select(s => s.SimilarProductId).ToListAsync()).ToList();

            foreach (var targetId in addedIds)
            {
                var currentCount = await db.SimilarProducts.CountAsync(s => s.ProductId == targetId);
                if (currentCount >= SimilarProductConstants.MaxManualSimilar)
                {
                    var targetName = await db.Products.Where(p => p.Id == targetId)
                        .Select(p => p.Name).FirstOrDefaultAsync() ?? $"#{targetId}";
                    throw new InvalidOperationException(
                        $"{targetName} ürününün manuel benzer limiti ({SimilarProductConstants.MaxManualSimilar}) dolu; işlem iptal edildi.");
                }
            }
        }

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var currentIds = await db.SimilarProducts
                .Where(s => s.ProductId == productId)
                .Select(s => s.SimilarProductId)
                .ToListAsync();

            var removedIds = currentIds.Except(similarProductIds).ToList();
            var addedIds = similarProductIds.Except(currentIds).ToList();

            // Çıkarılanlar: kaynak tarafı sil; karşılıklıysa hedef tarafı da sil.
            if (removedIds.Count > 0)
            {
                await db.SimilarProducts
                    .Where(s => s.ProductId == productId && removedIds.Contains(s.SimilarProductId))
                    .ExecuteDeleteAsync();

                if (bidirectional)
                {
                    await db.SimilarProducts
                        .Where(s => removedIds.Contains(s.ProductId) && s.SimilarProductId == productId)
                        .ExecuteDeleteAsync();
                }
            }

            // Mevcut kayıtların SortOrder'ını güncelle (sıralama admin tarafından belirlenir).
            var existingRows = await db.SimilarProducts
                .Where(s => s.ProductId == productId && similarProductIds.Contains(s.SimilarProductId))
                .ToListAsync();
            var rowByTarget = existingRows.ToDictionary(r => r.SimilarProductId);
            for (var i = 0; i < similarProductIds.Count; i++)
            {
                var targetId = similarProductIds[i];
                if (rowByTarget.TryGetValue(targetId, out var row))
                {
                    row.SortOrder = i;
                }
                else
                {
                    // Yeni eklenecek.
                    db.SimilarProducts.Add(new SimilarProduct
                    {
                        ProductId = productId,
                        SimilarProductId = targetId,
                        SortOrder = i,
                        CreatedAt = DateTime.UtcNow
                    });

                    if (bidirectional)
                    {
                        // Karşı ürünün listesinin SONUNA ekle; mevcut sıralama bozulmaz.
                        var maxSort = await db.SimilarProducts
                            .Where(s => s.ProductId == targetId)
                            .MaxAsync(s => (int?)s.SortOrder) ?? -1;
                        db.SimilarProducts.Add(new SimilarProduct
                        {
                            ProductId = targetId,
                            SimilarProductId = productId,
                            SortOrder = maxSort + 1,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<SimilarDisplayDto>> GetSimilarProductsForDisplayAsync(int productId)
    {
        // 1) Manuel benzer ürünler (sıralı).
        var manualIds = await GetManualSimilarIdsAsync(productId);

        // Aktif/görünür filtresi: bu projede Product'ta aktif bayrağı yok; silinen
        // ürün zaten tabloda yok. Pasif/gizli kavramı için ek alan bulunmadığından
        // tüm kayıtlar "satışa açık" kabul edilir.
        var manualProducts = manualIds.Count == 0
            ? new List<Product>()
            : await db.Products.AsNoTracking()
                .Where(p => manualIds.Contains(p.Id))
                .ToListAsync();

        var imageMap = await LoadPrimaryImagesAsync(manualProducts.Select(p => p.Id)
            .Concat(new[] { productId }).Distinct());

        var manualSet = new HashSet<int>(manualIds);
        var result = new List<SimilarDisplayDto>();
        var manualDict = manualProducts.ToDictionary(p => p.Id);
        foreach (var id in manualIds)
        {
            if (manualDict.TryGetValue(id, out var p))
                result.Add(ToDisplay(p, imageMap, isAuto: false));
        }

        // 2) Otomatik tamamlama: manuel sayı MinDisplayCount'un altındaysa aynı kategoriden.
        if (result.Count < SimilarProductConstants.MinDisplayCount)
        {
            var currentProduct = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
            if (currentProduct is not null)
            {
                var exclude = new HashSet<int>(manualSet) { productId };
                var remainingSlots = SimilarProductConstants.MaxDisplayCount - result.Count;
                var need = Math.Min(
                    SimilarProductConstants.MinDisplayCount - result.Count,
                    remainingSlots);
                // En az need kadar; toplam MaxDisplayCount'u aşma. Yeterli ürün varsa
                // MinDisplayCount'a tamamla; daha fazlasını ekleme (kural: toplam en fazla Max).
                // need negatif olamaz.
                if (need > 0)
                {
                    // Önce stokta olanlar, sonra en yeni eklenenler (Id desc deterministik).
                    var candidates = await db.Products.AsNoTracking()
                        .Where(p => p.CategoryId == currentProduct.CategoryId
                                    && p.Id != productId
                                    && !manualSet.Contains(p.Id))
                        .OrderByDescending(p => p.Stock > 0 ? 1 : 0)
                        .ThenByDescending(p => p.Id)
                        .ToListAsync();

                    // need kadar (MinDisplayCount'a tamamlamak için) otomatik ekle; MaxDisplayCount'u aşma.
                    var autoPicked = candidates
                        .Where(p => !exclude.Contains(p.Id))
                        .Take(need)
                        .ToList();

                    var autoImages = await LoadPrimaryImagesAsync(autoPicked.Select(p => p.Id));
                    foreach (var kv in autoImages) imageMap[kv.Key] = kv.Value;
                    foreach (var p in autoPicked)
                    {
                        if (result.Count >= SimilarProductConstants.MaxDisplayCount) break;
                        result.Add(ToDisplay(p, imageMap, isAuto: true));
                    }
                }
            }
        }

        return result;
    }

    private async Task<Dictionary<int, string>> LoadPrimaryImagesAsync(IEnumerable<int> productIds)
    {
        var idSet = productIds.ToHashSet();
        if (idSet.Count == 0) return new Dictionary<int, string>();
        var rows = await db.ProductImages
            .AsNoTracking()
            .Where(i => idSet.Contains(i.ProductId))
            .OrderBy(i => i.Id)
            .ToListAsync();
        var map = new Dictionary<int, string>();
        foreach (var g in rows.GroupBy(i => i.ProductId))
        {
            map[g.Key] = g.First().Url;
        }
        return map;
    }

    private static SimilarDisplayDto ToDisplay(Product p, Dictionary<int, string> images, bool isAuto)
    {
        var img = images.TryGetValue(p.Id, out var url) && !string.IsNullOrWhiteSpace(url)
            ? url
            : null;
        return new SimilarDisplayDto(
            p.Id,
            p.Name,
            img,
            p.Price,
            p.ListPrice,
            p.Stock,
            p.AverageRating,
            p.ReviewCount,
            isAuto
        );
    }
}