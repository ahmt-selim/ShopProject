using System.Diagnostics;

namespace Backend.Logging;

// Tüm istekleri saran try-catch: yakalanmayan hatayı loglar, 500 JSON döner.
// Ayrıca her isteğin yöntem/yol/durum/süre bilgisini loglar.
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await next(context);
            sw.Stop();
            logger.LogInformation("{Method} {Path} -> {Status} ({Ms}ms)",
                context.Request.Method, context.Request.Path,
                context.Response.StatusCode, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "{Method} {Path} HATA ({Ms}ms) Query={Query}",
                context.Request.Method, context.Request.Path,
                sw.ElapsedMilliseconds, context.Request.QueryString);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { error = "Beklenmeyen bir hata oluştu." });
            }
            else
            {
                throw;
            }
        }
    }
}
