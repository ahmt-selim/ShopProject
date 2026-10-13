using Backend.Models;

namespace Backend.Data;

public static class SeedData
{
    public static readonly List<Category> Categories =
    [
        new() { Id = 1, Name = "Elektronik", Slug = "elektronik" },
        new() { Id = 2, Name = "Giyim", Slug = "giyim" },
        new() { Id = 3, Name = "Ev & Yaşam", Slug = "ev-yasam" },
        new() { Id = 4, Name = "Spor", Slug = "spor" },
    ];

    public static readonly List<Product> Products =
    [
        new() { Id = 1, Name = "Kablosuz Kulaklık", Description = "Aktif gürültü engelleme, 30 saat pil, Bluetooth 5.3.", Price = 2499.99m, ProductCode = "ELK-001", Barcode = "869000000001", VatRate = 20, CategoryId = 1, Stock = 25 },
        new() { Id = 2, Name = "Mekanik Klavye", Description = "RGB aydınlatmalı, hot-swap switch, Türkçe Q dizilim.", Price = 1899.50m, ProductCode = "ELK-002", Barcode = "869000000002", VatRate = 20, CategoryId = 1, Stock = 40 },
        new() { Id = 3, Name = "Akıllı Saat", Description = "Nabız + uyku takibi, 10 gün pil, 5ATM su geçirmez.", Price = 3299.00m, ProductCode = "ELK-003", Barcode = "869000000003", VatRate = 20, CategoryId = 1, Stock = 15 },
        new() { Id = 4, Name = "Pamuk T-Shirt", Description = "%100 pamuk, regular fit, 4 renk seçeneği.", Price = 349.99m, ProductCode = "GYM-001", Barcode = "869000000004", VatRate = 10, CategoryId = 2, Stock = 100 },
        new() { Id = 5, Name = "Denim Ceket", Description = "Hafif yıkamalı denim, unisex kesim.", Price = 1299.00m, ProductCode = "GYM-002", Barcode = "869000000005", VatRate = 10, CategoryId = 2, Stock = 30 },
        new() { Id = 6, Name = "Seramik Vazo Seti", Description = "El yapımı 3'lü vazo seti, mat beyaz.", Price = 749.00m, ProductCode = "EVY-001", Barcode = "869000000006", VatRate = 20, CategoryId = 3, Stock = 20 },
        new() { Id = 7, Name = "LED Masa Lambası", Description = "3 renk sıcaklığı, dokunmatik kısma, USB-C.", Price = 599.99m, ProductCode = "EVY-002", Barcode = "869000000007", VatRate = 20, CategoryId = 3, Stock = 50 },
        new() { Id = 8, Name = "Koşu Ayakkabısı", Description = "Nefes alabilir file, EVA taban, 42-45 numara.", Price = 2199.00m, ProductCode = "SPR-001", Barcode = "869000000008", VatRate = 10, CategoryId = 4, Stock = 35 },
        new() { Id = 9, Name = "Yoga Matı", Description = "6mm kalınlık, kaymaz yüzey, taşıma askılı.", Price = 499.50m, ProductCode = "SPR-002", Barcode = "869000000009", VatRate = 10, CategoryId = 4, Stock = 60 },
    ];

    // Ürün adı -> seed resim URL'leri (ProductImages tablosuna yazılır)
    public static readonly Dictionary<string, string[]> ProductImageUrls = new()
    {
        ["Kablosuz Kulaklık"] = ["https://picsum.photos/seed/headphone/600/400"],
        ["Mekanik Klavye"] = ["https://picsum.photos/seed/keyboard/600/400"],
        ["Akıllı Saat"] = ["https://picsum.photos/seed/watch/600/400"],
        ["Pamuk T-Shirt"] = ["https://picsum.photos/seed/tshirt/600/400"],
        ["Denim Ceket"] = ["https://picsum.photos/seed/jacket/600/400"],
        ["Seramik Vazo Seti"] = ["https://picsum.photos/seed/vase/600/400"],
        ["LED Masa Lambası"] = ["https://picsum.photos/seed/lamp/600/400"],
        ["Koşu Ayakkabısı"] = ["https://picsum.photos/seed/shoes/600/400"],
        ["Yoga Matı"] = ["https://picsum.photos/seed/yoga/600/400"],
        ["Bluetooth Hoparlör"] = ["https://picsum.photos/seed/speaker/600/400"],
        ["Eşofman Takımı"] = ["https://picsum.photos/seed/tracksuit/600/400"],
    };
}
