using LinguaForge.Application.Interface;
using LinguaForge.Application.UseCaseServices;
using LinguaForge.Infrastructure.Configuration;
using LinguaForge.Infrastructure.Data;
using LinguaForge.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter JWT as: Bearer {token}",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.Configure<AzureSpeechOptions>(builder.Configuration.GetSection(AzureSpeechOptions.SectionName));
builder.Services.Configure<AzureOpenAIOptions>(builder.Configuration.GetSection(AzureOpenAIOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<LoginThrottleOptions>(builder.Configuration.GetSection(LoginThrottleOptions.SectionName));

builder.Services.AddDbContext<LinguaForgeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

// Fail fast: never boot with a missing, default, or weak signing key. A short/known key
// lets anyone forge tokens and bypass every [Authorize] endpoint. (see JwtKeyGuard, LF-101)
JwtKeyGuard.Validate(jwtOptions.Key);

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddHttpClient<ITranslationService, AzureTranslationService>();
builder.Services.AddScoped<IAzureSpeechService, AzureSpeechService>();
builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IUserProgressService, UserProgressService>();
builder.Services.AddScoped<ILessonService, LessonService>();

// Login brute-force throttle: in-memory failure counters per IP+email. (LF-105)
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ILoginThrottle, LoginThrottle>();

builder.Services.AddScoped<AuthAppService>();
builder.Services.AddScoped<TranslationAppService>();
builder.Services.AddScoped<SpeechAppService>();
builder.Services.AddScoped<AiChatAppService>();
builder.Services.AddScoped<RecommendationAppService>();
builder.Services.AddScoped<UserProgressAppService>();
builder.Services.AddScoped<LessonAppService>();

// Allowed origins come from config (Cors:AllowedOrigins) so prod can lock to the real
// domain instead of localhost. Falls back to the Angular dev server for local work.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (allowedOrigins is null || allowedOrigins.Length == 0)
{
    allowedOrigins = new[] { "http://localhost:4200" };
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              // Required so the browser will send the HttpOnly refresh cookie on /auth calls
              // (SPA must use withCredentials). Safe here because origins are explicitly
              // allow-listed above — credentials + AllowAnyOrigin is what would be unsafe. (LF-103)
              .AllowCredentials());
});

// Per-user (or per-IP when anonymous) limit on the metered Azure endpoints
// (translation, speech, OpenAI chat) so a single token can't run up the bill.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("MeteredApi", httpContext =>
    {
        var partitionKey = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Skip DB migrate/seed under the integration-test host, which has no SQL Server. (LF-102)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LinguaForgeDbContext>();
    await DbBootstrapper.InitializeAsync(db);
    await ContentSeeder.SeedAsync(db);
}

app.UseCors("AllowAngular");
app.UseHttpsRedirection();
// Authentication MUST run before the rate limiter so the "MeteredApi" policy can partition by
// the authenticated user id; otherwise the partition key is empty and it silently degrades to
// per-IP throttling on the billable endpoints. (LF-102)
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();

// Exposed so integration tests can bootstrap the app via WebApplicationFactory<Program>. (LF-100)
public partial class Program { }
