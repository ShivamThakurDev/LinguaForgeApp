# LinguaForge

LinguaForge is a modern, gamified language-learning application (currently an **A1 German** MVP) built to make learning engaging, adaptive, and data-driven. It pairs an interactive learning engine with real-time progress analytics and optional AI-powered assistance (chat tutor, pronunciation assessment, translation).

The system is a **.NET clean-architecture API** + an **Angular 21 SSR frontend**, backed by **SQL Server**.

> **Status (as of 2026-07):** Functional MVP. Core learning loop, auth (JWT + rotating refresh tokens), server-authoritative scoring, XP ledger, content pipeline, tests, Docker, and CI are in place. See [Project status & roadmap](#project-status--roadmap) and [Security posture](#security-posture--hardening-backlog).

---

## Table of contents

- [Features](#features)
- [Tech stack](#tech-stack)
- [Architecture](#architecture)
- [Solution layout](#solution-layout)
- [Domain model & database](#domain-model--database)
- [API reference](#api-reference)
- [Authentication & authorization](#authentication--authorization)
- [Learning engine](#learning-engine)
- [Content pipeline](#content-pipeline)
- [Getting started](#getting-started)
- [Configuration](#configuration)
- [Database & migrations](#database--migrations)
- [Testing](#testing)
- [Docker & deployment](#docker--deployment)
- [CI](#ci)
- [Security posture & hardening backlog](#security-posture--hardening-backlog)
- [Project status & roadmap](#project-status--roadmap)

---

## Features

- **Structured A1 German course** — data-driven lessons, exercises, and vocabulary.
- **Server-authoritative scoring** — the correct answer is looked up on the server and never trusted from the client.
- **Gamification** — XP, levels, streaks, and badges, all computed server-side.
- **Idempotent XP ledger** — append-only `XpEvent` ledger; `User.TotalXp` is a cached projection.
- **Progress analytics** — per-user progress dashboard and an activity heatmap derived from the ledger.
- **AI features (optional, metered)** — AI chat tutor (Azure OpenAI), pronunciation assessment (Azure Speech), and translation (Azure Translator). Authenticated + rate-limited.
- **Auth** — email/password registration and login, short-lived JWT access tokens, and rotating refresh tokens with reuse detection.
- **Guest trial** — read-only lesson content is available anonymously.

---

## Tech stack

| Layer | Technology |
|---|---|
| API framework | ASP.NET Core Web API, **.NET 8.0** |
| ORM / data | Entity Framework Core **8.0.12** (code-first), **SQL Server** |
| Auth | `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.12 (JWT HS256 + refresh tokens) |
| API docs | Swashbuckle (Swagger) 6.6.2 — Development only |
| Frontend | **Angular 21.2** (standalone components, signals), **SSR** via `@angular/ssr` + Express 5 |
| Frontend lang/tooling | TypeScript 5.9, RxJS 7.8 |
| Tests | xUnit + **SQLite in-memory** (backend); Vitest (frontend) |
| Containerization | Multi-stage Docker (API + Angular SSR), `docker-compose` (adds SQL Server) |
| CI | GitHub Actions (builds both stacks + runs backend tests) |
| Cloud services | Azure OpenAI, Azure Speech, Azure Translator (all optional) |

---

## Architecture

LinguaForge follows **Clean Architecture** with a strict dependency direction (outer layers depend on inner; the Domain depends on nothing):

```
┌─────────────────────────────────────────────────────────────┐
│  LinguaForge.API            (Controllers, Program.cs, DI,    │
│                              JWT config, CORS, rate limiting) │
├─────────────────────────────────────────────────────────────┤
│  LinguaForge.Application     (DTOs, service interfaces,       │
│                              *AppService orchestrators)       │
├─────────────────────────────────────────────────────────────┤
│  LinguaForge.Infrastructure  (EF Core DbContext, migrations,  │
│                              AuthService, Azure services,     │
│                              seeding)                         │
├─────────────────────────────────────────────────────────────┤
│  LinguaForge.Domain          (Entities only — no deps)        │
└─────────────────────────────────────────────────────────────┘
```

**Request flow (example — auth):**
`AuthController` → `AuthAppService` (orchestration) → `AuthService` (business logic: hashing, JWT minting, refresh rotation) → `LinguaForgeDbContext` → SQL Server.

Controllers never touch EF Core directly; all persistence goes through Infrastructure services.

**Frontend:** Angular standalone components with a single `AuthService` that keeps **only the short-lived access token in memory** (the refresh token lives in an `HttpOnly` cookie the browser manages — never in `localStorage`), a functional HTTP interceptor (attaches the bearer token and performs single-flight `401 → refresh → retry`), and a route guard. The session is restored on startup via a silent cookie-based refresh. All routes are server-rendered (`RenderMode.Server`).

---

## Solution layout

```
LinguaForgeApp/
├─ LinguaForge.API/                # ASP.NET Core Web API (entry point)
│  ├─ Controllers/                 # Auth, User, Lessons, Ai, Speech, Translation, Recommendations
│  ├─ Program.cs                   # DI, JWT, CORS, rate limiter, boot guard, pipeline
│  └─ appsettings.example.json     # config template (real appsettings.json is gitignored)
├─ LinguaForge.Application/        # DTOs, interfaces, use-case app services
│  ├─ DTOs/
│  ├─ Interface/
│  └─ UseCaseServices/
├─ LinguaForge.Domain/             # Entities (User, AuthCredential, RefreshToken, Lesson, Exercise, XpEvent, …)
│  └─ Entities/
├─ LinguaForge.Infrastructure/     # EF Core + concrete services
│  ├─ Configuration/               # JwtOptions, Azure*Options
│  ├─ Content/a1-course.json       # embedded A1 course content (source of truth)
│  ├─ Data/                        # DbContext, ContentSeeder, DbBootstrapper, design-time factory
│  ├─ Migrations/                  # EF Core migrations
│  └─ Services/                    # AuthService, Azure*Service, LessonService, UserProgressService, …
├─ LinguaForge.Tests/              # xUnit + SQLite in-memory
├─ src/                            # Angular 21 SSR frontend
│  └─ app/{core,features,layout,pages,shared}/
├─ docker-compose.yml              # SQL Server + API + web (SSR)
├─ Dockerfile                      # Angular SSR image
├─ LinguaForge.API/Dockerfile      # API image
├─ .github/workflows/ci.yml        # CI
└─ DEPLOYMENT.md                   # deployment guide
```

---

## Domain model & database

**Persistence is code-first EF Core** (no database-first, no stored procedures, no manual SQL scripts). The schema is defined by `LinguaForgeDbContext.OnModelCreating` + migrations, and applied at startup via `Database.MigrateAsync()`.

### Auth-related tables

| Table | Key columns | Notes |
|---|---|---|
| `Users` | `Id` (GUID, app-generated), `UserName`, `Email` (**unique index**), `TotalXp`, `Level`, `CurrentStreakDays`, `LastLessonCompletedOnUtc`, `CreatedAtUtc` | 1:1 → `AuthCredential`, 1:many → `RefreshTokens` |
| `AuthCredentials` | `UserId` (shared PK, 1:1), `PasswordHash`, `PasswordSalt`, `CreatedAtUtc` | PBKDF2-HMAC-SHA256, per-user salt; cascade-delete from `User` |
| `RefreshTokens` | `Id`, `UserId`, `TokenHash` (**unique index**), `ExpiresAtUtc`, `RevokedAtUtc?`, `ReplacedByTokenId?` | Only the SHA-256 **hash** is stored; rotation + reuse detection |

### Learning-domain tables (high level)

`Lessons`, `Exercises`, `VocabItems`, `LessonProgress`, `QuizAttempt`, `Badge`, `UserBadge`, `WeakTopic`, `Translation`, and the append-only **`XpEvent`** ledger (unique index on `(UserId, Reason, SourceId)` for idempotent XP).

> There is currently **no role/permission model** (no `Role`, `Permission`, module, or navigation-permission tables). Authorization is binary (authenticated vs anonymous). See [Authentication & authorization](#authentication--authorization).

---

## API reference

All endpoints are versioned under `/api/v1`. Swagger UI is available at `/swagger` in **Development** only.

### Auth — `/api/v1/auth`

| Method | Route | Auth | Description |
|---|---|---|---|
| `POST` | `/register` | Anonymous | Create an account; returns access + refresh tokens |
| `POST` | `/login` | Anonymous | Authenticate; returns access + refresh tokens |
| `POST` | `/refresh` | Cookie | Exchange the refresh **cookie** for a new token pair (rotates); no request body |
| `POST` | `/logout` | Cookie | Revoke the refresh token and clear the cookie; no request body |
| `GET` | `/me` | **Bearer** | Return the current user (from JWT) |

### User progress — `/api/v1/user` *(all Bearer)*

| Method | Route | Description |
|---|---|---|
| `GET` | `/progress` | Current user's progress (XP, level, streak, badges, heatmap) |
| `POST` | `/progress/lesson-complete` | Record a lesson completion (`{ lessonKey }`; identity from JWT) |

### Lessons — `/api/v1/lessons`

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/?level=A1` | Anonymous | List lessons for a level (guest trial) |
| `POST` | `/answer` | **Bearer** | Submit an answer; **scored server-side** |

### Recommendations — `/api/v1/recommendations` *(Bearer)*

| Method | Route | Description |
|---|---|---|
| `GET` | `/next` | Next recommended activity for the user |

### AI features *(Bearer + rate-limited `MeteredApi`)*

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/v1/ai/chat` | AI chat tutor (Azure OpenAI) |
| `POST` | `/api/v1/speech/assess` | Pronunciation assessment (multipart WAV upload, ≤ 15 MB; Azure Speech) |
| `POST` | `/api/v1/translation/Translate` | Translate text (Azure Translator) |
| `GET` | `/api/v1/translation/languages` | Supported languages |

### System

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/health` | Anonymous | Health check |

---

## Authentication & authorization

### Model

- **Authentication:** email + password. Passwords hashed with **PBKDF2-HMAC-SHA256, 100,000 iterations, 16-byte per-user salt, 32-byte hash**. Login comparison is timing-safe (`CryptographicOperations.FixedTimeEquals`).
- **Access token:** JWT, **HS256** (symmetric key from `Jwt:Key`), **15-minute** lifetime. Claims: `sub`, `email`, `NameIdentifier`, `Name`. Validated on `Issuer`, `Audience`, `Lifetime`, and `SigningKey` with a 2-minute clock skew.
- **Refresh token:** 256-bit CSPRNG value. **Only its SHA-256 hash is stored** (unique-indexed), **30-day** lifetime, **rotates on every use** (old token revoked and linked to its replacement). **Reuse detection:** presenting an already-revoked token revokes the user's entire active token chain. It is delivered as an **`HttpOnly; Secure; SameSite=Strict` cookie** scoped to `Path=/api/v1/auth` — never in the JSON body — so JavaScript (and therefore XSS) cannot read it. `refresh`/`logout` read the token from that cookie; `logout` clears it. *(LF-103)*
- **Login throttling:** repeated failed logins for the same **IP + email** are rate-limited and temporarily locked out (`429` + `Retry-After`), with a generic response that never reveals whether the account exists. Configurable via `Auth:Login:*`. *(LF-105)*
- **Boot guard:** the API refuses to start if `Jwt:Key` is missing, a known placeholder, or shorter than 32 bytes.

### Authorization

Authorization is **binary**: an endpoint is either `[Authorize]` (valid bearer token required) or anonymous. Inside every authorized endpoint the acting user is derived from the **`NameIdentifier` claim on the token** — never from a request body or route/query parameter. This means a caller can only ever act on their own data (no object-id is client-supplied for user-scoped resources), so there is effectively **no IDOR / broken-object-level-authorization surface**.

There is currently **no role-based access control** (RBAC), no permissions, no modules, and no navigation-menu permissions. All authenticated users have identical privileges. (Adding an RBAC + navigation-permission layer is a candidate next step — see roadmap.)

### Flow

```
Register/Login ──► access JWT (15 min) in memory + refresh token (30 days) in HttpOnly cookie
     │
     ▼
SPA sends "Authorization: Bearer <jwt>" on requests (refresh cookie sent only on /auth calls)
     │
     ▼
On 401 ──► POST /auth/refresh (single-flight, withCredentials; cookie carries the token)
     │        ──► new access token + rotated refresh cookie ──► retry original request
     ▼
Logout ──► POST /auth/logout ──► refresh token revoked + cookie cleared
```

> On reload the access token (memory-only) is gone, but the `HttpOnly` refresh cookie survives, so the SPA silently refreshes on startup to restore the session. Only a **refresh** failure logs the user out — a transient error on the *retried* request does not.

---

## Learning engine

- **Server-authoritative scoring:** `POST /api/v1/lessons/answer` looks up the correct answer on the server; the client's submitted answer is only compared, never trusted.
- **XP is idempotent:** all XP flows through `IUserProgressService.AwardXpAsync` and is recorded in the append-only `XpEvent` ledger with a unique index on `(UserId, Reason, SourceId)`. `User.TotalXp` is a cached projection of the ledger. Reasons include `ExerciseFirstCorrect`, `LessonCompletion`, and `BadgeBonus`.
- **Lesson completion:** `POST /api/v1/user/progress/lesson-complete` takes only `{ lessonKey }`; the title, XP, and accuracy are all derived server-side. Completion is idempotent.
- **Badges & heatmap:** badges unlock server-side; the activity heatmap is derived from the XP ledger.

---

## Content pipeline

Course content lives in **`LinguaForge.Infrastructure/Content/a1-course.json`** (embedded, 6 A1 lessons). On startup, `ContentSeeder` **idempotently upserts** lessons/exercises/vocab by natural key. Adding or editing content is pure JSON editing — no code changes and no manual SQL.

---

## Getting started

### Prerequisites

- .NET 8 SDK
- Node.js 20+ and npm 10+
- SQL Server (local instance, LocalDB, or the Dockerized one via `docker-compose`)

### Backend (API)

```bash
# From the repo root
cd LinguaForge.API

# 1. Provide config (copy the template and fill in real values, OR use user-secrets / env vars)
cp appsettings.example.json appsettings.json    # then edit — file is gitignored

# 2. Set a real JWT signing key (>= 32 bytes) or the app refuses to start
#    (dev: user-secrets recommended)
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)"

# 3. Run — migrations are applied and the A1 course is seeded automatically on boot
dotnet run
```

- API: `https://localhost:<port>` (see `Properties/launchSettings.json`)
- Swagger: `/swagger` (Development)
- Health: `/health`

### Frontend (Angular SSR)

```bash
# From the repo root
npm install
npm start           # dev server at http://localhost:4200
```

The dev frontend expects the API at the URL configured in `src/enviornment/environment.ts`. CORS defaults to allowing `http://localhost:4200`.

---

## Configuration

Configuration comes from `appsettings.json` (gitignored) or environment variables. ASP.NET maps `__` (double underscore) in env-var names to nested keys.

| Key / Env var | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` / `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `Jwt:Key` / `Jwt__Key` | **JWT signing secret, ≥ 32 bytes** (enforced by boot guard) |
| `Jwt:Issuer`, `Jwt:Audience` | Token issuer / audience |
| `Jwt:ExpiryMinutes` | Access-token lifetime (default 15) |
| `Jwt:RefreshTokenDays` | Refresh-token lifetime + refresh-cookie `Max-Age` (default 30) |
| `Cors:AllowedOrigins:0`, `:1`, … | Allowed browser origins (the frontend URL). Credentials are allowed for these origins so the refresh cookie can round-trip |
| `Auth:Login:PermitLimit` | Failed logins per window (per IP+email) before lockout (default 5) |
| `Auth:Login:WindowSeconds` | Rolling window for counting failures (default 60) |
| `Auth:Login:LockoutSeconds` | Lockout duration once the limit trips (default 300) |
| `AzureOpenAI:*`, `AzureSpeech:*`, `AzureTranslator:*` | Optional; only needed for the metered AI features |

The real `appsettings.json` is **gitignored**; only `appsettings.example.json` (placeholders) is committed. Never commit secrets.

---

## Database & migrations

- **Code-first EF Core.** Migrations live in `LinguaForge.Infrastructure/Migrations/`.
- On startup, `DbBootstrapper.InitializeAsync` runs `Database.MigrateAsync()` (applies pending migrations, creating the schema on first run), then `ContentSeeder` seeds the A1 course.
- A design-time factory (`LinguaForgeDbContextFactory`) lets the EF tooling scaffold without booting the API.

```bash
# Add a migration (run from the repo root; startup project is the API)
dotnet ef migrations add <Name> \
  --project LinguaForge.Infrastructure \
  --startup-project LinguaForge.API

# Apply migrations manually (normally applied automatically on boot)
dotnet ef database update \
  --project LinguaForge.Infrastructure \
  --startup-project LinguaForge.API
```

---

## Testing

Backend tests use **xUnit** with **SQLite in-memory** (no SQL Server needed):

```bash
dotnet test
```

Coverage includes `AuthService` (incl. refresh rotation + reuse detection), `ContentSeeder` idempotency, server-side scoring, server-authoritative XP, and XP/completion idempotency, plus integration tests (`WebApplicationFactory`) for the JWT boot guard, rate-limiter user partitioning, the `HttpOnly` refresh-cookie flow, and login throttling/lockout.

Frontend uses Vitest:

```bash
npm test
```

---

## Docker & deployment

Two images — the **.NET API** and the **Angular SSR frontend** — plus **SQL Server**. All secrets are supplied at runtime via env vars.

```bash
# A real signing key (>= 32 bytes) is required or the API refuses to start.
export JWT_KEY="$(openssl rand -base64 48)"
export DB_PASSWORD='Your_strong_passw0rd!'

docker compose up --build
```

- Frontend: `http://localhost:4000`
- API: `http://localhost:8080` (health at `/health`)

The API applies EF migrations and seeds the course on startup, so the database is ready on first boot.

See **[DEPLOYMENT.md](DEPLOYMENT.md)** for the recommended Azure-native production stack (Container Apps + Azure SQL + Key Vault) and the pre-production checklist.

> ⚠️ **SSR caveat:** the browser API base URL is baked at build time (`angular.json` `fileReplacements`), so SSR uses the same URL. Inside `docker compose`, `localhost:8080` is not reachable from the `web` container, so the first server render of data-fetching pages may fail and recover on the client. Non-issue behind a single production domain; documented as a follow-up.

---

## CI

`.github/workflows/ci.yml` runs on every push/PR to `main`:

- **backend** — restore, build (Release), and run the xUnit suite.
- **frontend** — `npm ci` and a production build.

---

## Security posture & hardening backlog

### Security hardening — 2026-07 (Sprint 1)

Sprint 1 ("Secure Auth Foundations" — see [SPRINT-1-TICKETS.md](SPRINT-1-TICKETS.md) and [DELIVERY-PLAN.md](DELIVERY-PLAN.md)) closed the High-severity auth findings:

| Area | Before | After |
|---|---|---|
| JWT signing key (LF-101) | Docker shipped a working default key that slipped past the boot guard while running as `Production`. | `JwtKeyGuard` rejects missing/short/placeholder keys (any hyphen/underscore/case variant); compose has **no** default — `JWT_KEY` is required. |
| Rate-limiter ordering (LF-102) | `UseRateLimiter()` ran before auth, so per-user throttling silently degraded to per-IP. | Limiter runs **after** `UseAuthentication()`/`UseAuthorization()`; metered endpoints partition per authenticated user. |
| Refresh-token storage (LF-103/104) | 30-day refresh token stored in `localStorage` and returned in the JSON body (XSS-exfiltratable). | Delivered as an `HttpOnly; Secure; SameSite=Strict` cookie scoped to `/api/v1/auth`; SPA keeps only the in-memory access token; silent cookie refresh on startup. |
| Login brute-force (LF-105) | No throttling/lockout on `/auth/login`. | Per IP+email throttle + temporary lockout (`429` + `Retry-After`), generic (non-enumerating) responses; configurable via `Auth:Login:*`. |

> **Cookies require HTTPS.** The `Secure` refresh cookie is only sent over TLS — see [DEPLOYMENT.md](DEPLOYMENT.md#cookies--https-requirement).

**Implemented:**

- ✅ PBKDF2 password hashing with per-user salt; timing-safe comparison.
- ✅ Short-lived access JWT (15 min) + rotating refresh tokens (hash-at-rest, reuse detection).
- ✅ JWT boot guard v2 (`JwtKeyGuard`) — rejects missing, `< 32`-byte, and any placeholder/default key (incl. the former compose default, in any hyphen/underscore/case variant); `docker-compose.yml` ships **no** working default (`JWT_KEY` is required). *(LF-101)*
- ✅ `[Authorize]` on all user-scoped and metered endpoints; user identity always from the token (no IDOR surface).
- ✅ Rate limiting on metered Azure endpoints, with the limiter ordered **after** authentication so throttling partitions per authenticated user; config-driven CORS (origin-restricted; credentials allowed only for the allow-listed origins so the refresh cookie can round-trip). *(LF-102)*
- ✅ Refresh token delivered as an `HttpOnly; Secure; SameSite=Strict` cookie scoped to `/api/v1/auth` (not `localStorage`, not the JSON body) — the SPA holds only the short-lived access token in memory. *(LF-103 / LF-104)*
- ✅ Login brute-force throttling: failed logins are rate-limited + temporarily locked out per IP+email (`429` + `Retry-After`), responses stay generic. *(LF-105)*
- ✅ Secrets via env vars / user-secrets; real `appsettings.json` gitignored.
- ✅ `/health` endpoint; login returns a generic error (no user enumeration on login).

**Known issues to address before production** (identified in an architecture/security review):

| Priority | Issue |
|---|---|
| ~~High~~ **Fixed (LF-101)** | ~~`docker-compose.yml` shipped a default `Jwt__Key` that slipped past the boot guard while running as `Production`.~~ Resolved: `JwtKeyGuard` rejects all placeholder/default keys and compose has no working default. |
| ~~High~~ **Fixed (LF-102)** | ~~Rate limiter ran before authentication, silently degrading per-user throttling to per-IP.~~ Resolved: `UseRateLimiter()` now runs after `UseAuthentication()`/`UseAuthorization()`. |
| ~~High~~ **Fixed (LF-103/104)** | ~~Frontend stored the long-lived refresh token in `localStorage` (XSS-exfiltration risk).~~ Resolved: delivered as an `HttpOnly; Secure; SameSite=Strict` cookie scoped to `/api/v1/auth`; the SPA holds only the access token in memory. |
| ~~Medium~~ **Fixed (LF-105)** | ~~No rate limiting / lockout on `/auth/login` (brute-force / credential-stuffing exposure).~~ Resolved: per IP+email throttle + temporary lockout (`429` + `Retry-After`), generic responses. |
| **Medium** | Registration reveals whether an email exists (enumeration) and a concurrent-registration race can surface a raw 500 instead of a clean 409. |
| **Medium** | No security headers / HSTS (`X-Content-Type-Options`, frame-ancestors/CSP, `Referrer-Policy`, `Strict-Transport-Security`). |
| **Medium** | Access token cannot be revoked before its 15-min expiry (no `jti`/denylist; logout only revokes the refresh token). |
| **Low** | `Issuer` == `Audience` in the example config; PBKDF2 iterations (100k) below current OWASP guidance; `UserName` not uniquely indexed; no purge job for expired refresh tokens; no centralized `ProblemDetails` exception handling. |

---

## Project status & roadmap

**Done:** deployable A1 German MVP — clean-architecture API, JWT + refresh-token auth, server-authoritative scoring, idempotent XP ledger, badges/heatmap, JSON content pipeline, xUnit tests, Docker + `docker-compose`, and CI.

**Next up:**

- **Phase B:** real lesson foreign keys, server-authoritative lesson unlock (`UserLessonProgress.Status`), streak/daily-goal, analytics events, migration/readiness split, exercise-key content matching.
- **Phase C:** platformization — spaced-repetition knowledge items, grammar concepts, a CMS, and A2 content.
- **Security:** address the High-severity hardening items above; consider adding an **RBAC + navigation-permission layer** (roles, permissions, permission-based policies, and a navigation endpoint) if an admin/content-management surface is introduced.

---

*This README reflects the state of the codebase as of the latest commit and an architecture review conducted in July 2026.*
