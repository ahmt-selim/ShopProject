using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers;

// Şifre sıfırlama akışı. Hata formatı AuthController ile aynı: { error }.
// Token/şifre/reset bağlantısı ASLA loglanmaz. Başarılı reset otomatik
// giriş yapmaz (yalnızca bilgi mesajı döner).
[ApiController]
[Route("api/auth")]
public class PasswordResetController(PasswordResetService resets) : ControllerBase
{
    public record ForgotDto(string? Email);
    public record VerifyDto(string? Email, string? Code);
    public record ResetDto(string? Email, string? Code, string? NewPassword);

    private const string GenericMessage =
        "Bu e-posta ile kayıtlı bir hesap varsa, şifre sıfırlama kodu gönderildi.";

    // Kayıtlı/kayıtsız her durumda 200 + aynı mesaj (enumeration önlemi).
    // E-posta bazlı limit aşımında da aynı yanıt; IP limiti aşımında 429.
    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth-forgot")]
    public async Task<ActionResult<object>> Forgot(ForgotDto dto, CancellationToken ct)
    {
        await resets.RequestResetAsync(dto.Email, ct);
        return Ok(new { message = GenericMessage });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("auth-reset")]
    public async Task<ActionResult<object>> Reset(ResetDto dto, CancellationToken ct)
    {
        var (result, error) = await resets.ResetPasswordAsync(dto.Email, dto.Code, dto.NewPassword, ct);
        return result switch
        {
            PasswordResetResult.Done => Ok(new { message = "Şifren güncellendi." }),
            PasswordResetResult.WeakPassword => BadRequest(new { error }),
            _ => BadRequest(new { error = "Kod geçersiz veya süresi dolmuş." }),
        };
    }

    // Kodun doğruluğunu şifre sormadan denetler (şifre ekranına geçiş kapısı).
    // Yanlış denemeler sayaca işlenir; reset ile aynı IP limit kovasını paylaşır.
    [HttpPost("verify-reset-code")]
    [EnableRateLimiting("auth-reset")]
    public async Task<ActionResult<object>> Verify(VerifyDto dto, CancellationToken ct)
    {
        var ok = await resets.VerifyCodeAsync(dto.Email, dto.Code, ct);
        return ok
            ? Ok(new { valid = true })
            : BadRequest(new { error = "Kod geçersiz veya süresi dolmuş." });
    }
}
