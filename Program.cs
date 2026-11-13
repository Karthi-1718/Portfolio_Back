using AspNetCoreRateLimit;
using dotenv.net;
using FluentValidation;
using PortfolioApi.Middleware;
using PortfolioApi.Models;
using PortfolioApi.Services;
using PortfolioApi.Validators;
using Scalar.AspNetCore;

// ─── Load .env ────────────────────────────────────────────────────────────────
DotEnv.Load(options: new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 3));

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// ─── Bind Resend settings from environment ────────────────────────────────────
builder.Services.AddOptions<ResendSettings>()
    .Configure(options =>
{
    options.ApiKey               = builder.Configuration["RESEND_API_KEY"]          ?? string.Empty;
    options.FromEmail            = builder.Configuration["RESEND_FROM_EMAIL"]       ?? string.Empty;
    options.ContactReceiverEmail = builder.Configuration["CONTACT_RECEIVER_EMAIL"]  ?? string.Empty;
    })
    .Validate(settings =>
        !string.IsNullOrWhiteSpace(settings.ApiKey) &&
        settings.ApiKey.StartsWith("re_", StringComparison.Ordinal) &&
        !settings.ApiKey.Contains("your_key", StringComparison.OrdinalIgnoreCase),
        "RESEND_API_KEY is missing or still contains the template value. Set a valid Resend API key.")
    .Validate(settings =>
        IsConfiguredEmail(settings.FromEmail) &&
        !settings.FromEmail.Contains("yourdomain.com", StringComparison.OrdinalIgnoreCase),
        "RESEND_FROM_EMAIL is missing or still contains the template value. Use a sender address on a domain verified in Resend.")
    .Validate(settings =>
        IsConfiguredEmail(settings.ContactReceiverEmail) &&
        !settings.ContactReceiverEmail.Contains("yourdomain.com", StringComparison.OrdinalIgnoreCase),
        "CONTACT_RECEIVER_EMAIL is missing or still contains the template value. Set the inbox that should receive contact messages.")
    .ValidateOnStart();

static bool IsConfiguredEmail(string value) =>
    !string.IsNullOrWhiteSpace(value) &&
    System.Net.Mail.MailAddress.TryCreate(value, out var address) &&
    string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase);

// ─── CORS ─────────────────────────────────────────────────────────────────────
var frontendUrl = builder.Configuration["FRONTEND_URL"] ?? "http://localhost:4200";

builder.Services.AddCors(options =>
{
    options.AddPolicy("PortfolioCors", policy =>
        policy.WithOrigins(frontendUrl, "http://localhost:4200")
              .WithMethods("GET", "POST", "OPTIONS")
              .WithHeaders("Content-Type", "Accept"));
});

// ─── IP Rate Limiting ─────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(opts =>
{
    opts.EnableEndpointRateLimiting = true;
    opts.StackBlockedRequests       = false;
    opts.HttpStatusCode             = 429;
    opts.RealIpHeader               = "X-Real-IP";
    opts.GeneralRules =
    [
        new RateLimitRule { Endpoint = "POST:/api/contact", Period = "15m", Limit = 5 },
        new RateLimitRule { Endpoint = "*",                 Period = "1m",  Limit = 60 }
    ];
});
builder.Services.AddSingleton<IIpPolicyStore,            MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore,    MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IProcessingStrategy,       AsyncKeyLockProcessingStrategy>();
builder.Services.AddSingleton<IRateLimitConfiguration,  RateLimitConfiguration>();
builder.Services.AddInMemoryRateLimiting();

// ─── HTTP Client for Resend ───────────────────────────────────────────────────
builder.Services.AddHttpClient<IEmailService, ResendEmailService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("User-Agent", "PortfolioApi/1.0 (.NET 10)");
});

// ─── Validators ───────────────────────────────────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<ContactRequestValidator>();

// ─── Controllers + JSON ───────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

// ─── OpenAPI (built-in .NET 10 + Scalar UI) ───────────────────────────────────
builder.Services.AddOpenApi();

var app = builder.Build();

// ─── Middleware ───────────────────────────────────────────────────────────────
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseIpRateLimiting();
app.UseHttpsRedirection();
app.UseCors("PortfolioCors");

// ─── OpenAPI / Scalar (dev only) ──────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opts =>
    {
        opts.Title       = "Karthikeyan Portfolio API";
        opts.Theme       = ScalarTheme.DeepSpace;
        opts.DefaultFonts = false;
    });
}

// ─── Routes ───────────────────────────────────────────────────────────────────
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status    = "healthy",
    service   = "Karthikeyan Portfolio API",
    framework = ".NET 10 / C# / ASP.NET Core",
    timestamp = DateTime.UtcNow.ToString("o")
})).ExcludeFromDescription();

// ─── Startup Banner ───────────────────────────────────────────────────────────
var port = builder.Configuration["PORT"] ?? "5000";

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine($"""

  ╔═══════════════════════════════════════════════════════╗
  ║   KARTHIKEYAN K – Portfolio API  (.NET 10 / C#)       ║
  ╠═══════════════════════════════════════════════════════╣
  ║  📧  POST  http://localhost:{port}/api/contact          ║
  ║  🏥  GET   http://localhost:{port}/health               ║
  ║  📖  GET   http://localhost:{port}/scalar  (dev only)   ║
  ║  🌍  Env:  {app.Environment.EnvironmentName,-43}  ║
  ╚═══════════════════════════════════════════════════════╝

""");
Console.ResetColor();

app.Run($"http://localhost:{port}");
