namespace Backend.Auth;

// Kayıt (AuthController) ve şifre sıfırlama (PasswordResetService) aynı
// kuralı kullanır: yalnızca minimum uzunluk, başka karmaşıklık şartı yok.
// Hash için her zaman PasswordHasher (PBKDF2) kullanılır.
public static class PasswordRules
{
    public const int MinLength = 8;
    // Üst sınır: MB'larca şifrenin PBKDF2'den geçip CPU yormasını engeller.
    public const int MaxLength = 128;

    public static string? Validate(string? password)
    {
        if (password is null || password.Length < MinLength)
            return $"Şifre en az {MinLength} karakter olmalı.";
        if (password.Length > MaxLength)
            return $"Şifre en fazla {MaxLength} karakter olmalı.";
        return null;
    }
}
