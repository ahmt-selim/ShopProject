namespace Backend.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = "customer"; // admin | customer
    // E-posta dogrulama durumu. Kayitta false; dogrulama kodu dogru girilince true.
    public bool EmailConfirmed { get; set; } = false;
    // JWT iptali icin sayac: sifre sifirlaninca artar, eski tokenlar elenir.
    public int TokenVersion { get; set; } = 0;
}