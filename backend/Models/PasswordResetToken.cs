namespace Backend.Models;

// Sifre sifirlama ve e-posta dogrulama kodlari SHA-256 hash'i (TokenHash) olarak
// saklanir. Duz kod asla saklanmaz. Tek kullanimliktir.
// Purpose alani iki akisin birbirinin yerine kullanilmasini engeller.
public class PasswordResetToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    // Kod amaci: "password_reset" veya "email_verification".
    public string Purpose { get; set; } = "password_reset";
    // Duz kod ASLA saklanmaz; yalnizca SHA-256 hash'i (TokenHash) tutulur.
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    // Yanlis kod denemeleri; sinira ulasan kayit gecersiz kilinir.
    public int FailedAttempts { get; set; }
}