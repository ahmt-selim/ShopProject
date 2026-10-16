namespace Backend.Services.Email;

// YALNIZCA Development: SMTP ayarı (özellikle şifre) yoksa devreye girer.
// Reset bağlantısını log dosyasına DEĞİL, yalnızca konsola yazar
// (Console.WriteLine, ILogger değil -> logs/*.log'a düşmez).
// Production'da bu sınıf asla kullanılmaz (bkz. Program.cs seçimi).
public sealed class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        Console.WriteLine("=== [DEV] Şifre sıfırlama bağlantısı ===");
        Console.WriteLine($"Kime: {message.To}");
        Console.WriteLine($"Konu: {message.Subject}");
        Console.WriteLine();
        Console.WriteLine(message.TextBody);
        Console.WriteLine("========================================");
        logger.LogInformation("Geliştirme e-postası konsola yazıldı. To={Email}", message.To);
        return Task.CompletedTask;
    }
}
