# Sprint 1 — "Secure Auth Foundations" · Jira-ready tickets

**Sprint goal:** Ensure LinguaForge cannot boot with a weak/committed JWT key, align rate limiting with authenticated users, and move refresh tokens to a secure cookie-based flow.

**Duration:** Days 1–14 (2-week sprint). **Release gate:** app is *not* production-ready until every ticket here is Done.

**Definition of Done (all stories):** code implemented + reviewed · unit/integration tests added or updated · verified locally **and** via `docker compose` · docs updated (README/DEPLOYMENT) · one-line before/after security note captured.

> These six tickets refine the Sprint-1 slice of [DELIVERY-PLAN.md](DELIVERY-PLAN.md). Keys `LF-101`…`LF-106`. Every technical note points at the exact code touched. Example acceptance tests follow the repo's existing conventions (`LinguaForge.Tests`, xUnit + `SqliteTestContext`; integration tests use `WebApplicationFactory<Program>`).

**Prerequisite subtask (blocks LF-102/103/105 integration tests): `LF-100` — expose `Program` for integration testing.** Add `public partial class Program { }` at the end of `LinguaForge.API/Program.cs` and reference `LinguaForge.API` from `LinguaForge.Tests`. *(1 pt)*

---

## LF-101 · Fail fast on bad JWT key (boot guard v2)

| Field | Value |
|---|---|
| Type | Story · Security |
| Priority | Highest |
| Story points | 3 |
| Components | API, Config, DevOps |
| Labels | `security`, `jwt`, `high` |

**Description**
As a DevOps engineer, I want the API to refuse startup when the JWT signing key is missing, a known placeholder, or too weak, so we never run production with a committed or trivial secret.

