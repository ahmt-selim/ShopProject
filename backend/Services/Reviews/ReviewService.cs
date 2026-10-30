using Backend.Data;
using Backend.Dtos.Reviews;
using Backend.Models;
using Backend.Services.Files;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Reviews;

public interface IReviewService
{
    Task<ReviewListResponse> GetReviewsAsync(int productId, int page, int pageSize,
        string sort, int? rating, bool withImages, string? viewerUserId, CancellationToken ct);
    Task<ReviewSummaryDto> GetSummaryAsync(int productId, CancellationToken ct);
    Task<EligibilityDto> GetEligibilityAsync(int productId, string? userId, CancellationToken ct);
    Task<ReviewDto?> GetMineAsync(int productId, string userId, CancellationToken ct);
    Task<ReviewDto> CreateAsync(int productId, string userId, int rating,
        string? title, string? comment, IList<(byte[] Content, string Extension)> images,
        CancellationToken ct);
    Task<ReviewDto> UpdateAsync(int reviewId, string userId, int rating,
        string? title, string? comment, IList<int>? keepImageIds,
        IList<(byte[] Content, string Extension)> newImages, CancellationToken ct);
    Task DeleteAsync(int reviewId, string userId, bool isAdmin, CancellationToken ct);
}

public sealed class ReviewService(AppDbContext db, IFileStorage files) : IReviewService
{
    public const int MaxImages = 5;
    public const long MaxImageBytes = 5 * 1024 * 1024;
    public const int MaxTitleLength = 100;
    public const int MaxCommentLength = 2000;

    // Dosya imzaları (magic bytes). Uzantı + content-type kontrolü controller'da
    // yapılsa bile sunucu burada baytlara da bakar.
    public static bool HasAllowedImageSignature(byte[] content, string extension) => extension switch
    {
        ".jpg" or ".jpeg" => content.Length >= 3 &&
            content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF,
        ".png" => content.Length >= 8 &&
            content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47 &&
            content[4] == 0x0D && content[5] == 0x0A && content[6] == 0x1A && content[7] == 0x0A,
        ".webp" => content.Length >= 12 &&
            content[0] == (byte)'R' && content[1] == (byte)'I' &&
            content[2] == (byte)'F' && content[3] == (byte)'F' &&
            content[8] == (byte)'W' && content[9] == (byte)'E' &&
            content[10] == (byte)'B' && content[11] == (byte)'P',
        _ => false,
    };

