using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Backend.Auth;
using Backend.Services;
using Backend.Services.Email;
using Backend.Data;
using Backend.Logging;
using Backend.Services.Files;
using Backend.Services.Reviews;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFile(Path.Combine(builder.Environment.ContentRootPath, "logs"));

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IFileStorage, LocalFileStorage>();

// E-posta: ayarlar Email:* bölümünden okunur; Password yalnızca user-secrets
// (geliştirme) veya Email__Password ortam değişkeni (canlı) ile verilir.
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddSingleton<PasswordResetMailQueue>();
builder.Services.AddHostedService<PasswordResetMailWorker>();
builder.Services.AddSingleton<EmailVerificationMailQueue>();
builder.Services.AddHostedService<EmailVerificationMailWorker>();
builder.Services.AddScoped<PasswordResetService>();
builder.Services.AddScoped<EmailVerificationService>();
builder.Services.AddScoped<IEmailSender>(sp =>
{
    var env = sp.GetRequiredService<IHostEnvironment>();
    var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailOptions>>().Value;
    if (opt.HasSmtpSettings)
        return ActivatorUtilities.CreateInstance<SmtpEmailSender>(sp);
    if (env.IsDevelopment())
        return (IEmailSender)ActivatorUtilities.CreateInstance<ConsoleEmailSender>(sp);
    return ActivatorUtilities.CreateInstance<NullEmailSender>(sp);
});

// Reverse proxy arkasında gerçek istemci IP'si için (rate limiter IP
// partition'ı buna dayanır). KnownNetworks/Proxies temizlenir: barındırma
// ortamındaki tek proxy'ye güvenilir. UseForwardedHeaders, UseRateLimiter'dan
// önce (en üstte) çalışmalı.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    // Gerçek proxy IP'leri ForwardedHeaders:KnownProxies ile verilir
    // (prod varsayılanı 127.0.0.1: nginx aynı makinede). Liste boşsa tüm
    // X-Forwarded-For değerlerine güvenilir ve IP limiti atlatılabilir.
    foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
        if (System.Net.IPAddress.TryParse(proxy, out var ip))
            options.KnownProxies.Add(ip);
});

// Yorum oluşturma: kullanıcı Id'sine göre partition (anonim istek IP'ye göre).
// Partition User'ı okuduğu için UseRateLimiter auth'tan SONRA çalışmalı.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Limit aşımında mevcut hata formatına uygun JSON dönülür.
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Çok fazla istek. Lütfen biraz bekleyip tekrar deneyin." }, ct);
    };
    options.AddPolicy("reviews-write", httpContext =>
    {
        var sub = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var key = sub is not null
            ? $"reviews:user:{sub}"
            : $"reviews:ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        });
    });
    // Şifre sıfırlama: IP bazlı (e-posta bazlı limit servis içinde DB'ye bakarak
    // uygulanır ve 429 döndürmez). E-posta bazlıdan bağımsız kaba koruma.
    options.AddPolicy("auth-forgot", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        $"forgot:ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0,
            AutoReplenishment = true,
        }));
    options.AddPolicy("auth-reset", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        $"reset:ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0,
            AutoReplenishment = true,
        }));
    // Login: brute-force / credential-stuffing koruması (IP bazlı).
    options.AddPolicy("auth-login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        $"login:ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0,
            AutoReplenishment = true,
        }));
    // Register: toplu sahte hesap açılmasına karşı (IP bazlı).
    options.AddPolicy("auth-register", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        $"register:ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromHours(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        }));
    // Anonim sipariş takibi: sıralı sipariş no taramasına karşı (IP bazlı).
    options.AddPolicy("orders-track", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        $"track:ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0,
            AutoReplenishment = true,
        }));
    // Şifre değiştirme: brute-force koruması (kullanıcı/IP bazlı).
    options.AddPolicy("account-change-password", httpContext =>
    {
        var sub = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var key = sub is not null
            ? $"changepw:user:{sub}"
            : $"changepw:ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0,
            AutoReplenishment = true,
        });
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ihtiyacın3D API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Login/register cevabındaki tokeni yapıştırın"
    });
    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Shop") ?? "Data Source=shop.db"));

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key eksik. Development: appsettings.Development.json, canlı: Jwt__Key ortam değişkeni zorunlu.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "minishop";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "minishop";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        };
        // Şifre sıfırlanınca User.TokenVersion artar; eski token burda elenir.
        // tv claim'i olmayan eski tokenlar da reddedilir (tek seferlik relogin).
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var sub = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
                var tv = context.Principal?.FindFirst("tv")?.Value;
                if (sub is null || tv is null || !int.TryParse(tv, out var version))
                {
                    context.Fail("Token geçersiz.");
                    return Task.CompletedTask;
                }
                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var current = db.Users.AsNoTracking().FirstOrDefault(u => u.Id == sub);
                if (current is null || current.TokenVersion != version)
                    context.Fail("Token geçersiz.");
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    // appsettings "Cors:AllowedOrigins" yoksa geliştirme varsayılanları kullanılır.
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:5173", "http://localhost:5174", "http://localhost:3000"];
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// İstemci IP'si gerçeğe dönüştürülür (rate limiter ve loglar için);
// UseRateLimiter'dan önce, en üstte çalışmalı.
app.UseForwardedHeaders();

