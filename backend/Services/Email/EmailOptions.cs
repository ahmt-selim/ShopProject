namespace Backend.Services.Email;

// "Email" configuration bölümüne bağlanır (appsettings / ortam değişkeni).
// Password ASLA appsettings.json'a yazılmaz; yalnızca user-secrets
// (geliştirme) veya Email__Password ortam değişkeni (canlı) ile verilir.
public sealed class EmailOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 465;

    // SslOnConnect (465, implicit TLS) | StartTls (587) |
    // StartTlsWhenAvailable | None | Auto
    public string SecureMode { get; set; } = "SslOnConnect";

    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "İhtiyacın 3D";

    public bool HasSmtpSettings =>
        !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(User)
        && !string.IsNullOrWhiteSpace(Password)
        && !string.IsNullOrWhiteSpace(FromAddress);
}
