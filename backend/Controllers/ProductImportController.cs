using System.Globalization;
using System.Text.Json;
using Backend.Data;
using Backend.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

// Excel'den toplu ürün aktarımı. Sütun harfi (A, B, ... AA) -> ürün alanı eşleşmesiyle çalışır.
// matchBy (productId | productCode | barcode) alanıyla eşleşen ürün güncellenir, yoksa eklenir.
[Authorize(Roles = "admin")]
[ApiController]
[Route("api/products")]
public class ProductImportController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    private static readonly string[] KnownFields =
        ["productId", "name", "description", "price", "productCode", "barcode", "vatRate",
         "category", "brand", "stock", "imageUrls"];

    [HttpPost("import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<object>> Import(
        IFormFile? file,
        [FromForm] int? templateId,
        [FromForm] string? mappings,
        [FromForm] int? startRow,
        [FromForm] string? matchBy)
    {
        ImportTemplate? template = null;
        if (templateId.HasValue)
        {
            template = db.ImportTemplates.Find(templateId.Value);
            if (template is null) return NotFound(new { error = "Şablon bulunamadı." });
        }

        // Dosya kaynağı: yeni yüklenen dosya öncelikli, yoksa şablonun kayıtlı kopyası kullanılır.
        byte[] bytes;
        if (file is not null && file.Length > 0)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not ".xlsx" and not ".xls")
                return BadRequest(new { error = "Sadece .xlsx / .xls yüklenebilir." });
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            bytes = ms.ToArray();
            if (template is not null)
            {
                template.StoredFile = SaveTemplateFile(template.Id, ext, bytes);
                template.FileName = Path.GetFileName(file.FileName);
                db.SaveChanges();
            }
        }
        else if (template is not null && !string.IsNullOrEmpty(template.StoredFile))
        {
            var path = TemplateFilePath(template.StoredFile);
            if (!System.IO.File.Exists(path))
                return BadRequest(new { error = "Kayıtlı dosya bulunamadı, lütfen dosya seçin." });
            bytes = await System.IO.File.ReadAllBytesAsync(path);
        }
        else
        {
            return BadRequest(new { error = "Excel dosyası seçin." });
        }

        Dictionary<string, string> map;
        var row = startRow ?? 2;
        var match = NormalizeMatchBy(matchBy);
        var matchFromForm = !string.IsNullOrWhiteSpace(matchBy);
        if (template is not null)
        {
            map = JsonSerializer.Deserialize<Dictionary<string, string>>(template.MappingsJson) ?? new();
            if (!startRow.HasValue) row = template.StartRow;
            if (!matchFromForm) match = NormalizeMatchBy(template.MatchBy);
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
        var imageCols = new List<int>();
        try
        {
            cols = map.Where(kv => kv.Key != "imageUrls" && KnownFields.Contains(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
                .ToDictionary(kv => kv.Key, kv => ColumnIndex(kv.Value));
            if (map.TryGetValue("imageUrls", out var imgRaw) && !string.IsNullOrWhiteSpace(imgRaw))
                foreach (var part in imgRaw.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries))
                    imageCols.Add(ColumnIndex(part));
        }
        catch
        {
            return BadRequest(new { error = "Sütun harfleri A-Z formatında olmalı (örn. A, B, AA; resimler için J,K yazılabilir)." });
        }
        if (!cols.TryGetValue(match, out _))
            return BadRequest(new { error = $"Eşleştirme alanı ({MatchLabel(match)}) için sütun harfi girin." });

        var inserted = 0;
        var updated = 0;
        var errors = new List<object>();
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheets.First();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;

        for (var r = row; r <= lastRow; r++)
        {
            string Cell(string field)
            {
                if (field == "imageUrls")
                    return string.Join(",", imageCols.Select(c => ws.Cell(r, c).GetString().Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s)));
                return cols.TryGetValue(field, out var c) ? ws.Cell(r, c).GetString().Trim() : "";
            }

            try
            {
                var matchRaw = Cell(match);
                if (string.IsNullOrWhiteSpace(matchRaw)) continue; // eşleşme değeri boş satır atlanır

                var existing = FindProduct(match, matchRaw);
                if (existing is null)
                {
                    var name = Cell("name");
                    var priceRaw = Cell("price");
                    var price = 0m;
                    if (!string.IsNullOrWhiteSpace(priceRaw))
                    {
                        if (!decimal.TryParse(priceRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out price) || price < 0)
                            throw new Exception("Fiyat geçersiz (örn. 99.99).");
                    }
                    var stock = 0;
                    var stockRaw = Cell("stock");
                    if (!string.IsNullOrWhiteSpace(stockRaw))
                    {
                        if (!int.TryParse(stockRaw, out stock)) stock = 0;
                        if (stock < 0) throw new Exception("Adet negatif olamaz.");
                    }
                    var vat = 20m;
                    var vatRaw = Cell("vatRate");
                    if (!string.IsNullOrWhiteSpace(vatRaw))
                    {
                        if (!decimal.TryParse(vatRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out vat) || vat is < 0 or > 100)
                            throw new Exception("KDV 0-100 arasında olmalı.");
                    }

                    var categoryId = ResolveOrCreateCategory(Cell("category"));
                    var brandId = ResolveOrCreateBrand(Cell("brand"));

                    var p = new Product
                    {
                        Name = name,
                        Description = Cell("description"),
                        Price = price,
                        ProductCode = match == "productCode" ? matchRaw : Cell("productCode"),
                        Barcode = match == "barcode" ? matchRaw : Cell("barcode"),
                        VatRate = vat,
                        CategoryId = categoryId,
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
                    inserted++;
                }
                else
                {
                    var name = Cell("name");
                    if (cols.ContainsKey("name") && !string.IsNullOrWhiteSpace(name))
                        existing.Name = name;
                    var desc = Cell("description");
                    if (cols.ContainsKey("description") && !string.IsNullOrWhiteSpace(desc))
                        existing.Description = desc;
                    var priceRaw = Cell("price");
                    if (cols.ContainsKey("price") && !string.IsNullOrWhiteSpace(priceRaw))
                    {
                        if (!decimal.TryParse(priceRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price < 0)
                            throw new Exception("Fiyat geçersiz (örn. 99.99).");
                        existing.Price = price;
                    }
                    if (cols.ContainsKey("productCode") && match != "productCode")
                        existing.ProductCode = Cell("productCode");
                    if (cols.ContainsKey("barcode") && match != "barcode")
                        existing.Barcode = Cell("barcode");
                    var vatRaw = Cell("vatRate");
                    if (cols.ContainsKey("vatRate") && !string.IsNullOrWhiteSpace(vatRaw))
                    {
                        if (!decimal.TryParse(vatRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var vat) || vat is < 0 or > 100)
                            throw new Exception("KDV 0-100 arasında olmalı.");
                        existing.VatRate = vat;
                    }
                    var stockRaw = Cell("stock");
                    if (cols.ContainsKey("stock") && !string.IsNullOrWhiteSpace(stockRaw))
                    {
                        if (!int.TryParse(stockRaw, out var stock)) stock = existing.Stock;
                        if (stock < 0) throw new Exception("Adet negatif olamaz.");
                        existing.Stock = stock;
                    }
                    var catRaw = Cell("category");
                    if (cols.ContainsKey("category") && !string.IsNullOrWhiteSpace(catRaw))
                        existing.CategoryId = ResolveOrCreateCategory(catRaw);
                    var brandRaw = Cell("brand");
                    if (cols.ContainsKey("brand"))
                    {
                        if (string.IsNullOrWhiteSpace(brandRaw))
                        {
                            // boş bırakıldıysa marka değiştirilmez
                        }
                        else
                        {
                            existing.BrandId = ResolveOrCreateBrand(brandRaw);
                        }
                    }
                    var urlsRaw = Cell("imageUrls");
                    if (imageCols.Count > 0 && !string.IsNullOrWhiteSpace(urlsRaw))
                    {
                        var urls = urlsRaw.Split([',', ';', '\n'],
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Distinct().ToList();
                        db.ProductImages.RemoveRange(db.ProductImages.Where(i => i.ProductId == existing.Id));
                        foreach (var url in urls)
                            db.ProductImages.Add(new ProductImage { ProductId = existing.Id, Url = url });
                    }
                    db.SaveChanges();
                    updated++;
                }
            }
            catch (Exception ex)
            {
                errors.Add(new { row = r, error = ex.Message });
            }
        }

        return Ok(new { imported = inserted, updated, skipped = errors.Count, errors });
    }

    private Product? FindProduct(string match, string raw)
    {
        return match switch
        {
            "productId" => int.TryParse(raw, out var id) ? db.Products.Find(id) : null,
            "barcode" => db.Products.FirstOrDefault(p => p.Barcode == raw),
            _ => db.Products.FirstOrDefault(p => p.ProductCode == raw),
        };
    }

    private static string NormalizeMatchBy(string? v) =>
        v?.Trim() switch
        {
            "productId" => "productId",
            "barcode" => "barcode",
            _ => "productCode",
        };

    private static string MatchLabel(string match) => match switch
    {
        "productId" => "Ürün İd",
        "barcode" => "Barkod",
        _ => "Ürün Kodu",
    };

    // Excel'deki kategori adı slug'lanıp mevcutlarla karşılaştırılır; yoksa yeni kategori eklenir.
    private int ResolveOrCreateCategory(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return db.Categories.OrderBy(c => c.Id).Select(c => c.Id).FirstOrDefault();
        var name = raw.Trim();
        if (int.TryParse(name, out var id) && db.Categories.Any(c => c.Id == id)) return id;
        var slug = ShopDbSeeder.Slugify(name);
        var existing = db.Categories.FirstOrDefault(c => c.Slug == slug);
        if (existing is not null) return existing.Id;
        var c = new Category { Name = name, Slug = UniqueCategorySlug(slug) };
        db.Categories.Add(c);
        db.SaveChanges();
        return c.Id;
    }

    // Excel'deki marka adı slug'lanıp mevcutlarla karşılaştırılır; yoksa yeni marka eklenir.
    private int? ResolveOrCreateBrand(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var name = raw.Trim();
        if (int.TryParse(name, out var id) && db.Brands.Any(b => b.Id == id)) return id;
        var slug = ShopDbSeeder.Slugify(name);
        var existing = db.Brands.FirstOrDefault(b => b.Slug == slug);
        if (existing is not null) return existing.Id;
        var b = new Brand { Name = name, Slug = UniqueBrandSlug(slug) };
        db.Brands.Add(b);
        db.SaveChanges();
        return b.Id;
    }

    private string UniqueCategorySlug(string baseSlug)
    {
        var slug = baseSlug;
        var i = 2;
        while (db.Categories.Any(c => c.Slug == slug))
            slug = $"{baseSlug}-{i++}";
        return slug;
    }

    private string UniqueBrandSlug(string baseSlug)
    {
        var slug = baseSlug;
        var i = 2;
        while (db.Brands.Any(b => b.Slug == slug))
            slug = $"{baseSlug}-{i++}";
        return slug;
    }

    private string SaveTemplateFile(int templateId, string ext, byte[] bytes)
    {
        var dir = Path.Combine(env.ContentRootPath, "App_Data", "imports");
        Directory.CreateDirectory(dir);
        foreach (var old in Directory.GetFiles(dir, $"template_{templateId}.*"))
            System.IO.File.Delete(old);
        var name = $"template_{templateId}{ext}";
        System.IO.File.WriteAllBytes(Path.Combine(dir, name), bytes);
        return name;
    }

    private string TemplateFilePath(string storedFile) =>
        Path.Combine(env.ContentRootPath, "App_Data", "imports", storedFile);

    private static int ColumnIndex(string letters)
    {
        var s = letters.Trim().ToUpperInvariant();
        if (s.Length == 0 || s.Any(c => c < 'A' || c > 'Z'))
            throw new Exception("bad column");
        var n = 0;
        foreach (var c in s) n = checked(n * 26 + (c - 'A' + 1));
        if (n < 1 || n > 16384) // Excel sütun aralığı: A-XFD
            throw new Exception("bad column");
        return n;
    }
}
