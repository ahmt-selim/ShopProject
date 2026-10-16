using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Backend.Services.Email;

// Hosting sağlayıcısının SMTP sunucusuna MailKit ile gönderir.
// Kimlik doğrulama zorunludur. Hata log'ları şifre/token/link içermez.
public sealed class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        mime.To.Add(new MailboxAddress(message.ToName ?? string.Empty, message.To));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder { HtmlBody = message.HtmlBody, TextBody = message.TextBody }.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_options.Host, _options.Port, ToSecureSocket(_options.SecureMode), ct);
            if (!string.IsNullOrWhiteSpace(_options.User))
                await client.AuthenticateAsync(_options.User, _options.Password, ct);
            await client.SendAsync(mime, ct);
            await client.DisconnectAsync(true, ct);
            logger.LogInformation("E-posta gönderildi. To={Email}", message.To);
        }
        catch (Exception ex)
        {
            // Şifre ve içerik (token/link) loglanmaz; yalnızca bağlantı bilgileri.
            logger.LogError(ex, "SMTP ile e-posta gönderilemedi. Host={Host} Port={Port} User={User}",
                _options.Host, _options.Port, _options.User);
            throw;
        }
    }

    private static SecureSocketOptions ToSecureSocket(string mode) =>
        mode switch
        {
            "SslOnConnect" => SecureSocketOptions.SslOnConnect,
            "StartTls" => SecureSocketOptions.StartTls,
            "StartTlsWhenAvailable" => SecureSocketOptions.StartTlsWhenAvailable,
            "None" => SecureSocketOptions.None,
            _ => SecureSocketOptions.Auto,
        };
}
