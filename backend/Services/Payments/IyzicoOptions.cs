namespace Backend.Services.Payments;

public class IyzicoOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://sandbox-api.iyzipay.com";
    // Sandbox test kimlik numarası. Üretimde kullanıcıdan toplanacak TC Kimlik No kullanılmalıdır.
    public string TestIdentityNumber { get; set; } = "74300864791";
}