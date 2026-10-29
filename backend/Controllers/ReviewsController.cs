using Backend.Auth;
using Backend.Data;
using Backend.Services.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers;

// ProblemDetails kullanan tek controller (diğerleri mevcut { error } formatında).
[ApiController]
[Route("api")]
public class ReviewsController(IReviewService reviews, AppDbContext db) : ControllerBase
{
    public record CreateReviewForm(
        int Rating, string? Title, string? Comment, List<IFormFile>? Images);
    public record UpdateReviewForm(
        int Rating, string? Title, string? Comment,
        List<int>? KeepImageIds, List<IFormFile>? Images);

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly Dictionary<string, string> ContentTypeByExtension = new()
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
    };

    private string? ViewerId =>
        User.Identity?.IsAuthenticated == true ? ClaimsHelper.UserId(User) : null;

    [HttpGet("products/{productId:int}/reviews")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> List(int productId,
        [FromQuery] int? page, [FromQuery] int? pageSize,
        [FromQuery] string? sort, [FromQuery] int? rating,
        [FromQuery] bool withImages, CancellationToken ct)
    {
        try
        {
            return Ok(await reviews.GetReviewsAsync(productId,
                page ?? 1, pageSize ?? 10, sort ?? "newest",
                rating, withImages, ViewerId, ct));
        }
        catch (ReviewNotFoundException ex)
        {
            return Problem(title: "Bulunamadı", detail: ex.Message, statusCode: 404);
        }
    }

    [HttpGet("products/{productId:int}/reviews/summary")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> Summary(int productId, CancellationToken ct)
    {
        try
        {
            return Ok(await reviews.GetSummaryAsync(productId, ct));
        }
        catch (ReviewNotFoundException ex)
        {
            return Problem(title: "Bulunamadı", detail: ex.Message, statusCode: 404);
        }
    }

    [HttpGet("products/{productId:int}/reviews/eligibility")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> Eligibility(int productId, CancellationToken ct)
    {
        try
        {
            return Ok(await reviews.GetEligibilityAsync(productId, ViewerId, ct));
        }
        catch (ReviewNotFoundException ex)
        {
            return Problem(title: "Bulunamadı", detail: ex.Message, statusCode: 404);
        }
    }

    [Authorize]
    [HttpGet("products/{productId:int}/reviews/mine")]
    public async Task<ActionResult<object>> Mine(int productId, CancellationToken ct)
    {
        try
        {
            var dto = await reviews.GetMineAsync(productId, ClaimsHelper.UserId(User), ct);
            if (dto is null)
                return Problem(title: "Bulunamadı", detail: "Bu ürüne yorumunuz yok.", statusCode: 404);
            return Ok(dto);
        }
        catch (ReviewNotFoundException ex)
        {
            return Problem(title: "Bulunamadı", detail: ex.Message, statusCode: 404);
        }
    }

    [Authorize]
    [HttpPost("products/{productId:int}/reviews")]
    [EnableRateLimiting("reviews-write")]
    public async Task<ActionResult<object>> Create(int productId,
        [FromForm] CreateReviewForm form, CancellationToken ct)
    {
        var userId = ClaimsHelper.UserId(User);
        var user = db.Users.Find(userId);
        if (user is null) return Unauthorized();
        if (!user.EmailConfirmed)
            return Problem(title: "E-posta dogrulanmamis", detail: "Yorum yazmak icin e-postanizi dogrulayin.", statusCode: 403);

        try
        {
            var images = await ReadAndValidateFilesAsync(form.Images, ct);
            var dto = await reviews.CreateAsync(productId, ClaimsHelper.UserId(User),
                form.Rating, form.Title, form.Comment, images, ct);
            return Created($"/api/reviews/{dto.Id}", dto);
        }
        catch (ReviewNotFoundException ex)
        {
            return Problem(title: "Bulunamadı", detail: ex.Message, statusCode: 404);
        }
        catch (ReviewValidationException ex)
        {
            return Problem(title: "Geçersiz istek", detail: ex.Message, statusCode: 400);
        }
        catch (ReviewForbiddenException ex)
        {
            return Problem(title: "Yorum yazamazsınız", detail: ex.Message, statusCode: 403);
        }
        catch (ReviewConflictException ex)
        {
            return Problem(title: "Zaten yorum yazılmış", detail: ex.Message, statusCode: 409);
        }
    }

    [Authorize]
    [HttpPut("reviews/{id:int}")]
    public async Task<ActionResult<object>> Update(int id,
        [FromForm] UpdateReviewForm form, CancellationToken ct)
    {
        try
        {
            var images = await ReadAndValidateFilesAsync(form.Images, ct);
            return Ok(await reviews.UpdateAsync(id, ClaimsHelper.UserId(User),
                form.Rating, form.Title, form.Comment, form.KeepImageIds, images, ct));
        }
        catch (ReviewNotFoundException ex)
        {
            return Problem(title: "Bulunamadı", detail: ex.Message, statusCode: 404);
        }
        catch (ReviewValidationException ex)
        {
            return Problem(title: "Geçersiz istek", detail: ex.Message, statusCode: 400);
        }
        catch (ReviewForbiddenException ex)
        {
            return Problem(title: "Yetkisiz işlem", detail: ex.Message, statusCode: 403);
        }
    }

    [Authorize]
    [HttpDelete("reviews/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            var isAdmin = User.IsInRole("admin");
            await reviews.DeleteAsync(id, ClaimsHelper.UserId(User), isAdmin, ct);
            return NoContent();
        }
        catch (ReviewNotFoundException ex)
        {
            return Problem(title: "Bulunamadı", detail: ex.Message, statusCode: 404);
        }
        catch (ReviewForbiddenException ex)
        {
            return Problem(title: "Yetkisiz işlem", detail: ex.Message, statusCode: 403);
        }
    }

    // Uzantı + content-type + boyut ön kontrolü. Dosya imzası (magic bytes)
    // service katmanında baytlar üzerinden doğrulanır.
    private static async Task<List<(byte[] Content, string Extension)>> ReadAndValidateFilesAsync(
        List<IFormFile>? files, CancellationToken ct)
    {
        var result = new List<(byte[], string)>();
        if (files is null) return result;
        if (files.Count > ReviewService.MaxImages)
            throw new ReviewValidationException("Bir yoruma en fazla 5 resim eklenebilir.");

        foreach (var file in files)
        {
            if (file.Length == 0 || file.Length > ReviewService.MaxImageBytes)
                throw new ReviewValidationException("Her resim en fazla 5MB olmalı.");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                throw new ReviewValidationException("Sadece jpg, jpeg, png, webp yüklenebilir.");
            if (!string.IsNullOrWhiteSpace(file.ContentType) &&
                !file.ContentType.Equals(ContentTypeByExtension[ext], StringComparison.OrdinalIgnoreCase))
                throw new ReviewValidationException("Dosya içeriği uzantıyla eşleşmiyor.");

            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            var bytes = ms.ToArray();
            if (!ReviewService.HasAllowedImageSignature(bytes, ext))
                throw new ReviewValidationException("Dosya içeriği resim formatıyla eşleşmiyor.");
            result.Add((bytes, ext));
        }
        return result;
    }
}
