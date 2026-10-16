using System.Data;
using System.Text.Json;
using Backend.Auth;
using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;
#pragma warning disable EF1002 // Seeder SQL'leri sabit kolon adları içerir, kullanıcı girdisi yok

public static class ShopDbSeeder
{
    public static void Seed(AppDbContext db)
    {
        // Şema DbMigrator.EnsureMigrated ile hazırlanır (Migrate + legacy damga).
        // Bu metot sadece veri seedler.

        if (!db.Categories.Any())
        {
            db.Categories.AddRange(SeedData.Categories.Select(c => new Category
            {
                Name = c.Name,
                Slug = c.Slug
            }));
            db.SaveChanges();
        }

        if (!db.Products.Any())
        {
            var catIds = db.Categories.ToDictionary(c => c.Slug, c => c.Id);
            var slugByOldId = SeedData.Categories.ToDictionary(c => c.Id, c => c.Slug);
            foreach (var p in SeedData.Products)
            {
                var entity = new Product
                {
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    ProductCode = p.ProductCode,
                    Barcode = p.Barcode,
                    VatRate = p.VatRate,
                    CategoryId = catIds[slugByOldId[p.CategoryId]],
                    Stock = p.Stock
                };
                db.Products.Add(entity);
                db.SaveChanges();
                AddImages(db, entity.Id, p.Name);
            }
        }

        if (!db.Users.Any(u => u.Email == "admin@shop.com"))
        {
            var (hash, salt) = PasswordHasher.Hash("Admin123!");
            db.Users.Add(new User
            {
                Email = "admin@shop.com",
                PasswordHash = hash,
                PasswordSalt = salt,
                Name = "Admin",
                Role = "admin"
            });
            db.SaveChanges();
        }

        if (!db.Brands.Any())
        {
            foreach (var name in new[] { "Genel", "TeknoPlus", "Evora", "SportX" })
                db.Brands.Add(new Brand { Name = name, Slug = Slugify(name) });
            db.SaveChanges();
        }

        var defaults = new Dictionary<string, string>
        {
            ["storeName"] = "ihtiyacın3D",
            ["supportEmail"] = "destek@ihtiyacin3d.com",
            ["supportPhone"] = "0850 000 00 00",
            ["storeAddress"] = "İstanbul, Türkiye",
            ["currency"] = "₺",
            ["vatRate"] = "20",
            ["freeShippingLimit"] = "1500",
            ["shippingFee"] = "49.99",
            ["announcement"] = "",
            ["maintenanceMode"] = "false",
        };
        var existing = db.Settings.Select(s => s.Key).ToHashSet();
        foreach (var kv in defaults.Where(kv => !existing.Contains(kv.Key)))
            db.Settings.Add(new Setting { Key = kv.Key, Value = kv.Value });
        db.SaveChanges();

        SeedDemoCustomerAndOrders(db);
    }

    private static void AddImages(AppDbContext db, int productId, string productName)
    {
        if (!SeedData.ProductImageUrls.TryGetValue(productName, out var urls)) return;
        foreach (var url in urls.Where(u => !string.IsNullOrWhiteSpace(u)))
            db.ProductImages.Add(new ProductImage { ProductId = productId, Url = url });
        db.SaveChanges();
    }

