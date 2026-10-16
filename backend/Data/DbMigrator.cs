using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

// Eski shop.db'ler EnsureCreated ile oluştuğu için __EFMigrationsHistory tabloları
// yok. İlk Migrate() çağrısında tarihsiz DB'yi patlatmamak için:
// - Tarihsiz ama tabloları olan DB (legacy) -> idempotent EnsureNewSchema ile
//   güncel şemaya getirilir, mevcut migration'lar history'ye "uygulandı" diye
//   damgalanır, sonra Migrate() no-op olur. Veri kaybı olmaz.
// - Tablosu olmayan DB (sıfır) -> EnsureNewSchema çalışmaz, Migrate() Baseline
//   + sonraki migration'ları gerçekten uygular.
// - History'si olan DB -> normal Migrate().
public static class DbMigrator
{
    private const string HistoryTable = "__EFMigrationsHistory";

    public static void EnsureMigrated(AppDbContext db)
    {
        if (IsLegacyDatabase(db))
        {
            // Mevcut raw-SQL yükseltme korunur (baseline hazır olana kadar kaldırma):
            // eksik tablo/kolonları IF NOT EXISTS / kolon kontrolüyle ekler.
            ShopDbSeeder.EnsureNewSchema(db);
            ShopDbSeeder.MigrateProductImages(db);
            StampAppliedMigrations(db);
        }
        db.Database.Migrate();
    }

    private static bool IsLegacyDatabase(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name IN ('Products', '__EFMigrationsHistory')";
        var found = new HashSet<string>(StringComparer.Ordinal);
        using (var reader = cmd.ExecuteReader())
            while (reader.Read()) found.Add(reader.GetString(0));
        return found.Contains("Products") && !found.Contains(HistoryTable);
    }

    private static void StampAppliedMigrations(AppDbContext db)
    {
        var applied = db.Database.GetMigrations().ToList();
        db.Database.ExecuteSqlRaw($"""
            CREATE TABLE IF NOT EXISTS "{HistoryTable}" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL)
            """);
        foreach (var id in applied)
        {
            db.Database.ExecuteSqlRaw(
                "INSERT OR IGNORE INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1})",
                id, "10.0.0");
        }
    }
}
