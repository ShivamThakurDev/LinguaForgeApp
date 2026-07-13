using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace LinguaForge.Tests;

/// <summary>
/// Verifies LF-105: repeated failed logins for the same IP+email are throttled with a 429 +
/// Retry-After, and the response stays generic (no account-existence leak).
/// </summary>
[Collection(IntegrationCollection.Name)]
public class LoginThrottlingTests
{
    private readonly CustomWebApplicationFactory _factory;

    public LoginThrottlingTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Repeated_failed_logins_are_throttled_with_429_and_retry_after()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "throttle@b.com", password = "pw123456", userName = "T" });

        HttpResponseMessage last = null!;
        for (var i = 0; i < 12; i++) // exceed the default per-window permit limit (5)
        {
            last = await client.PostAsJsonAsync("/api/v1/auth/login",
                new { email = "throttle@b.com", password = "WRONG-PASSWORD" });
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last.StatusCode);
        Assert.NotNull(last.Headers.RetryAfter);

        // Generic response — must not reveal whether the account exists.
        var body = await last.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", body, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_different_email_is_not_penalised_by_another_accounts_failures()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "victim@b.com", password = "pw123456", userName = "V" });
        await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "bystander@b.com", password = "pw123456", userName = "B" });

        // Hammer the first account into lockout.
        for (var i = 0; i < 12; i++)
        {
            await client.PostAsJsonAsync("/api/v1/auth/login",
                new { email = "victim@b.com", password = "WRONG" });
        }

        // A different email (same IP) still authenticates normally.
        var other = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "bystander@b.com", password = "pw123456" });

        Assert.Equal(HttpStatusCode.OK, other.StatusCode);
    }
}
