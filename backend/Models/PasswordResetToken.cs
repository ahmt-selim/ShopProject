namespace Backend.Models;

// Şifre sıfırlama bağlantılarındaki token'ın kendisi ASLA saklanmaz;
// yalnızca SHA-256 hash'i (TokenHash) tutulur. E-postadaki düz token
// tek kullanımlıktır ve 60 dakika geçerlidir.
// Geçersiz kılınan (yeni istek / başarılı reset) kayıtlar UsedAt ile işaretlenir.
public class PasswordResetToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    // Düz kod ASLA saklanmaz; yalnızca SHA-256 hash'i (TokenHash) tutulur.
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    // Yanlış kod denemeleri; sınıra ulaşan kayıt geçersiz kılınır.
    public int FailedAttempts { get; set; }
}
