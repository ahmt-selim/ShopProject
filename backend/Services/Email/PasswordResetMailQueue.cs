using System.Net;
using System.Threading.Channels;

namespace Backend.Services.Email;

public sealed record PasswordResetMail(string ToEmail, string ToName, string Code, string SiteUrl);

// E-posta gönderimi isteği bloklamasın diye forgot-password endpoint'i
// iletiyi kuyruğa bırakıp hemen döner; gönderimi Worker arka planda yapar.
public sealed class PasswordResetMailQueue
{
    private readonly Channel<PasswordResetMail> _channel = Channel.CreateUnbounded<PasswordResetMail>();

    public bool Enqueue(PasswordResetMail mail) => _channel.Writer.TryWrite(mail);

    internal ChannelReader<PasswordResetMail> Reader => _channel.Reader;
}

// Kendi DI scope'unu açar; istek kapsamındaki DbContext'i kullanmaz.
public sealed class PasswordResetMailWorker(
    PasswordResetMailQueue queue,
    IServiceScopeFactory scopes,
    ILogger<PasswordResetMailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var mail in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                await sender.SendAsync(PasswordResetEmailBuilder.Build(mail), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Token/link loglanmaz; yalnızca alıcı adresi.
                logger.LogError(ex, "Şifre sıfırlama e-postası gönderilemedi. To={Email}", mail.ToEmail);
            }
        }
    }
}

public static class PasswordResetEmailBuilder
{
    public const int ValidityMinutes = 60;

    public static EmailMessage Build(PasswordResetMail mail)
    {
        // Dinamik değerler HTML encode edilir; ayrıca düz metin alternatifi var.
        var name = string.IsNullOrWhiteSpace(mail.ToName) ? "Merhaba" : $"Merhaba {mail.ToName.Trim()}";
        var safeName = WebUtility.HtmlEncode(name);
        var safeCode = WebUtility.HtmlEncode(mail.Code);
        var safeSite = WebUtility.HtmlEncode(mail.SiteUrl);

        var html = $"""
            <p>{safeName},</p>
            <p>Hesabın için şifre sıfırlama isteği aldık. Şifre sıfırlama sayfasına aşağıdaki kodu gir:</p>
            <p style="font-size:28px;letter-spacing:6px;"><strong>{safeCode}</strong></p>
            <p>Bu kod <strong>{ValidityMinutes} dakika</strong> geçerlidir ve tek kullanımlıktır.</p>
            <p>Bu isteği sen yapmadıysan bu e-postayı görmezden gelebilirsin; şifren değişmez.</p>
            <p>Sevgiler,<br/>İhtiyacın 3D<br/><a href="{safeSite}">{safeSite}</a></p>
            """;

        var text = $"""
            {name},

            Hesabın için şifre sıfırlama isteği aldık. Şifre sıfırlama sayfasına bu kodu gir:
            {mail.Code}

            Bu kod {ValidityMinutes} dakika geçerlidir ve tek kullanımlıktır.
            Bu isteği sen yapmadıysan bu e-postayı görmezden gelebilirsin; şifren değişmez.

            Sevgiler,
            İhtiyacın 3D
            {mail.SiteUrl}
            """;

        return new EmailMessage(mail.ToEmail, mail.ToName, "Şifre sıfırlama kodun – İhtiyacın 3D", html, text);
    }
}
