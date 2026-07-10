using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LinguaForge.Application.DTOs;
using LinguaForge.Application.Interface;
using LinguaForge.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace LinguaForge.Tests;

/// <summary>
/// Boots the real API in an in-process test host for integration tests:
/// runs in the "Testing" environment (skips DB migrate/seed), supplies a valid JWT config,
/// swaps SQL Server for SQLite in-memory, and stubs the Azure OpenAI service so no external
/// call is made. Used to verify middleware/pipeline behavior such as rate-limiter partitioning.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // A key that clears the boot guard: >= 32 bytes and contains no placeholder fragment.
    public const string JwtKey = "integration-test-signing-key-0123456789-abcdefghij";
    public const string Issuer = "LinguaForge";
    public const string Audience = "LinguaForge";

    private SqliteConnection? _connection;

    public CustomWebApplicationFactory()
    {
        // The boot guard reads Jwt:Key from builder.Configuration BEFORE the host is built, so
        // ConfigureAppConfiguration is too late. Environment variables are read at CreateBuilder
        // time, so set them here (before the host is created on first CreateClient()).
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("Jwt__Key", JwtKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", Issuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", Audience);
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", "60");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Replace the SQL Server DbContext with SQLite in-memory so no real database is needed.
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<LinguaForgeDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(LinguaForgeDbContext)).ToList();
            foreach (var d in toRemove) services.Remove(d);

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            services.AddDbContext<LinguaForgeDbContext>(o => o.UseSqlite(_connection));

            // Deterministic AI so the rate-limiter test exercises throttling, not Azure.
            services.RemoveAll<IAzureOpenAIService>();
            services.AddScoped<IAzureOpenAIService, FakeOpenAiService>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Program skips DB migrate/seed under "Testing", so create the SQLite schema here for
        // integration tests that actually hit the database (register/login/refresh cookie flow).
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LinguaForgeDbContext>();
            db.Database.EnsureCreated();
        }

        return host;
    }

    /// <summary>Mints a valid access JWT for the given user id, signed with the test key.</summary>
    public string CreateToken(string userId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Dispose();
            Environment.SetEnvironmentVariable("Jwt__Key", null);
            Environment.SetEnvironmentVariable("Jwt__Issuer", null);
            Environment.SetEnvironmentVariable("Jwt__Audience", null);
            Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", null);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        }
    }

    private sealed class FakeOpenAiService : IAzureOpenAIService
    {
        public Task<QuizExerciseDto> GenerateExerciseAsync(string topic, string level, string exerciseType, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<QuizEvaluationResponseDto> EvaluateExerciseAsync(QuizEvaluationRequestDto request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<string> GetChatResponseAsync(IReadOnlyList<ChatMessageDto> conversationHistory, CancellationToken cancellationToken = default)
            => Task.FromResult("ok");
    }
}