    // Test müşterisi + o müşterinin vermiş gibi göründüğü örnek ürün/siparişler.
    // Sadece sipariş tablosu boşken çalışır, tekrar çalışmalarda kopya oluşturmaz.
    private static void SeedDemoCustomerAndOrders(AppDbContext db)
    {
        const string demoEmail = "test@musteri.com";
        var demo = db.Users.FirstOrDefault(u => u.Email == demoEmail);
        if (demo is null)
        {
            var (hash, salt) = PasswordHasher.Hash("Test123!");
            demo = new User
            {
                Email = demoEmail,
                PasswordHash = hash,
                PasswordSalt = salt,
                Name = "Test Müşteri",
                Phone = "0532 000 00 00",
                Role = "customer"
            };
            db.Users.Add(demo);
            db.SaveChanges();
        }

        if (db.Orders.Any()) return;

        var cats = db.Categories.ToDictionary(c => c.Slug, c => c.Id);
        var brands = db.Brands.ToDictionary(b => b.Slug, b => b.Id);

        AddProductIfMissing(db, "Bluetooth Hoparlör",
            "360° ses, 20 saat pil, IPX7 su geçirmez.", 1499.00m, "BLT-001", "869000000010", 20,
            cats.GetValueOrDefault("elektronik"), brands.GetValueOrDefault("teknoplus"), 12);
        AddProductIfMissing(db, "Eşofman Takımı",
            "Pamuk-polyester karışımı, 3 renk seçeneği.", 899.99m, "GYM-003", "869000000011", 10,
            cats.GetValueOrDefault("giyim"), brands.GetValueOrDefault("sportx"), 45);

        if (!db.Addresses.Any(a => a.UserId == demo.Id))
        {
            db.Addresses.Add(new Address
            {
                UserId = demo.Id,
                Title = "Ev",
                FullName = "Test Müşteri",
                Phone = "0532 000 00 00",
                City = "İstanbul",
                District = "Kadıköy",
                Detail = "Örnek Mah. Demo Sok. No:1 D:2"
            });
            db.SaveChanges();
        }
        var addr = db.Addresses.First(a => a.UserId == demo.Id);

        CreateOrder(db, demo, addr,
            [("Kablosuz Kulaklık", 1), ("Pamuk T-Shirt", 2)],
            OrderStatuses.Delivered, DateTime.UtcNow.AddDays(-6));
        CreateOrder(db, demo, addr,
            [("Koşu Ayakkabısı", 1), ("Bluetooth Hoparlör", 1)],
            OrderStatuses.Shipping, DateTime.UtcNow.AddDays(-1));
    }

    private static void AddProductIfMissing(AppDbContext db, string name, string desc,
        decimal price, string code, string barcode, decimal vat,
        int categoryId, int brandId, int stock)
    {
        if (db.Products.Any(p => p.Name == name)) return;
        var p = new Product
        {
            Name = name,
            Description = desc,
            Price = price,
            ProductCode = code,
            Barcode = barcode,
            VatRate = vat,
            CategoryId = categoryId == 0 ? db.Categories.OrderBy(c => c.Id).Select(c => c.Id).First() : categoryId,
            BrandId = brandId == 0 ? null : brandId,
            Stock = stock
        };
        db.Products.Add(p);
        db.SaveChanges();
        AddImages(db, p.Id, name);
    }