    public static string MaskDisplayName(string? name)
    {
        var parts = (name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "Kullanıcı";
        if (parts.Length == 1) return parts[0];
        return $"{parts[0]} {parts[1][0]}."; // "Ahmet K."
    }

    public async Task<ReviewListResponse> GetReviewsAsync(int productId, int page, int pageSize,
        string sort, int? rating, bool withImages, string? viewerUserId, CancellationToken ct)
    {
        if (!await db.Products.AsNoTracking().AnyAsync(p => p.Id == productId, ct))
            throw new ReviewNotFoundException("Ürün bulunamadı.");

        var size = Math.Clamp(pageSize, 1, 50);
        var p = Math.Max(page, 1);

        var query = from r in db.Reviews.AsNoTracking()
                    join u in db.Users.AsNoTracking() on r.UserId equals u.Id
                    where r.ProductId == productId && r.Status == ReviewStatus.Approved
                    select new { r, userName = u.Name };

        if (rating is >= 1 and <= 5)
            query = query.Where(x => x.r.Rating == rating.Value);
        if (withImages)
            query = query.Where(x => db.ReviewImages.Any(i => i.ReviewId == x.r.Id));

        query = sort switch
        {
            "highest" => query.OrderByDescending(x => x.r.Rating).ThenByDescending(x => x.r.CreatedAt),
            "lowest" => query.OrderBy(x => x.r.Rating).ThenByDescending(x => x.r.CreatedAt),
            _ => query.OrderByDescending(x => x.r.CreatedAt).ThenByDescending(x => x.r.Id),
        };

        var total = await query.CountAsync(ct);
        var rows = await query.Skip((p - 1) * size).Take(size).ToListAsync(ct);

        var ids = rows.Select(x => x.r.Id).ToHashSet();
        var images = await db.ReviewImages.AsNoTracking()
            .Where(i => ids.Contains(i.ReviewId))
            .OrderBy(i => i.SortOrder)
            .ToListAsync(ct);
        var byReview = images.GroupBy(i => i.ReviewId)
            .ToDictionary(g => g.Key, g => g.Select(i => new ReviewImageDto(i.Id, i.Url, i.SortOrder)).ToList());

        return new ReviewListResponse(
            rows.Select(x => new ReviewDto(
                x.r.Id, x.r.ProductId, x.r.Rating, x.r.Title, x.r.Comment,
                MaskDisplayName(x.userName),
                VerifiedPurchase: true, // yazabilen herkes satın almış demektir
                IsMine: viewerUserId == x.r.UserId,
                x.r.CreatedAt, x.r.UpdatedAt,
                byReview.TryGetValue(x.r.Id, out var list) ? list : [])).ToList(),
            total, p, size, (int)Math.Ceiling(total / (double)size));
    }

    public async Task<ReviewSummaryDto> GetSummaryAsync(int productId, CancellationToken ct)
    {
        if (!await db.Products.AsNoTracking().AnyAsync(p => p.Id == productId, ct))
            throw new ReviewNotFoundException("Ürün bulunamadı.");

        var groups = await db.Reviews.AsNoTracking()
            .Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved)
            .GroupBy(r => r.Rating)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var dist = new Dictionary<string, int>();
        for (var star = 1; star <= 5; star++)
            dist[star.ToString()] = groups.Where(g => g.Key == star).Sum(g => g.Count);

        var count = dist.Values.Sum();
        var avg = count == 0 ? 0 : Math.Round(
            await db.Reviews.AsNoTracking()
                .Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved)
                .AverageAsync(r => (double)r.Rating, ct), 1);

