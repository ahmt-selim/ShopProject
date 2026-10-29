using System.Net;
using Backend.Auth;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Backend.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController(TokenService tokens, AppDbContext db, IEmailSender email, ILogger<AccountController> logger) : ControllerBase
{
    public record ChangePasswordDto(string? CurrentPassword, string? NewPassword, string? ConfirmNewPassword);

    [HttpPost("change-password")]
    [EnableRateLimiting("account-change-password")]
    public async Task<ActionResult<object>> ChangePassword(ChangePasswordDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.CurrentPassword)
            || string.IsNullOrWhiteSpace(dto.NewPassword)
            || string.IsNullOrWhiteSpace(dto.ConfirmNewPassword))
            return BadRequest(new { error = "Tüm alanlar zorunlu." });

        if (!string.Equals(dto.NewPassword, dto.ConfirmNewPassword, StringComparison.Ordinal))
            return BadRequest(new { error = "Yeni şifreler eşleşmiyor." });

        var pwError = PasswordRules.Validate(dto.NewPassword);
        if (pwError is not null)
            return BadRequest(new { error = pwError });

        var userId = ClaimsHelper.UserId(User);
        var user = db.Users.Find(userId);
        if (user is null) return Unauthorized();

        if (!PasswordHasher.Verify(dto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            return BadRequest(new { error = "Mevcut şifre yanlış." });

        if (PasswordHasher.Verify(dto.NewPassword, user.PasswordHash, user.PasswordSalt))
            return BadRequest(new { error = "Yeni şifre mevcut şifreyle aynı olamaz." });

        var (hash, salt) = PasswordHasher.Hash(dto.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.TokenVersion++;
        db.SaveChanges();

        try
        {
            var name = string.IsNullOrWhiteSpace(user.Name) ? "Merhaba" : $"Merhaba {user.Name.Trim()}";
            var safeName = WebUtility.HtmlEncode(name);
            var html = $"""
                <p>{safeName},</p>
                <p>Hesabınızın şifresi değiştirildi. Bu değişikliği siz yapmadıysanız lütfen hemen bizimle iletişime geçin.</p>
                <p>Sevgiler,<br/>İhtiyacın 3D</p>
                """;
            var text = $"""
                {name},

                Hesabınızın şifresi değiştirildi. Bu değişikliği siz yapmadıysanız lütfen hemen bizimle iletişime geçin.

                Sevgiler,
                İhtiyacın 3D
                """;
            await email.SendAsync(new EmailMessage(user.Email, user.Name, "Şifreniz değiştirildi – İhtiyacın 3D", html, text), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Şifre değişikliği bildirim e-postası gönderilemedi. To={Email}", user.Email);
        }

        return Ok(new { token = tokens.CreateToken(user), user = new { user.Id, user.Email, user.Name, user.Phone, user.Role, user.EmailConfirmed }, message = "Şifreniz değiştirildi." });
    }
}