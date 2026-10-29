using System.Net;
using System.Threading.Channels;

namespace Backend.Services.Email;

public sealed record EmailVerificationMail(string ToEmail, string ToName, string Code, string SiteUrl);

public sealed class EmailVerificationMailQueue
{
    private readonly Channel<EmailVerificationMail> _channel = Channel.CreateUnbounded<EmailVerificationMail>();

    public bool Enqueue(EmailVerificationMail mail) => _channel.Writer.TryWrite(mail);

    internal ChannelReader<EmailVerificationMail> Reader => _channel.Reader;
}

public sealed class EmailVerificationMailWorker(
    EmailVerificationMailQueue queue,
    IServiceScopeFactory scopes,
    ILogger<EmailVerificationMailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var mail in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                await sender.SendAsync(EmailVerificationEmailBuilder.Build(mail), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "E-posta dogrulama e-postasi gonderilemedi. To={Email}", mail.ToEmail);
            }
        }
    }
}

public static class EmailVerificationEmailBuilder
{
    public const int ValidityMinutes = 10;

    public static EmailMessage Build(EmailVerificationMail mail)
    {
        var name = string.IsNullOrWhiteSpace(mail.ToName) ? "Merhaba" : $"Merhaba {mail.ToName.Trim()}";
        var safeName = WebUtility.HtmlEncode(name);
        var safeCode = WebUtility.HtmlEncode(mail.Code);
        var safeSite = WebUtility.HtmlEncode(mail.SiteUrl);

        var html = $"""
            <p>{safeName},</p>
            <p>Hesabini dogrulamak icin asagidaki 6 haneli kodu gir:</p>
            <p style="font-size:28px;letter-spacing:6px;"><strong>{safeCode}</strong></p>
            <p>Bu kod <strong>{ValidityMinutes} dakika</strong> gecerlidir ve tek kullanimliktir.</p>
            <p>Bu istegi sen yapmadiysan bu e-postayi gormezden gelebilirsin.</p>
            <p>Sevgiler,<br/>Ihtiyacin 3D<br/><a href="{safeSite}">{safeSite}</a></p>
            """;

        var text = $"""
            {name},

            Hesabini dogrulamak icin asagidaki kodu gir:
            {mail.Code}

            Bu kod {ValidityMinutes} dakika gecerlidir ve tek kullanimliktir.

            Sevgiler,
            Ihtiyacin 3D
            {mail.SiteUrl}
            """;

        return new EmailMessage(mail.ToEmail, mail.ToName, "E-posta dogrulama kodun - Ihtiyacin 3D", html, text);
    }
}