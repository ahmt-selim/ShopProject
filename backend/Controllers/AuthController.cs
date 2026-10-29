using Backend.Auth;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(TokenService tokens, AppDbContext db, EmailVerificationService verifications) : ControllerBase
{
    public record RegisterDto(string Email, string Password, string? Name);
    public record LoginDto(string Email, string Password);
    public record ProfileDto(string? Name, string? Phone, string? Email);
    public record VerifyEmailDto(string? Email, string? Code);
    public record ResendVerificationDto(string? Email);

    [HttpPost("register")]
    [EnableRateLimiting("auth-register")]
    public async Task<ActionResult<object>> Register(RegisterDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { error = "E-posta ve sifre zorunlu." });
        var pwError = PasswordRules.Validate(dto.Password);
        if (pwError is not null)
            return BadRequest(new { error = pwError });
        if (db.Users.Any(u => u.Email.ToLower() == dto.Email.Trim().ToLower()))
            return Conflict(new { error = "Bu e-posta zaten kayitli." });

        var (hash, salt) = PasswordHasher.Hash(dto.Password);
        var user = new User
        {
            Email = dto.Email.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            Name = dto.Name?.Trim() ?? string.Empty,
            Role = "customer",
            EmailConfirmed = false
        };
        db.Users.Add(user);
        db.SaveChanges();

        await verifications.RequestVerificationAsync(user.Email, ct);
        return Ok(new { message = "Kayit olusturuldu. E-postaniza dogrulama kodu gonderildi.", email = user.Email });
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    public ActionResult<object> Login(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return Unauthorized(new { error = "E-posta veya sifre hatali." });
        var email = dto.Email.Trim().ToLower();
        var user = db.Users.FirstOrDefault(u => u.Email.ToLower() == email);
        if (user is null || !PasswordHasher.Verify(dto.Password, user.PasswordHash, user.PasswordSalt))
            return Unauthorized(new { error = "E-posta veya sifre hatali." });
        return Ok(new { token = tokens.CreateToken(user), user = ToDto(user) });
    }

    [HttpPost("verify-email")]
    [EnableRateLimiting("auth-reset")]
    public async Task<ActionResult<object>> VerifyEmail(VerifyEmailDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Code))
            return BadRequest(new { error = "E-posta ve kod zorunlu." });
        var email = dto.Email.Trim();
        var ok = await verifications.VerifyAsync(email, dto.Code.Trim(), ct);
        if (!ok)
            return BadRequest(new { error = "Kod gecersiz veya suresi dolmus." });

        var user = db.Users.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());
        if (user is null) return BadRequest(new { error = "Kod gecersiz veya suresi dolmus." });
        return Ok(new { token = tokens.CreateToken(user), user = ToDto(user) });
    }

    [HttpPost("resend-verification")]
    [EnableRateLimiting("auth-forgot")]
    public async Task<ActionResult<object>> ResendVerification(ResendVerificationDto dto, CancellationToken ct)
    {
        await verifications.RequestVerificationAsync(dto.Email, ct);
        return Ok(new { message = "Bu e-posta ile kayitli ve dogrulanmamis bir hesap varsa, dogrulama kodu gonderildi." });
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<object> Me()
    {
        var user = db.Users.Find(ClaimsHelper.UserId(User));
        return user is null ? Unauthorized() : Ok(ToDto(user));
    }

    [Authorize]
    [HttpPut("profile")]
    public ActionResult<object> UpdateProfile(ProfileDto dto)
    {
        var user = db.Users.Find(ClaimsHelper.UserId(User));
        if (user is null) return Unauthorized();
        if (!string.IsNullOrWhiteSpace(dto.Name)) user.Name = dto.Name.Trim();
        if (dto.Phone is not null) user.Phone = dto.Phone.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Email) && !dto.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (db.Users.Any(u => u.Email.ToLower() == dto.Email.Trim().ToLower()))
                return Conflict(new { error = "Bu e-posta baska hesapta kayitli." });
            user.Email = dto.Email.Trim();
            // E-posta degisirse yeniden dogrulama gerekir.
            user.EmailConfirmed = false;
        }
        db.SaveChanges();
        return Ok(ToDto(user));
    }

    private static object ToDto(User u) => new { u.Id, u.Email, u.Name, u.Phone, u.Role, u.EmailConfirmed };
}