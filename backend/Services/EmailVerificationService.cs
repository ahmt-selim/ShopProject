using System.Security.Cryptography;
using Backend.Data;
using Backend.Models;
using Backend.Services.Email;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public sealed class EmailVerificationService(
    AppDbContext db,
    EmailVerificationMailQueue mailQueue,
    IConfiguration config)
{
    public const int CodeLifetimeMinutes = 10;
    public const int MaxRequestsPerHourPerEmail = 3;
    public const int MaxFailedAttempts = 5;

    public async Task RequestVerificationAsync(string? email, CancellationToken ct = default)
    {
        var normalized = (email ?? "").Trim().ToLower();
        if (normalized.Length == 0) return;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, ct);
        if (user is null || user.EmailConfirmed) return;

        var now = DateTime.UtcNow;
        var recentCount = await db.PasswordResetTokens.CountAsync(
            t => t.UserId == user.Id && t.Purpose == "email_verification" && t.CreatedAt >= now.AddHours(-1), ct);
        if (recentCount >= MaxRequestsPerHourPerEmail) return;

        await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.Purpose == "email_verification" && t.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now), ct);

        var code = PasswordResetService.GenerateCode();
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            Purpose = "email_verification",
            TokenHash = PasswordResetService.HashToken(code),
            ExpiresAt = now.AddMinutes(CodeLifetimeMinutes),
            CreatedAt = now,
        });
        await db.SaveChangesAsync(ct);

        var siteUrl = (config["App:FrontendBaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
        mailQueue.Enqueue(new EmailVerificationMail(user.Email, user.Name, code, siteUrl));
    }

    public async Task<bool> VerifyAsync(string? email, string? code, CancellationToken ct = default)
    {
        var normalized = (email ?? "").Trim().ToLower();
        if (normalized.Length == 0 || string.IsNullOrWhiteSpace(code)) return false;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, ct);
        if (user is null) return false;
        if (user.EmailConfirmed) return true;

        var matchId = await FindMatchingTokenIdAsync(user.Id, code.Trim(), ct);
        if (matchId is null)
        {
            await RegisterFailedAttemptAsync(user.Id, ct);
            return false;
        }

        var now = DateTime.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var marked = await db.PasswordResetTokens
            .Where(t => t.Id == matchId && t.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now), ct);
        if (marked != 1)
        {
            await tx.RollbackAsync(ct);
            return false;
        }

        user.EmailConfirmed = true;
        await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.Purpose == "email_verification" && t.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now), ct);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    private async Task<int?> FindMatchingTokenIdAsync(string userId, string code, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var presentedHash = PasswordResetService.HashToken(code);
        var candidates = await db.PasswordResetTokens
            .Where(t => t.UserId == userId && t.Purpose == "email_verification" && t.UsedAt == null && t.ExpiresAt > now)
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
            .Where(t => t.UserId == userId && t.Purpose == "email_verification" && t.UsedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.FailedAttempts, t => t.FailedAttempts + 1), ct);
        await db.PasswordResetTokens
            .Where(t => t.UserId == userId && t.Purpose == "email_verification" && t.UsedAt == null && t.FailedAttempts >= MaxFailedAttempts)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now), ct);
    }
}