**Context (code):** the current guard at [Program.cs:58-65](LinguaForge.API/Program.cs#L58-L65) only rejects keys containing the literal `"CHANGE_ME"` (underscore) or `< 32` bytes. The docker default `Jwt__Key: "local-dev-only-signing-key-change-me-32b+"` ([docker-compose.yml:29](docker-compose.yml#L29)) uses hyphens and is 41 bytes, so it **passes** the guard while `ASPNETCORE_ENVIRONMENT: Production` ([docker-compose.yml:26](docker-compose.yml#L26)).

**Acceptance criteria**
- [ ] Startup fails with a clear log message if `Jwt:Key` is missing/empty or `< 32` bytes.
- [ ] Startup fails if the key matches any known placeholder — `CHANGE_ME`, the former docker default, and format variants (hyphen/underscore, case-insensitive, "change me"/"changeme").
- [ ] `docker-compose.yml` no longer ships a working key: `Jwt__Key: "${JWT_KEY:?JWT_KEY must be set}"` (compose fails fast if unset).
- [ ] Guard logic extracted to a pure, unit-testable method (e.g. `JwtKeyGuard.Validate(string? key)` returning/throwing) rather than inline in `Program.cs`.
- [ ] `DEPLOYMENT.md` + `README.md` updated with the rules and example env config.

**Example acceptance tests** (`LinguaForge.Tests/JwtKeyGuardTests.cs`)
```csharp
using LinguaForge.Infrastructure.Configuration; // where JwtKeyGuard lives
using Xunit;

public class JwtKeyGuardTests
{
    [Theory]
    [InlineData(null)]                                              // missing
    [InlineData("")]                                               // empty
    [InlineData("too-short")]                                      // < 32 bytes
    [InlineData("CHANGE_ME_TO_A_LONG_RANDOM_SECRET_AT_LEAST_32_BYTES")] // appsettings placeholder
    [InlineData("local-dev-only-signing-key-change-me-32b+")]      // former docker default (regression)
    public void Rejects_weak_or_placeholder_keys(string? key)
    {
        Assert.Throws<InvalidOperationException>(() => JwtKeyGuard.Validate(key));
    }

    [Fact]
    public void Accepts_a_strong_random_key()
    {
        var strong = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
        var ex = Record.Exception(() => JwtKeyGuard.Validate(strong));
        Assert.Null(ex);
    }
}
```
**Manual/ops check**
```bash
# Expect: compose refuses to start, error names JWT_KEY.
unset JWT_KEY; docker compose up --build 2>&1 | grep -i "JWT_KEY"
```

---

## LF-102 · Rate limiter after authentication (user-aware throttling)

| Field | Value |
|---|---|
| Type | Story · Security |
| Priority | Highest |
| Story points | 2 |
| Components | API |
| Labels | `security`, `rate-limiting`, `high` |

**Description**
As a security engineer, I want rate limiting to partition by authenticated user when available, so throttling on the metered Azure endpoints is per-user, not silently per-IP.

**Context (code):** `UseRateLimiter()` runs **before** `UseAuthentication()` at [Program.cs:156-158](LinguaForge.API/Program.cs#L156-L158), so the partition key `User.FindFirstValue(NameIdentifier)` ([Program.cs:124](LinguaForge.API/Program.cs#L124)) is always null → per-IP fallback for every request.

**Acceptance criteria**
- [ ] Pipeline order: `UseAuthentication()` → `UseAuthorization()` → `UseRateLimiter()`.
- [ ] Partition key uses the JWT user id when present; falls back to IP for anonymous.
- [ ] Anonymous-endpoint throttling behavior unchanged (no regression).
- [ ] Rate-limiter config/behavior documented (per-user limit on AI/Speech/Translation).

**Example acceptance test** (`LinguaForge.Tests/RateLimiterPartitionTests.cs`, integration)
```csharp
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class RateLimiterPartitionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public RateLimiterPartitionTests(WebApplicationFactory<Program> f) => _factory = f;

    [Fact]
    public async Task Two_users_same_ip_get_independent_buckets()
    {
        var client = _factory.CreateClient(); // same source connection/IP

        // Exhaust userA's window (limit = 20/min) on a metered endpoint.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor("userA"));
        for (var i = 0; i < 20; i++) await PostChat(client);
        var userAOverLimit = await PostChat(client);
        Assert.Equal(HttpStatusCode.TooManyRequests, userAOverLimit.StatusCode);

        // userB (same IP, different token) must NOT be throttled by userA's usage.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor("userB"));
        var userBFirst = await PostChat(client);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, userBFirst.StatusCode);
    }

    private static Task<HttpResponseMessage> PostChat(HttpClient c) =>
        c.PostAsJsonAsync("/api/v1/ai/chat", new { conversationHistory = new[] { new { role = "user", content = "hi" } } });
    // TokenFor(...) = helper that mints a valid signed JWT for the test user id.
}
```

---

## LF-103 · Move refresh token to `HttpOnly` cookie (backend)

| Field | Value |
|---|---|
| Type | Story · Security |
| Priority | Highest |
| Story points | 5 |
| Components | API, Infrastructure |
| Labels | `security`, `jwt`, `refresh-token`, `high` |

**Description**
As a security engineer, I want refresh tokens delivered and read via an `HttpOnly; Secure; SameSite=Strict` cookie instead of the response body, so an XSS cannot exfiltrate the 30-day token.

**Context (code):** `AuthResponseDto` returns the raw refresh token ([AuthService.cs:191](LinguaForge.Infrastructure/Services/AuthService.cs#L191)); the frontend persists it in `localStorage`. Endpoints in [AuthController.cs](LinguaForge.API/Controllers/AuthController.cs) currently take/return the token in the body.

**Acceptance criteria**
- [ ] `register`/`login`/`refresh` set the refresh cookie: `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/v1/auth`, `Max-Age` = refresh lifetime.
- [ ] `refresh` and `logout` read the token from the cookie (body accepted only as a deprecated fallback, or removed).
- [ ] `AuthResponseDto` no longer exposes the raw refresh token to JS (access token + expiry only).
- [ ] `logout` clears the cookie (`Max-Age=0`).
- [ ] CORS allows credentials **only** for the auth path/origin; documented.

**Example acceptance test** (`LinguaForge.Tests/RefreshCookieTests.cs`, integration)
```csharp
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class RefreshCookieTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public RefreshCookieTests(WebApplicationFactory<Program> f) => _factory = f;

    [Fact]
    public async Task Login_sets_httponly_secure_samesite_refresh_cookie()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "c@b.com", password = "pw123456", userName = "C" });

        var res = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "c@b.com", password = "pw123456" });

        var setCookie = Assert.Single(res.Headers.GetValues("Set-Cookie"));
        Assert.Contains("HttpOnly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie.Replace(" ", ""), StringComparison.OrdinalIgnoreCase);

        // Body must NOT leak the refresh token to JS.
        var body = await res.Content.ReadAsStringAsync();
        Assert.DoesNotContain("refreshToken", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refresh_uses_cookie_and_logout_clears_it()
    {
        var handler = new HttpClientHandler { UseCookies = true, CookieContainer = new() };
        var client = _factory.CreateDefaultClient(handler);
        await client.PostAsJsonAsync("/api/v1/auth/register", new { email = "d@b.com", password = "pw123456", userName = "D" });

        var refresh = await client.PostAsync("/api/v1/auth/refresh", null); // no body; cookie carries it
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);

        await client.PostAsync("/api/v1/auth/logout", null);
        var afterLogout = await client.PostAsync("/api/v1/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }
}
```

---

## LF-104 · Update Angular auth flow for cookie-based refresh (frontend)

| Field | Value |
|---|---|
| Type | Story · Frontend |
| Priority | Highest |
| Story points | 3 |
| Components | Web (Angular) |
| Labels | `security`, `frontend`, `auth` |

**Description**
As a user, I want the Angular app to transparently use cookie-based refresh, so the UX stays smooth while the refresh token is no longer readable by JS.

**Context (code):** [auth.service.ts](src/app/core/services/auth.service.ts) stores the whole `AuthResponse` (incl. refresh token) in `localStorage`; [auth.interceptor.ts](src/app/core/services/auth.interceptor.ts) sends the refresh token from JS.

**Acceptance criteria**
- [ ] `AuthService` holds only the **access** JWT in memory (no refresh token in `localStorage`).
- [ ] Interceptor keeps `Authorization: Bearer <jwt>`; on 401 calls `/auth/refresh` with `withCredentials: true` (cookie carries the token).
- [ ] Single-flight refresh preserved (no concurrent refresh storms).
- [ ] A failed **refresh** logs out; a failed **retried request** does not (ties to interceptor hardening).
- [ ] CORS/`withCredentials` verified — no console credential/CORS errors.

**Example acceptance test** (`src/app/core/services/auth.interceptor.spec.ts`, Vitest + `HttpTestingController`)
```ts
it('on 401, refreshes via cookie (no token in body) and retries the original request', async () => {
  // Arrange: interceptor set up with HttpClientTesting; access token present in AuthService.
  http.get('/api/v1/user/progress').subscribe();
  httpMock.expectOne('/api/v1/user/progress').flush(null, { status: 401, statusText: 'Unauthorized' });

  const refresh = httpMock.expectOne('/api/v1/auth/refresh');
  expect(refresh.request.withCredentials).toBe(true);        // cookie-based
  expect(refresh.request.body).toBeFalsy();                  // no refresh token in JS payload
  refresh.flush({ token: 'new.jwt', expiresAtUtc: futureIso });

  const retried = httpMock.expectOne('/api/v1/user/progress');
  expect(retried.request.headers.get('Authorization')).toBe('Bearer new.jwt');
});

it('does not force logout when the RETRIED request fails transiently', () => {
  // 401 -> refresh OK -> retry returns 500 -> user stays authenticated (no /welcome redirect)
});
```
**Manual browser check:** after login, `localStorage` contains no refresh token; `document.cookie` cannot read the `HttpOnly` refresh cookie; expiring the access token triggers a silent refresh + retry.

---

## LF-105 · Basic login throttling / lockout

| Field | Value |
|---|---|
| Type | Story · Security |
| Priority | High |
| Story points | 5 |
| Components | API |
| Labels | `security`, `brute-force`, `medium` |

**Description**
As a security engineer, I want login attempts rate-limited and temporarily locked out, so brute-force and credential-stuffing are impractical.

**Context (code):** [AuthController.cs](LinguaForge.API/Controllers/AuthController.cs) `login`/`register` have no throttling; only the Azure controllers use `MeteredApi`.

**Acceptance criteria**
- [ ] Limiter on `/auth/login` partitioned by **IP + email** (e.g. N attempts/min; temporary block after M failures).
- [ ] Over-limit returns `429` with `Retry-After`; responses stay generic (no user enumeration).
- [ ] Limits configurable via `appsettings` (`Auth:Login:PermitLimit`, `:WindowSeconds`, `:LockoutSeconds`).
- [ ] Successful login works again after the window elapses.

**Example acceptance test** (`LinguaForge.Tests/LoginThrottlingTests.cs`, integration)
```csharp
[Fact]
public async Task Repeated_failed_logins_are_throttled_with_429()
{
    var client = _factory.CreateClient();
    await client.PostAsJsonAsync("/api/v1/auth/register", new { email = "t@b.com", password = "pw123456", userName = "T" });

    HttpResponseMessage last = null!;
    for (var i = 0; i < 12; i++)   // exceed the configured per-minute limit
        last = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "t@b.com", password = "WRONG" });

    Assert.Equal(HttpStatusCode.TooManyRequests, last.StatusCode);
    Assert.True(last.Headers.RetryAfter is not null);
}
```

---

## LF-106 · Docs & ops updates for Sprint 1

| Field | Value |
|---|---|
| Type | Task · Docs |
| Priority | High |
| Story points | 3 |
| Components | Docs, DevOps |
| Labels | `docs`, `security` |

**Description**
As a developer/operator, I want updated docs so the new security behaviors are clear for local dev, Docker, and production.

**Acceptance criteria**
- [ ] `README.md` + `DEPLOYMENT.md` updated for: JWT key requirements (LF-101), cookie-based refresh (LF-103/104), rate-limiter ordering + config (LF-102), login throttling (LF-105).
- [ ] Sample configs contain no working keys; CORS/`withCredentials` flags corrected.
- [ ] A short "Security hardening 2026-07" note (before/after) added and cross-linked.
- [ ] `docker compose up --build` behaves exactly as documented (fails fast without `JWT_KEY`).

**Verification**
```bash
# Documented happy path must run cleanly:
JWT_KEY="$(openssl rand -base64 48)" DB_PASSWORD='Your_strong_passw0rd!' docker compose up --build
```

---

## Board import notes

- **Suggested order:** LF-100 (unblock) → LF-101 → LF-102 → LF-103 → LF-104 → LF-105 → LF-106.
- **Dependencies:** LF-104 depends on LF-103; LF-102/103/105 integration tests depend on LF-100.
- **Total:** 22 pts (incl. LF-100). Trim LF-105 to Sprint 2 if capacity is tight — it's the only Medium here.
