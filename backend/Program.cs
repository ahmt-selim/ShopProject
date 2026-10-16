using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Backend.Auth;
using Backend.Data;
using Backend.Logging;
using Backend.Services.Files;
using Backend.Services.Reviews;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

// Yorum oluşturma: kullanıcı Id'sine göre partition (anonim istek IP'ye göre).
// Partition User'ı okuduğu için UseRateLimiter auth'tan SONRA çalışmalı.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
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

var jwtKey = builder.Configuration["Jwt:Key"] ?? "minishop-dev-secret-key-en-az-32-karakter-123456";
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
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// En dışta: tüm istekleri yakalar, hatayı loglar
app.UseMiddleware<RequestLoggingMiddleware>();

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
        ShopDbSeeder.Seed(db);
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

app.Run();
