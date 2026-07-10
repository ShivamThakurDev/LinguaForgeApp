using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace LinguaForge.Tests;

/// <summary>
/// Verifies LF-102: with the rate limiter ordered after authentication, the "MeteredApi"
/// policy partitions by the authenticated user id — so two users sharing one source IP get
/// independent buckets, instead of silently sharing a per-IP bucket.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class RateLimiterPartitionTests
{
    private readonly CustomWebApplicationFactory _factory;

    public RateLimiterPartitionTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Two_users_same_ip_get_independent_buckets()
    {
        var client = _factory.CreateClient(); // one client == one source connection/IP

        // Exhaust userA's window (PermitLimit = 20/min) on a metered endpoint.
        var userA = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken(userA));
        for (var i = 0; i < 20; i++)
        {
            var ok = await PostChat(client);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, ok.StatusCode);
        }

        var userAOverLimit = await PostChat(client);
        Assert.Equal(HttpStatusCode.TooManyRequests, userAOverLimit.StatusCode);

        // userB (same IP, different token) must NOT inherit userA's exhausted bucket.
        var userB = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken(userB));
        var userBFirst = await PostChat(client);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, userBFirst.StatusCode);
    }

    [Fact]
    public async Task Anonymous_request_to_metered_endpoint_is_unauthorized_not_throttled()
    {
        var client = _factory.CreateClient();

        var res = await PostChat(client); // no bearer token

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    private static Task<HttpResponseMessage> PostChat(HttpClient client) =>
        client.PostAsJsonAsync("/api/v1/ai/chat", new
        {
            conversationHistory = new[] { new { role = "user", content = "hi" } }
        });
}