    private static void CreateOrder(AppDbContext db, User user, Address addr,
        (string name, int qty)[] lines, string status, DateTime createdAt)
    {
        var items = new List<(Product p, int qty)>();
        foreach (var (name, qty) in lines)
        {
            var p = db.Products.FirstOrDefault(x => x.Name == name);
            if (p is null || p.Stock < qty) return;
            items.Add((p, qty));
        }

        var subtotal = items.Sum(x => x.p.Price * x.qty);
        var limit = GetSettingDecimal(db, "freeShippingLimit", 1500);
        var shipping = subtotal >= limit ? 0 : GetSettingDecimal(db, "shippingFee", 49.99m);

        var order = new Order
        {
            UserId = user.Id,
            CustomerName = user.Name,
            CustomerEmail = user.Email,
            CustomerPhone = user.Phone,
            AddressTitle = addr.Title,
            AddressFullName = addr.FullName,
            AddressPhone = addr.Phone,
            AddressCity = addr.City,
            AddressDistrict = addr.District,
            AddressDetail = addr.Detail,
            ItemsCount = items.Sum(x => x.qty),
            Subtotal = subtotal,
            Shipping = shipping,
            Total = subtotal + shipping,
            Status = status,
            CreatedAt = createdAt,
        };
        db.Orders.Add(order);
        db.SaveChanges();

        order.OrderNo = $"SP-{order.CreatedAt.Year}-{order.Id:0000}";
        foreach (var (p, qty) in items)
        {
            p.Stock -= qty;
            db.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = p.Id,
                ProductName = p.Name,
                Price = p.Price,
                Qty = qty,
                ImageUrl = db.PrimaryImageUrl(p.Id)
            });
        }
        db.SaveChanges();
    }

    private static decimal GetSettingDecimal(AppDbContext db, string key, decimal fallback)
    {
        var v = db.Settings.Where(s => s.Key == key).Select(s => s.Value).FirstOrDefault();
        return v is not null && decimal.TryParse(v,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : fallback;
    }

    // Mevcut shop.db dosyalarını yeni tablolara yükseltir (veri kaybı olmaz).
    // DbMigrator legacy yolu için korunur; IF NOT EXISTS / kolon kontrolüyle idempotent.
    internal static void EnsureNewSchema(AppDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "Categories" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Categories" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL,
                "Slug" TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS "Products" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Products" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL,
                "Description" TEXT NOT NULL,
                "Price" TEXT NOT NULL,
                "ProductCode" TEXT NOT NULL DEFAULT '',
                "Barcode" TEXT NOT NULL DEFAULT '',
                "VatRate" TEXT NOT NULL DEFAULT '20',
                "CategoryId" INTEGER NOT NULL,
                "BrandId" INTEGER NULL,
                "Stock" INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS "Users" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY,
                "Email" TEXT NOT NULL,
                "PasswordHash" TEXT NOT NULL,
                "PasswordSalt" TEXT NOT NULL,
                "Name" TEXT NOT NULL,
                "Phone" TEXT NOT NULL,
                "Role" TEXT NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email");
            CREATE TABLE IF NOT EXISTS "Addresses" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Addresses" PRIMARY KEY AUTOINCREMENT,
                "UserId" TEXT NOT NULL,
                "Title" TEXT NOT NULL,
                "FullName" TEXT NOT NULL,
                "Phone" TEXT NOT NULL,
                "City" TEXT NOT NULL,
                "District" TEXT NOT NULL,
                "Detail" TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS "Favorites" (
                "UserId" TEXT NOT NULL,
                "ProductId" INTEGER NOT NULL,
                CONSTRAINT "PK_Favorites" PRIMARY KEY ("UserId", "ProductId"));
            CREATE TABLE IF NOT EXISTS "Brands" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Brands" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL,
                "Slug" TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS "Orders" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Orders" PRIMARY KEY AUTOINCREMENT,
                "OrderNo" TEXT NOT NULL,
                "UserId" TEXT NOT NULL,
                "CustomerName" TEXT NOT NULL,
                "CustomerEmail" TEXT NOT NULL,
                "CustomerPhone" TEXT NOT NULL,
                "AddressTitle" TEXT NOT NULL,
                "AddressFullName" TEXT NOT NULL,
                "AddressPhone" TEXT NOT NULL,
                "AddressCity" TEXT NOT NULL,
                "AddressDistrict" TEXT NOT NULL,
                "AddressDetail" TEXT NOT NULL,
                "ItemsCount" INTEGER NOT NULL,
                "Subtotal" TEXT NOT NULL,
                "Shipping" TEXT NOT NULL,
                "Total" TEXT NOT NULL,
                "Status" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS "OrderItems" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_OrderItems" PRIMARY KEY AUTOINCREMENT,
                "OrderId" INTEGER NOT NULL,
                "ProductId" INTEGER NOT NULL,
                "ProductName" TEXT NOT NULL,
                "Price" TEXT NOT NULL,
                "Qty" INTEGER NOT NULL,
                "ImageUrl" TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS "Settings" (
                "Key" TEXT NOT NULL CONSTRAINT "PK_Settings" PRIMARY KEY,
                "Value" TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS "ProductImages" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ProductImages" PRIMARY KEY AUTOINCREMENT,
                "ProductId" INTEGER NOT NULL,
                "Url" TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS "ImportTemplates" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ImportTemplates" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL,
                "MappingsJson" TEXT NOT NULL,
                "StartRow" INTEGER NOT NULL,
                "MatchBy" TEXT NOT NULL DEFAULT 'productCode',
                "FileName" TEXT NOT NULL DEFAULT '',
                "StoredFile" TEXT NOT NULL DEFAULT '',
                "CreatedAt" TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS "Carts" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Carts" PRIMARY KEY AUTOINCREMENT,
                "UserId" TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS "CartItems" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_CartItems" PRIMARY KEY AUTOINCREMENT,
                "CartId" INTEGER NOT NULL,
                "ProductId" INTEGER NOT NULL,
                "Qty" INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS "Reviews" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Reviews" PRIMARY KEY AUTOINCREMENT,
                "ProductId" INTEGER NOT NULL,
                "UserId" TEXT NOT NULL,
                "OrderId" INTEGER NOT NULL,
                "OrderItemId" INTEGER NULL,
                "Rating" INTEGER NOT NULL,
                "Title" TEXT NULL,
                "Comment" TEXT NULL,
                "Status" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "UpdatedAt" TEXT NOT NULL,
                CONSTRAINT "CK_Reviews_Rating" CHECK ("Rating" BETWEEN 1 AND 5));
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Reviews_UserId_ProductId" ON "Reviews" ("UserId", "ProductId");
            CREATE INDEX IF NOT EXISTS "IX_Reviews_ProductId" ON "Reviews" ("ProductId");
            CREATE TABLE IF NOT EXISTS "ReviewImages" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ReviewImages" PRIMARY KEY AUTOINCREMENT,
                "ReviewId" INTEGER NOT NULL,
                "Url" TEXT NOT NULL,
                "SortOrder" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                CONSTRAINT "FK_ReviewImages_Reviews_ReviewId" FOREIGN KEY ("ReviewId") REFERENCES "Reviews" ("Id") ON DELETE CASCADE);
            CREATE INDEX IF NOT EXISTS "IX_ReviewImages_ReviewId" ON "ReviewImages" ("ReviewId");
            """);

        foreach (var col in new[] { ("BrandId", "INTEGER NULL"), ("ProductCode", "TEXT NOT NULL DEFAULT ''"), ("Barcode", "TEXT NOT NULL DEFAULT ''"), ("VatRate", "TEXT NOT NULL DEFAULT '20'"), ("AverageRating", "REAL NOT NULL DEFAULT 0"), ("ReviewCount", "INTEGER NOT NULL DEFAULT 0") })
        {
            if (!TableColumns(db, "Products").Contains(col.Item1))
                db.Database.ExecuteSqlRaw($"""ALTER TABLE "Products" ADD COLUMN "{col.Item1}" {col.Item2}""");
        }
        if (!TableColumns(db, "ImportTemplates").Contains("MatchBy"))
            db.Database.ExecuteSqlRaw("""ALTER TABLE "ImportTemplates" ADD COLUMN "MatchBy" TEXT NOT NULL DEFAULT 'productCode'""");
        if (!TableColumns(db, "ImportTemplates").Contains("FileName"))
            db.Database.ExecuteSqlRaw("""ALTER TABLE "ImportTemplates" ADD COLUMN "FileName" TEXT NOT NULL DEFAULT ''""");
        if (!TableColumns(db, "ImportTemplates").Contains("StoredFile"))
            db.Database.ExecuteSqlRaw("""ALTER TABLE "ImportTemplates" ADD COLUMN "StoredFile" TEXT NOT NULL DEFAULT ''""");
    }

    // Eski Products.ImageUrl/Images kolonlarındaki resimleri ProductImages tablosuna taşır.
    internal static void MigrateProductImages(AppDbContext db)
    {
        var cols = TableColumns(db, "Products");
        var hasUrl = cols.Contains("ImageUrl");
        var hasImages = cols.Contains("Images");
        if (!hasUrl && !hasImages) return;

        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        var select = "\"Id\"";
        if (hasUrl) select += ", \"ImageUrl\"";
        if (hasImages) select += ", \"Images\"";
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT {select} FROM \"Products\"";
        var rows = new List<(int id, string imageUrl, string images)>();
        using (var reader = cmd.ExecuteReader())
            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var i = 1;
                var imageUrl = hasUrl ? reader.GetString(i++) : "";
                var images = hasImages ? reader.GetString(i) : "[]";
                rows.Add((id, imageUrl, images));
            }

        foreach (var (id, imageUrl, imagesJson) in rows)
        {
            var urls = new List<string>();
            if (!string.IsNullOrWhiteSpace(imageUrl)) urls.Add(imageUrl);
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(imagesJson);
                if (parsed is not null) urls.AddRange(parsed.Where(u => !string.IsNullOrWhiteSpace(u)));
            }
            catch { /* bozuk JSON atlanır */ }
            foreach (var url in urls.Distinct())
                db.ProductImages.Add(new ProductImage { ProductId = id, Url = url });
        }
        db.SaveChanges();

        foreach (var col in new[] { "ImageUrl", "Images" })
        {
            if (!TableColumns(db, "Products").Contains(col)) continue;
            try { db.Database.ExecuteSqlRaw($"ALTER TABLE \"Products\" DROP COLUMN \"{col}\""); }
            catch { /* eski SQLite sürümlerinde kalabilir, EF yine de çalışır */ }
        }
    }

    private static List<string> TableColumns(AppDbContext db, string table)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info('{table}')";
        var cols = new List<string>();
        using (var reader = cmd.ExecuteReader())
            while (reader.Read()) cols.Add(reader.GetString(1));
        return cols;
    }

    public static string Slugify(string name)
    {
        var s = name.Trim().ToLowerInvariant()
            .Replace('ç', 'c').Replace('ğ', 'g').Replace('ı', 'i')
            .Replace('ö', 'o').Replace('ş', 's').Replace('ü', 'u');
        s = string.Concat(s.Select(c => char.IsLetterOrDigit(c) ? c : '-'));
        while (s.Contains("--")) s = s.Replace("--", "-");
        return s.Trim('-');
    }
}
