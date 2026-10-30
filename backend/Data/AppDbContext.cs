using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ImportTemplate> ImportTemplates => Set<ImportTemplate>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewImage> ReviewImages => Set<ReviewImage>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>().Property(u => u.EmailConfirmed).HasDefaultValue(true);
        modelBuilder.Entity<Favorite>().HasKey(f => new { f.UserId, f.ProductId });
        modelBuilder.Entity<Setting>().HasKey(s => s.Key);
        modelBuilder.Entity<ProductImage>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Cart>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .HasPrincipalKey(u => u.Id)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CartItem>()
            .HasOne<Cart>()
            .WithMany()
            .HasForeignKey(i => i.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        // Yorumlar: navigation'lar tek yönlü (çift yönlü navigation + JSON circular
        // reference riskine karşı API'den asla entity dönülmez, hep DTO projeksiyonu).
        modelBuilder.Entity<Review>(e =>
        {
            e.HasIndex(r => r.ProductId);
            e.HasIndex(r => new { r.UserId, r.ProductId }).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_Reviews_Rating", "\"Rating\" BETWEEN 1 AND 5"));
            e.Property(r => r.Title).HasMaxLength(100);
            e.Property(r => r.Comment).HasMaxLength(2000);
            e.Property(r => r.Status).HasConversion<string>().HasMaxLength(16);
            e.HasOne<Product>()
                .WithMany()
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ReviewImage>(e =>
        {
            e.HasIndex(i => i.ReviewId);
            e.HasOne<Review>()
                .WithMany()
                .HasForeignKey(i => i.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PasswordResetToken>(e =>
        {
            e.HasIndex(t => t.UserId);
            e.Property(t => t.Purpose).HasDefaultValue("password_reset");
        });
        modelBuilder.Entity<OrderStatusHistory>(e =>
        {
            e.HasIndex(h => h.OrderId);
            e.HasOne<Order>()
                .WithMany()
                .HasForeignKey(h => h.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // Ürünün ilk resmi (vitrin). Yoksa boş string.
    public string PrimaryImageUrl(int productId) =>
        ProductImages.Where(i => i.ProductId == productId)
            .OrderBy(i => i.Id).Select(i => i.Url).FirstOrDefault() ?? "";
}
