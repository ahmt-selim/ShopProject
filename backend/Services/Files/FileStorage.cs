namespace Backend.Services.Files;

// İleride Azure Blob/S3'e geçilebilmesi için dosya yazma soyutlaması.
// Şimdilik wwwroot altına yerel disk (LocalFileStorage).
public interface IFileStorage
{
    // content doğrulanmış baytlar, extension ".webp" formatında (noktalı, küçük harf).
    // Geriye "/uploads/reviews/<guid>.webp" gibi relative URL döner.
    Task<string> SaveReviewImageAsync(byte[] content, string extension, CancellationToken ct = default);
    Task DeleteAsync(string relativeUrl, CancellationToken ct = default);
}

public sealed class LocalFileStorage(IWebHostEnvironment env) : IFileStorage
{
    private const string Folder = "reviews";

    public async Task<string> SaveReviewImageAsync(byte[] content, string extension, CancellationToken ct = default)
    {
        var root = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var dir = Path.Combine(root, "uploads", Folder);
        Directory.CreateDirectory(dir);

        var name = $"{Guid.NewGuid():N}{extension}";
        await File.WriteAllBytesAsync(Path.Combine(dir, name), content, ct);
        return $"/uploads/{Folder}/{name}";
    }

    public Task DeleteAsync(string relativeUrl, CancellationToken ct = default)
    {
        // Sadece kendi klasörümüzdeki dosyalar silinir (path traversal koruması).
        var prefix = $"/uploads/{Folder}/";
        if (!relativeUrl.StartsWith(prefix, StringComparison.Ordinal)) return Task.CompletedTask;
        var fileName = relativeUrl[prefix.Length..];
        if (fileName.Contains('/') || fileName.Contains('\\') || fileName.Contains("..")) return Task.CompletedTask;

        var root = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var path = Path.Combine(root, "uploads", Folder, fileName);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }
}
