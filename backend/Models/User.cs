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
    // JWT iptali için sayaç: şifre sıfırlanınca artar, eski tokenlar elenir.
    public int TokenVersion { get; set; } = 0;
}