        return new ReviewSummaryDto(avg, count, dist);
    }

    public async Task<EligibilityDto> GetEligibilityAsync(int productId, string? userId, CancellationToken ct)
    {
        if (!await db.Products.AsNoTracking().AnyAsync(p => p.Id == productId, ct))
            throw new ReviewNotFoundException("Ürün bulunamadı.");
        if (userId is null) return new EligibilityDto(false, "login_required", null);

        var existingId = await db.Reviews.AsNoTracking()
            .Where(r => r.ProductId == productId && r.UserId == userId)
            .Select(r => (int?)r.Id).FirstOrDefaultAsync(ct);
        if (existingId.HasValue)
            return new EligibilityDto(false, "already_reviewed", existingId);

        if (await FindDeliveredItemAsync(productId, userId, ct) is null)
        {
            // Ürün bir siparişte var ama teslim edilmemiş mi, hiç alınmamış mı?
            var hasOpenOrder = await (from o in db.Orders.AsNoTracking()
                                      join i in db.OrderItems.AsNoTracking() on o.Id equals i.OrderId
                                      where o.UserId == userId && i.ProductId == productId
                                          && o.Status != OrderStatuses.Delivered
                                          && o.Status != OrderStatuses.Cancelled
                                      select o.Id).AnyAsync(ct);
            return new EligibilityDto(false, hasOpenOrder ? "order_not_completed" : "not_purchased", null);
        }

        return new EligibilityDto(true, null, null);
    }

    public async Task<ReviewDto?> GetMineAsync(int productId, string userId, CancellationToken ct)
    {
        if (!await db.Products.AsNoTracking().AnyAsync(p => p.Id == productId, ct))
            throw new ReviewNotFoundException("Ürün bulunamadı.");

        var row = await (from r in db.Reviews.AsNoTracking()
                         join u in db.Users.AsNoTracking() on r.UserId equals u.Id
                         where r.ProductId == productId && r.UserId == userId
                         select new { r, userName = u.Name }).FirstOrDefaultAsync(ct);
        if (row is null) return null;

        var images = await db.ReviewImages.AsNoTracking()
            .Where(i => i.ReviewId == row.r.Id)
            .OrderBy(i => i.SortOrder)
            .Select(i => new ReviewImageDto(i.Id, i.Url, i.SortOrder))
            .ToListAsync(ct);

        return new ReviewDto(
            row.r.Id, row.r.ProductId, row.r.Rating, row.r.Title, row.r.Comment,
            MaskDisplayName(row.userName), VerifiedPurchase: true, IsMine: true,
            row.r.CreatedAt, row.r.UpdatedAt, images);
    }

    public async Task<ReviewDto> CreateAsync(int productId, string userId, int rating,
        string? title, string? comment, IList<(byte[] Content, string Extension)> images,
        CancellationToken ct)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId, ct))
            throw new ReviewNotFoundException("Ürün bulunamadı.");
        ValidateText(rating, title, comment);
        ValidateImages(images, 0);

        if (await db.Reviews.AnyAsync(r => r.ProductId == productId && r.UserId == userId, ct))
            throw new ReviewConflictException("Bu ürüne zaten yorum yazdınız.");

        // Frontend'e güvenme: satın alma + teslim kontrolü sunucuda.
        var proof = await FindDeliveredItemAsync(productId, userId, ct)
            ?? throw new ReviewForbiddenException("Yorum yazmak için ürünü teslim almış olmanız gerekir.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var review = new Review
        {
            ProductId = productId,
            UserId = userId,
            OrderId = proof.OrderId,
            OrderItemId = proof.OrderItemId,
            Rating = rating,
            Title = Normalize(title, MaxTitleLength),
            Comment = Normalize(comment, MaxCommentLength),
            Status = ReviewStatus.Approved, // şimdilik otomatik yayın; moderasyon için alan hazır
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Reviews.Add(review);
        await db.SaveChangesAsync(ct);

        var sort = 0;
        foreach (var (content, ext) in images)
        {
            var url = await files.SaveReviewImageAsync(content, ext, ct);
            db.ReviewImages.Add(new ReviewImage
                { ReviewId = review.Id, Url = url, SortOrder = sort++, CreatedAt = DateTime.UtcNow });
        }
        await db.SaveChangesAsync(ct);

        await RefreshProductStatsAsync(productId, ct);
        await tx.CommitAsync(ct);

        // Yeni yorumu yeniden sorgulamak yerine mine DTO'su döndürülür;
        // aksi halde sıralama/filtre yarışı nedeniyle First() "Sequence contains no elements"
        // atıp bağlantı düşebilir (frontend'de "failed to fetch").
        return await GetMineAsync(productId, userId, ct)
            ?? throw new ReviewNotFoundException("Yorum oluşturuldu ancak okunamadı.");
    }

    public async Task<ReviewDto> UpdateAsync(int reviewId, string userId, int rating,
        string? title, string? comment, IList<int>? keepImageIds,
        IList<(byte[] Content, string Extension)> newImages, CancellationToken ct)
    {
        var review = await db.Reviews.FindAsync([reviewId], ct)
            ?? throw new ReviewNotFoundException("Yorum bulunamadı.");
        if (review.UserId != userId)
            throw new ReviewForbiddenException("Sadece kendi yorumunuzu düzenleyebilirsiniz.");

        ValidateText(rating, title, comment);

        var current = await db.ReviewImages
            .Where(i => i.ReviewId == reviewId).OrderBy(i => i.SortOrder).ToListAsync(ct);
        var keep = keepImageIds is null
            ? current.Select(i => i.Id).ToHashSet() // keep gönderilmediyse hepsini koru
            : keepImageIds.ToHashSet();
        ValidateImages(newImages, current.Count(i => keep.Contains(i.Id)));

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        review.Rating = rating;
        review.Title = Normalize(title, MaxTitleLength);
        review.Comment = Normalize(comment, MaxCommentLength);
        review.UpdatedAt = DateTime.UtcNow;

        foreach (var img in current.Where(i => !keep.Contains(i.Id)))
        {
            db.ReviewImages.Remove(img);
            await files.DeleteAsync(img.Url, ct);
        }

        var sort = current.Count == 0 ? 0 : current.Max(i => i.SortOrder) + 1;
        foreach (var (content, ext) in newImages)
        {
            var url = await files.SaveReviewImageAsync(content, ext, ct);
            db.ReviewImages.Add(new ReviewImage
                { ReviewId = review.Id, Url = url, SortOrder = sort++, CreatedAt = DateTime.UtcNow });
        }
        await db.SaveChangesAsync(ct);

        await RefreshProductStatsAsync(review.ProductId, ct);
        await tx.CommitAsync(ct);

        return await GetMineAsync(review.ProductId, userId, ct)
            ?? throw new ReviewNotFoundException("Yorum güncellendi ancak okunamadı.");
    }

    public async Task DeleteAsync(int reviewId, string userId, bool isAdmin, CancellationToken ct)
    {
        var review = await db.Reviews.FindAsync([reviewId], ct)
            ?? throw new ReviewNotFoundException("Yorum bulunamadı.");
        if (review.UserId != userId && !isAdmin)
            throw new ReviewForbiddenException("Sadece kendi yorumunuzu silebilirsiniz.");

        var urls = await db.ReviewImages
            .Where(i => i.ReviewId == reviewId).Select(i => i.Url).ToListAsync(ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        db.Reviews.Remove(review); // ReviewImage satırları cascade ile silinir
        await db.SaveChangesAsync(ct);
        await RefreshProductStatsAsync(review.ProductId, ct);
        await tx.CommitAsync(ct);

        foreach (var url in urls)
            await files.DeleteAsync(url, ct);
    }

    // Teslim edilmiş siparişteki en güncel satır (satın alma kanıtı).
    private Task<Proof?> FindDeliveredItemAsync(int productId, string userId, CancellationToken ct) =>
        (from o in db.Orders.AsNoTracking()
         join i in db.OrderItems.AsNoTracking() on o.Id equals i.OrderId
         where o.UserId == userId && i.ProductId == productId
             && o.Status == OrderStatuses.Delivered
         orderby o.Id descending
         select new Proof(o.Id, i.Id)).FirstOrDefaultAsync(ct);

    private sealed record Proof(int OrderId, int OrderItemId);

    private static void ValidateText(int rating, string? title, string? comment)
    {
        if (rating is < 1 or > 5)
            throw new ReviewValidationException("Puan 1-5 arasında olmalı.");
        if (title is not null && title.Trim().Length > MaxTitleLength)
            throw new ReviewValidationException("Başlık en fazla 100 karakter olmalı.");
        if (comment is not null && comment.Trim().Length > MaxCommentLength)
            throw new ReviewValidationException("Yorum en fazla 2000 karakter olmalı.");
    }

    private static void ValidateImages(
        IList<(byte[] Content, string Extension)> images, int existingCount)
    {
        if (existingCount + images.Count > MaxImages)
            throw new ReviewValidationException("Bir yoruma en fazla 5 resim eklenebilir.");
        foreach (var (content, ext) in images)
        {
            if (content.Length == 0 || content.Length > MaxImageBytes)
                throw new ReviewValidationException("Her resim en fazla 5MB olmalı.");
            if (!HasAllowedImageSignature(content, ext))
                throw new ReviewValidationException(
                    "Resim formatı geçersiz (sadece jpg, jpeg, png, webp).");
        }
    }

    private static string? Normalize(string? value, int max)
    {
        var t = value?.Trim();
        if (string.IsNullOrEmpty(t)) return null;
        return t.Length > max ? t[..max] : t; // savunma; asıl kontrol ValidateText'te
    }

    // Denormalize Product.AverageRating/ReviewCount aynı transaction içinde güncellenir.
    private async Task RefreshProductStatsAsync(int productId, CancellationToken ct)
    {
        var approved = db.Reviews
            .Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved);
        var count = await approved.CountAsync(ct);
        var avg = count == 0 ? 0 : Math.Round(await approved.AverageAsync(r => (double)r.Rating, ct), 1);

        var product = await db.Products.FindAsync([productId], ct);
        if (product is null) return;
        product.AverageRating = avg;
        product.ReviewCount = count;
        await db.SaveChangesAsync(ct);
    }
}
