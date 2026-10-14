using System.Globalization;
using System.Text.Json;
using Backend.Data;
using Backend.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

// Excel'den toplu ürün aktarımı. Sütun harfi (A, B, ... AA) -> ürün alanı eşleşmesiyle çalışır.
[Authorize(Roles = "admin")]
[ApiController]
[Route("api/products")]
public class ProductImportController(AppDbContext db) : ControllerBase
{
    private static readonly string[] KnownFields =
        ["name", "description", "price", "productCode", "barcode", "vatRate",
         "category", "brand", "stock", "imageUrls"];

    [HttpPost("import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public ActionResult<object> Import(
        IFormFile file,
        [FromForm] int? templateId,
        [FromForm] string? mappings,
        [FromForm] int? startRow)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Excel dosyası seçilmedi." });
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not ".xlsx" and not ".xls")
            return BadRequest(new { error = "Sadece .xlsx / .xls yüklenebilir." });

        Dictionary<string, string> map;
        var row = startRow ?? 2;
        if (templateId.HasValue)
        {
            var t = db.ImportTemplates.Find(templateId.Value);
            if (t is null) return NotFound(new { error = "Şablon bulunamadı." });
            map = JsonSerializer.Deserialize<Dictionary<string, string>>(t.MappingsJson) ?? new();
            if (!startRow.HasValue) row = t.StartRow;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(mappings))
                return BadRequest(new { error = "Şablon seçin ya da sütun eşleşmesi gönderin." });
            try
            {
                map = JsonSerializer.Deserialize<Dictionary<string, string>>(mappings!) ?? new();
            }
            catch
            {
                return BadRequest(new { error = "Eşleşme JSON formatı hatalı." });
            }
        }

        Dictionary<string, int> cols;
        try
        {
            cols = map.Where(kv => KnownFields.Contains(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
                .ToDictionary(kv => kv.Key, kv => ColumnIndex(kv.Value));
        }
        catch
        {
            return BadRequest(new { error = "Sütun harfleri A-Z formatında olmalı (örn. A, B, AA)." });
        }
        if (!cols.TryGetValue("name", out _))
            return BadRequest(new { error = "Ürün adı (name) sütunu zorunlu." });

        var categories = db.Categories.ToDictionary(c => c.Name.ToLower(), c => c.Id);
        var brands = db.Brands.ToDictionary(b => b.Name.ToLower(), b => b.Id);

        var imported = 0;
        var errors = new List<object>();
        using var wb = new XLWorkbook(file.OpenReadStream());
        var ws = wb.Worksheets.First();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;

        for (var r = row; r <= lastRow; r++)
        {
            string Cell(string field) =>
                cols.TryGetValue(field, out var c) ? ws.Cell(r, c).GetString().Trim() : "";

            try
            {
                var name = Cell("name");
                if (string.IsNullOrWhiteSpace(name)) continue; // boş satır atlanır
                if (!decimal.TryParse(Cell("price"), NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price < 0)
                    throw new Exception("Fiyat geçersiz (örn. 99.99).");
                if (!int.TryParse(Cell("stock"), out var stock)) stock = 0;
                if (stock < 0) throw new Exception("Adet negatif olamaz.");
                var vat = 20m;
                if (cols.ContainsKey("vatRate"))
                {
                    if (!decimal.TryParse(Cell("vatRate"), NumberStyles.Any, CultureInfo.InvariantCulture, out vat) || vat is < 0 or > 100)
                        throw new Exception("KDV 0-100 arasında olmalı.");
                }

                var categoryId = ResolveCategory(Cell("category"), categories);
                if (categoryId is null) throw new Exception("Kategori bulunamadı (adı ya da id yazın).");
                int? brandId = null;
                var brandRaw = Cell("brand");
                if (!string.IsNullOrWhiteSpace(brandRaw))
                {
                    brandId = ResolveBrand(brandRaw, brands);
                    if (brandId is null) throw new Exception($"Marka bulunamadı: {brandRaw}");
                }

                var p = new Product
                {
                    Name = name,
                    Description = Cell("description"),
                    Price = price,
                    ProductCode = Cell("productCode"),
                    Barcode = Cell("barcode"),
                    VatRate = vat,
                    CategoryId = categoryId.Value,
                    BrandId = brandId,
                    Stock = stock
                };
                db.Products.Add(p);
                db.SaveChanges();

                var urls = Cell("imageUrls").Split([',', ';', '\n'],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var url in urls.Distinct())
                    db.ProductImages.Add(new ProductImage { ProductId = p.Id, Url = url });
                db.SaveChanges();
                imported++;
            }
            catch (Exception ex)
            {
                errors.Add(new { row = r, error = ex.Message });
            }
        }

        return Ok(new { imported, skipped = errors.Count, errors });
    }

    private int? ResolveCategory(string raw, Dictionary<string, int> byName)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return db.Categories.OrderBy(c => c.Id).Select(c => c.Id).FirstOrDefault();
        if (int.TryParse(raw, out var id) && db.Categories.Any(c => c.Id == id)) return id;
        return byName.TryGetValue(raw.ToLower(), out var found) ? found : null;
    }

    private int? ResolveBrand(string raw, Dictionary<string, int> byName)
    {
        if (int.TryParse(raw, out var id) && db.Brands.Any(b => b.Id == id)) return id;
        return byName.TryGetValue(raw.ToLower(), out var found) ? found : null;
    }

    private static int ColumnIndex(string letters)
    {
        var s = letters.Trim().ToUpperInvariant();
        if (s.Length == 0 || s.Length > 3 || s.Any(c => c < 'A' || c > 'Z'))
            throw new Exception("bad column");
        var n = 0;
        foreach (var c in s) n = n * 26 + (c - 'A' + 1);
        return n;
    }
}
