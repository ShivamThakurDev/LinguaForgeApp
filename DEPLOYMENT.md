# Deployment

LinguaForge ships as two containers — the **.NET API** and the **Angular SSR frontend** —
backed by **SQL Server**. All secrets are supplied at runtime via environment variables;
nothing sensitive is baked into an image or committed to git.

## Run the full stack locally

```bash
# A real signing key (>= 32 bytes) is required or the API refuses to start.
export JWT_KEY="$(openssl rand -base64 48)"
export DB_PASSWORD='Your_strong_passw0rd!'

docker compose up --build
```

- Frontend: http://localhost:4000
- API + Swagger: http://localhost:8080/swagger (Development only)
- API health check: http://localhost:8080/health

The API applies EF migrations and seeds the A1 course on startup (idempotent), so the
database is ready on first boot.

## Required environment variables (API)

| Variable | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `Jwt__Key` | JWT signing secret, **≥ 32 bytes**. The boot guard (`JwtKeyGuard`) refuses to start on a missing, too-short, or placeholder/default key, and `docker-compose.yml` has **no** working default — `docker compose up` fails fast unless `JWT_KEY` is set. |
| `Jwt__Issuer`, `Jwt__Audience` | Token issuer/audience |
| `Cors__AllowedOrigins__0`, `__1`, … | Allowed browser origins (the frontend URL). Credentials are enabled for these origins so the SPA can send the refresh cookie on `/auth` calls (`withCredentials`). Must be explicit origins — never `*` with credentials |
| `Auth__Login__PermitLimit`, `Auth__Login__WindowSeconds`, `Auth__Login__LockoutSeconds` | Optional login-throttle tuning (defaults 5 / 60 / 300). Failed logins per IP+email before a temporary lockout |
| `AzureOpenAI__ApiKey`, `AzureSpeech__ApiKey`, `AzureTranslator__ApiKey` | Optional; only needed for the metered AI/speech/translation features |

ASP.NET maps `__` (double underscore) in env var names to nested config keys, so these
override `appsettings.json` without any code change.

## Frontend API URL

The browser API base URL is baked at build time via `angular.json` → `production`
`fileReplacements` (`src/enviornment/environment.prod.ts`). Set it to the **public** API
URL the browser will reach, then rebuild the image.

> ⚠️ **SSR caveat:** because the URL is baked, server-side rendering uses the same URL.
> In `docker compose`, `http://localhost:8080` is not reachable from inside the `web`
> container, so the first server render of pages that fetch data (e.g. the lesson map)
> may fail and then recover on the client. For production behind one domain this is a
> non-issue; if you need correct SSR data fetching across separate hosts, move the API
> URL to runtime config injected by the SSR server. Tracked as a follow-up.

## Cookies & HTTPS requirement

The refresh token is issued as a cookie with `HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth`
(see [AuthController](LinguaForge.API/Controllers/AuthController.cs)). Operational implications:

- **HTTPS is mandatory in any real environment.** Because the cookie is `Secure`, browsers only
  send it over TLS. Serve the API over HTTPS (platform-terminated TLS is fine — the cookie flag is
  set regardless of how TLS is terminated, and the browser enforces it).
- **`docker compose` is plain HTTP** (`http://localhost:8080`), so a browser will *store but not
  send back* the `Secure` refresh cookie there — cookie-based refresh won't work end-to-end in the
  raw compose stack. Put the stack behind an HTTPS reverse proxy (or use the API over HTTPS) to
  exercise the full login → refresh → logout flow in a browser. Local `dotnet run` already serves
  the API over `https://localhost:<port>`, so it works there.
- **`SameSite=Strict` ⇒ deploy front-end and API on the same site** (same registrable domain, e.g.
  `app.example.com` + `api.example.com`). Cross-site deployments won't send the cookie under Strict.
- **CORS + credentials:** the SPA calls `/auth` endpoints with `withCredentials: true`; the API
  enables `AllowCredentials()` for the **explicit** `Cors:AllowedOrigins` only. Never combine
  credentials with a wildcard origin.
- **Dev vs prod:** in local dev the Angular app (`http://localhost:4200`) and the API
  (`https://localhost:<port>`) are same-site (`localhost`), so the cookie round-trips. Set
  `Cors:AllowedOrigins` to the real front-end origin(s) in production.

## CI

`.github/workflows/ci.yml` runs on every push/PR to `main`:

- **backend** — restore, build (Release), and run the xUnit test suite
  (`LinguaForge.Tests`, SQLite in-memory).
- **frontend** — `npm ci` and a production build.

## Recommended production stack (Azure-native)

You already depend on Azure for Translator/Speech/OpenAI, so stay in one cloud:

- **API** → **Azure Container Apps** (deploy the API image; scales to zero).
- **Database** → **Azure SQL** (Serverless/Basic for MVP). `MigrateAsync` runs on startup.
- **Frontend (SSR)** → **Azure App Service (Node)** running the Express SSR server, or a
  second Container App from the frontend image. *(Vercel/Netlify don't run Angular's
  custom Express SSR cleanly — avoid them here.)*
- **Secrets** → **Azure Key Vault**, surfaced to the apps as the env vars above.
- **Monitoring** → Application Insights (backend) + a product analytics tool (frontend).

## Pre-production checklist

- [x] Containerized API + frontend (multi-stage, runtime-only images)
- [x] EF migrations applied on startup (no `EnsureCreated`)
- [x] Secrets via env vars; `.gitignore` excludes `appsettings.json`
- [x] JWT boot guard rejects weak/placeholder keys (LF-101)
- [x] Rate limiting on metered endpoints, ordered after auth (per-user); config-driven CORS (LF-102)
- [x] Short-lived access JWT + rotating refresh token in an `HttpOnly; Secure; SameSite=Strict` cookie (LF-103/104)
- [x] Login brute-force throttling + temporary lockout (LF-105)
- [x] `/health` endpoint
- [x] CI builds both stacks and runs tests
- [ ] Key Vault wiring + Application Insights in the target environment
- [ ] Custom domain + TLS (platform-terminated) — **required** for the `Secure` refresh cookie
