using Backend.Auth;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(TokenService tokens, AppDbContext db) : ControllerBase
{
    public record RegisterDto(string Email, string Password, string? Name);
    public record LoginDto(string Email, string Password);
    public record ProfileDto(string? Name, string? Phone, string? Email);

    [HttpPost("register")]
    [EnableRateLimiting("auth-register")]
    public ActionResult<object> Register(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { error = "E-posta ve şifre zorunlu." });
        var pwError = PasswordRules.Validate(dto.Password);
        if (pwError is not null)
            return BadRequest(new { error = pwError });
        if (db.Users.Any(u => u.Email.ToLower() == dto.Email.Trim().ToLower()))
            return Conflict(new { error = "Bu e-posta zaten kayıtlı." });

        var (hash, salt) = PasswordHasher.Hash(dto.Password);
        var user = new User
        {
            Email = dto.Email.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            Name = dto.Name?.Trim() ?? string.Empty,
            Role = "customer"
        };
        db.Users.Add(user);
        db.SaveChanges();
        return Ok(new { token = tokens.CreateToken(user), user = ToDto(user) });
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    public ActionResult<object> Login(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return Unauthorized(new { error = "E-posta veya şifre hatalı." });
        var email = dto.Email.Trim().ToLower();
        var user = db.Users.FirstOrDefault(u => u.Email.ToLower() == email);
        if (user is null || !PasswordHasher.Verify(dto.Password, user.PasswordHash, user.PasswordSalt))
            return Unauthorized(new { error = "E-posta veya şifre hatalı." });
        return Ok(new { token = tokens.CreateToken(user), user = ToDto(user) });
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
                return Conflict(new { error = "Bu e-posta başka hesapta kayıtlı." });
            user.Email = dto.Email.Trim();
        }
        db.SaveChanges();
        return Ok(ToDto(user));
    }

    private static object ToDto(User u) => new { u.Id, u.Email, u.Name, u.Phone, u.Role };
}