// En dışta: tüm istekleri yakalar, hatayı loglar
app.UseMiddleware<RequestLoggingMiddleware>();

// Güvenlik başlıkları: MIME-sniffing ve clickjacking'e karşı.
// Yüklenen dosyalar /uploads altında statik sunulduğu için nosniff kritik.
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Legacy shop.db'ler history damgasıyla, sıfır DB Migrate() ile açılır.
        // Migration DB'ye ilk kez burada değecek -> yedek almadan uygulamayı çalıştırma.
        DbMigrator.EnsureMigrated(db);
        ShopDbSeeder.Seed(db, app.Configuration, app.Logger);
    }
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Veritabanı başlatma/seed HATASI");
    throw;
}

var wwwroot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(wwwroot, "uploads"));

app.UseStaticFiles();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new { name = "E-Ticaret API", version = "4.0", db = "sqlite", endpoints = new[] { "/api/products", "/api/categories", "/api/brands", "/api/auth", "/api/addresses", "/api/favorites", "/api/orders", "/api/customers", "/api/settings", "/api/upload" } }));

// SMTP ayarı eksikse çökme olmaz; durum açılışta açıkça loglanır.
// Development: reset bağlantısı konsola yazılır. Production: gönderim devre dışıdır.
{
    var emailSection = app.Configuration.GetSection("Email");
    var smtpReady = !string.IsNullOrWhiteSpace(emailSection["Host"])
        && !string.IsNullOrWhiteSpace(emailSection["User"])
        && !string.IsNullOrWhiteSpace(emailSection["Password"])
        && !string.IsNullOrWhiteSpace(emailSection["FromAddress"]);
    if (!smtpReady)
    {
        if (app.Environment.IsDevelopment())
            app.Logger.LogWarning("Email SMTP ayarı eksik (Development): sıfırlama bağlantıları konsola yazılacak. Gerçek gönderim için user-secrets ile Email:Password girin.");
        else
            app.Logger.LogError("Email SMTP ayarı eksik (Production): e-posta gönderimi DEVRE DIŞI. Email__Password ortam değişkenini ve Email:* ayarlarını girin.");
    }
    // ForwardedHeaders proxy listesi boşsa IP rate-limit XFF spoof ile atlatılabilir.
    var knownProxies = app.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
    if (!app.Environment.IsDevelopment() && knownProxies.Length == 0)
        app.Logger.LogWarning("ForwardedHeaders:KnownProxies boş (Production): X-Forwarded-For spoof ile IP limiti atlatılabilir. Gerçek proxy IP'sini yazın.");
}

app.Run();
