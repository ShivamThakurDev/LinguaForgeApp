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
| `Jwt__Key` | JWT signing secret, **≥ 32 bytes** (boot guard enforces this) |
| `Jwt__Issuer`, `Jwt__Audience` | Token issuer/audience |
| `Cors__AllowedOrigins__0`, `__1`, … | Allowed browser origins (the frontend URL) |
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
- [x] JWT boot guard rejects weak/placeholder keys
- [x] Rate limiting on metered endpoints; config-driven CORS
- [x] `/health` endpoint
- [x] CI builds both stacks and runs tests
- [ ] Refresh tokens (current JWT is 60 min, no refresh) — follow-up
- [ ] Key Vault wiring + Application Insights in the target environment
- [ ] Custom domain + TLS (platform-terminated)
