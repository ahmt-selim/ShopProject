namespace Backend.Services.Email;

// Production'da SMTP ayarı eksikse e-posta gönderimi devre dışı kalır.
// Sessizce yutar + her denemede hata loglar; konsola bağlantı yazmaz.
public sealed class NullEmailSender(ILogger<NullEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        logger.LogError("E-posta gönderimi devre dışı: SMTP ayarı eksik. To={Email}", message.To);
        return Task.CompletedTask;
    }
}
