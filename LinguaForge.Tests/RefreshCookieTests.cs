using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace LinguaForge.Tests;

/// <summary>
/// Verifies LF-103: the refresh token is delivered and consumed as an
/// <c>HttpOnly; Secure; SameSite=Strict</c> cookie (never in the JSON body), and logout clears it.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class RefreshCookieTests
{
    private readonly CustomWebApplicationFactory _factory;

    public RefreshCookieTests(CustomWebApplicationFactory factory) => _factory = factory;

    // A cookie-aware client over HTTPS so the Secure refresh cookie round-trips (the CookieContainer
    // will not send a Secure cookie over http). The TestServer honors the https scheme.
    private HttpClient CreateSecureClient()
    {
        var client = _factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");
        return client;
    }

    [Fact]
    public async Task Login_sets_httponly_secure_samesite_refresh_cookie_and_body_has_no_refresh_token()
    {
        var client = CreateSecureClient();
        await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "cookie-login@b.com", password = "pw123456", userName = "C" });

        var res = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "cookie-login@b.com", password = "pw123456" });

        res.EnsureSuccessStatusCode();

        var setCookie = Assert.Single(res.Headers.GetValues("Set-Cookie"));
        Assert.Contains("refreshToken=", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie.Replace(" ", ""), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/v1/auth", setCookie, StringComparison.OrdinalIgnoreCase);

        // The body must carry the access token but must NOT leak the refresh token to JS.
        var body = await res.Content.ReadAsStringAsync();
        Assert.DoesNotContain("refreshToken", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"token\"", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refresh_reads_cookie_no_body_and_logout_clears_it()
    {
        var client = CreateSecureClient();
        await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "cookie-refresh@b.com", password = "pw123456", userName = "D" });

        // No body — the cookie set by register carries the refresh token.
        var refresh = await client.PostAsync("/api/v1/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);

        await client.PostAsync("/api/v1/auth/logout", null);

        var afterLogout = await client.PostAsync("/api/v1/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task Refresh_without_a_cookie_is_unauthorized()
    {
        var client = CreateSecureClient();

        var res = await client.PostAsync("/api/v1/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
