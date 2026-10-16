using System.Text.Json;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Authorize(Roles = "admin")]
[ApiController]
[Route("api/[controller]")]
public class ImportTemplatesController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    public record TemplateDto(string Name, Dictionary<string, string> Mappings, int StartRow, string? MatchBy, string? FileName);

    [HttpGet]
    public ActionResult<object> GetAll() =>
        Ok(db.ImportTemplates.OrderByDescending(t => t.Id).ToList()
            .Select(t => new
            {
                t.Id,
                t.Name,
                mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(t.MappingsJson) ?? new(),
                t.StartRow,
                matchBy = t.MatchBy,
                fileName = t.FileName,
                hasFile = !string.IsNullOrEmpty(t.StoredFile),
                t.CreatedAt
            }));

    private static string NormalizeMatchBy(string? v) =>
        v?.Trim() switch
        {
            "productId" => "productId",
            "barcode" => "barcode",
            _ => "productCode",
        };

    [HttpPost]
    public ActionResult<object> Create(TemplateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { error = "Şablon adı zorunlu." });
        var t = new ImportTemplate
        {
            Name = dto.Name.Trim(),
            MappingsJson = JsonSerializer.Serialize(dto.Mappings ?? new()),
            StartRow = dto.StartRow < 1 ? 2 : dto.StartRow,
            MatchBy = NormalizeMatchBy(dto.MatchBy),
            FileName = dto.FileName?.Trim() ?? "",
            CreatedAt = DateTime.UtcNow
        };
        db.ImportTemplates.Add(t);
        db.SaveChanges();
        return Ok(new { t.Id, t.Name });
    }

    [HttpPut("{id:int}")]
    public ActionResult<object> Update(int id, TemplateDto dto)
    {
        var t = db.ImportTemplates.Find(id);
        if (t is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(dto.Name)) t.Name = dto.Name.Trim();
        if (dto.Mappings is not null) t.MappingsJson = JsonSerializer.Serialize(dto.Mappings);
        if (dto.StartRow >= 1) t.StartRow = dto.StartRow;
        if (dto.MatchBy is not null) t.MatchBy = NormalizeMatchBy(dto.MatchBy);
        if (dto.FileName is not null) t.FileName = dto.FileName.Trim();
        db.SaveChanges();
        return Ok(new { t.Id, t.Name });
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var t = db.ImportTemplates.Find(id);
        if (t is null) return NotFound();
        if (!string.IsNullOrEmpty(t.StoredFile))
        {
            var path = Path.Combine(env.ContentRootPath, "App_Data", "imports", t.StoredFile);
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
        db.ImportTemplates.Remove(t);
        db.SaveChanges();
        return NoContent();
    }
}
