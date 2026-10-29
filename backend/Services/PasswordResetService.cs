using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Backend.Auth;
using Backend.Data;
using Backend.Models;
using Backend.Services.Email;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public enum PasswordResetResult { Done, InvalidLink, WeakPassword }

public sealed class PasswordResetService(
    AppDbContext db,
    PasswordResetMailQueue mailQueue,
    IConfiguration config)
{
    public const int TokenLifetimeMinutes = 60;
    public const int MaxRequestsPerHourPerEmail = 3;
    public const int MaxFailedAttempts = 5;

    // Her zaman aynı sonucu döndürür; kayıtlı/kayıtsız ayrımı sızdırılmaz.
    // E-posta kuyruğa bırakılır, gönderim arka planda yapılır (bloklama yok,
    // sleep ile timing normalizasyonu yok). Kod/şifre asla loglanmaz.
    public async Task RequestResetAsync(string? email, CancellationToken ct = default)
    {
        var normalized = (email ?? "").Trim().ToLower();
        if (normalized.Length == 0) return;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, ct);
        if (user is null) return;

        var now = DateTime.UtcNow;
        var recentCount = await db.PasswordResetTokens.CountAsync(
            t => t.UserId == user.Id && t.Purpose == "password_reset" && t.CreatedAt >= now.AddHours(-1), ct);
        // E-posta bazlı limit (saatte 3): aşılınca mail gönderilmez ama
        // kullanıcıya yine aynı genel yanıt döner; 429 YOK (enumeration önlemi).
        if (recentCount >= MaxRequestsPerHourPerEmail) return;

        // Eski kullanılmamış kodlar geçersiz kılınır.
        await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.Purpose == "password_reset" && t.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now), ct);

        var code = GenerateCode();
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            Purpose = "password_reset",
            TokenHash = HashToken(code),
            ExpiresAt = now.AddMinutes(TokenLifetimeMinutes),
            CreatedAt = now,
        });
        await db.SaveChangesAsync(ct);

        var siteUrl = (config["App:FrontendBaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
        mailQueue.Enqueue(new PasswordResetMail(user.Email, user.Name, code, siteUrl));
    }

    // Kodu tüketmeden doğrular; yanlış denemeler sayaca işlenir.
    // Şifre ekranına geçmeden önce kodun doğru olduğu bu metotla onaylanır.
    public async Task<bool> VerifyCodeAsync(string? email, string? code, CancellationToken ct = default)
    {
        var normalized = (email ?? "").Trim().ToLower();
        if (normalized.Length == 0 || string.IsNullOrWhiteSpace(code))
            return false;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, ct);
        if (user is null)
            return false;

        var matchId = await FindMatchingTokenIdAsync(user.Id, code.Trim(), ct);
        if (matchId is null)
        {
            await RegisterFailedAttemptAsync(user.Id, ct);
            return false;
        }
        return true;
    }

    public async Task<(PasswordResetResult Result, string? Error)> ResetPasswordAsync(
        string? email, string? code, string? newPassword, CancellationToken ct = default)
    {
        var normalized = (email ?? "").Trim().ToLower();
        if (normalized.Length == 0 || string.IsNullOrWhiteSpace(code))
            return (PasswordResetResult.InvalidLink, null);

        var pwError = PasswordRules.Validate(newPassword);
        if (pwError is not null)
            return (PasswordResetResult.WeakPassword, pwError);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, ct);
        if (user is null)
            return (PasswordResetResult.InvalidLink, null);

        var now = DateTime.UtcNow;
        var matchId = await FindMatchingTokenIdAsync(user.Id, code.Trim(), ct);
        if (matchId is null)
        {
            // Yanlış kod: deneme sayaçlarını artır, sınıra ulaşanları iptal et.
            // Hangi sebeple tutmadıysa tek tip yanıt döner.
            await RegisterFailedAttemptAsync(user.Id, ct);
            return (PasswordResetResult.InvalidLink, null);
        }

        // Şifre güncelleme + kod işaretleme aynı transaction'da; kod
        // koşullu güncellenir (UsedAt IS NULL), 1 satır etkilenmezse geçersiz.
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var marked = await db.PasswordResetTokens
            .Where(t => t.Id == matchId && t.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now), ct);
        if (marked != 1)
        {
            await tx.RollbackAsync(ct);
            return (PasswordResetResult.InvalidLink, null);
        }

        var (hash, salt) = PasswordHasher.Hash(newPassword!);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        // Eski JWT'leri geçersiz kıl (OnTokenValidated tv karşılaştırır).
        user.TokenVersion++;

        // Başarılı reset sonrası kalan kullanılmamış kodların tümü iptal.
        await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.Purpose == "password_reset" && t.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now), ct);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (PasswordResetResult.Done, null);
    }

    // 6 haneli sayısal kod (baştaki sıfırlar korunur). Düz hali yalnızca
    // e-postada yer alır; DB'ye SHA-256 hash'i yazılır.
    internal static string GenerateCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);

    internal static string HashToken(string plainToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(plainToken)));

    // Adaylar DB'den çekilip sabit zamanlı karşılaştırma ile eşleştirilir
    // (hash'in kendisi SQL eşitliğiyle aranmaz). Eşleşme yoksa null.
    private async Task<int?> FindMatchingTokenIdAsync(string userId, string code, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var presentedHash = HashToken(code);
        var candidates = await db.PasswordResetTokens
            .Where(t => t.UserId == userId && t.Purpose == "password_reset" && t.UsedAt == null && t.ExpiresAt > now)
            .Select(t => new { t.Id, t.TokenHash })
            .ToListAsync(ct);

        foreach (var c in candidates)
        {
            byte[] stored, presented;
            try
            {
                stored = Convert.FromBase64String(c.TokenHash);
                presented = Convert.FromBase64String(presentedHash);
            }
            catch (FormatException)
            {
                continue;
            }
            if (stored.Length == presented.Length && CryptographicOperations.FixedTimeEquals(stored, presented))
                return c.Id;
        }
        return null;
    }

    private async Task RegisterFailedAttemptAsync(string userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await db.PasswordResetTokens
            .Where(t => t.UserId == userId && t.Purpose == "password_reset" && t.UsedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.FailedAttempts, t => t.FailedAttempts + 1), ct);
        await db.PasswordResetTokens
            .Where(t => t.UserId == userId && t.Purpose == "password_reset" && t.UsedAt == null && t.FailedAttempts >= MaxFailedAttempts)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now), ct);
    }
}
