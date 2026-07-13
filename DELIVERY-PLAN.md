# LinguaForge — Delivery Plan (Sprint-wise Backlog)

A phased, sprint-ready execution plan: **secure & stabilize the MVP → strengthen the learning core → prepare for platform scale.** Every security ticket references the exact code location it touches.

> **Sequencing principle:** production-hardening High items ship before feature work (clearest attack path, highest payoff). RBAC/admin is deliberately deferred until an internal management surface actually exists.

---

## Conventions & assumptions

| Item | Value |
|---|---|
| Sprint length | **2 weeks** |
| Cadence | **6 sprints / 12 weeks** (matches the 30-60-90 rhythm) |
| Capacity assumption | **~20 story points / sprint** for one focused engineer (rescale to your team) |
| Point scale | Fibonacci: **1** (trivial) · **2** (small) · **3** (½–1 day) · **5** (2–3 days) · **8** (~a week) · **13** (split it) |
| Story-point meaning | Relative effort + uncertainty, **not** hours |

**Definition of Done (applies to every ticket):**
- [ ] Code merged to `main` via PR; CI green (build + tests both stacks).
- [ ] Unit/integration tests added or updated for the change.
- [ ] No new High/Medium security finding introduced.
- [ ] Docs updated where behavior/config changed (`README.md`, `DEPLOYMENT.md`).
- [ ] Verified in a running app (not just tests) for anything with a runtime surface.

**Ticket ID scheme:** `LF-1xx` Security · `LF-2xx` Learning Engine · `LF-3xx` Platform · `LF-4xx` Operability · `LF-5xx` Architecture/Test · `LF-6xx` Frontend.

---

## Sprint summary

| Sprint | Weeks | Theme | Points |
|---|---|---|---|
| **S1** | 1–2 | Security — High items (Production Security Gate, part 1) | 21 |
| **S2** | 3–4 | Security — Medium items + regression | 19 |
| **S3** | 5–6 | Learning Engine Phase B, part 1 | 19 |
| **S4** | 7–8 | Learning Engine Phase B, part 2 | 21 |
| **S5** | 9–10 | Platformization groundwork | 19 |
| **S6** | 11–12 | Platform + observability + readiness | 21 |

**Release gate:** the app is **not** marked production-ready until every **S1** ticket is closed.

---

## Sprint 1 (Weeks 1–2) — Production Security Gate, part 1  ·  *21 pts*

**Goal:** close the three High-severity issues so the app can be safely exposed.

### LF-101 · Harden JWT signing-key handling (remove working default, fail closed) · `3`
**Type:** Security (High)
**Context:** `docker-compose.yml:29` ships `Jwt__Key: "…change-me-32b+"` (hyphens), which evades the boot guard's `Contains("CHANGE_ME")` underscore check at `Program.cs:58-65` and is ≥32 bytes, while the service runs `ASPNETCORE_ENVIRONMENT: Production` (`docker-compose.yml:26`). A `docker compose up` without `JWT_KEY` boots production with a committed key → **token forgery / full `[Authorize]` bypass.**
**Acceptance criteria:**
- [ ] `docker-compose.yml` has **no working default** for `Jwt__Key` (e.g. `${JWT_KEY:?JWT_KEY must be set}` so compose fails fast if unset).
- [ ] Boot guard rejects: empty, `< 32 bytes`, **and** any known placeholder including the former docker default and case/format variants of "change me".
- [ ] Unit test asserts the guard throws for each rejected class and passes for a valid random key.
- [ ] `README.md` / `DEPLOYMENT.md` reflect the no-default behavior.

### LF-102 · Reorder rate-limiter after authentication · `2`
**Type:** Security (High)
**Context:** `Program.cs:156-158` calls `UseRateLimiter()` **before** `UseAuthentication()`, so the `MeteredApi` partition key `User.FindFirstValue(NameIdentifier)` (`Program.cs:124`) is always null and per-user throttling silently degrades to per-IP on the billable Azure endpoints.
**Acceptance criteria:**
- [ ] `UseRateLimiter()` runs **after** `UseAuthentication()` (and before/after `UseAuthorization()` consistently).
- [ ] Integration test: two authenticated users sharing one source IP get **independent** rate buckets; an anonymous caller falls back to per-IP.
- [ ] No regression in the metered endpoints' `429` behavior.

### LF-103 · Move refresh token to `HttpOnly; Secure; SameSite=Strict` cookie · `8`
**Type:** Security (High) · Backend + Frontend
**Context:** The 30-day refresh token is stored in browser `localStorage` (`src/app/core/services/auth.service.ts`) and returned in `AuthResponseDto` — XSS-exfiltratable → persistent account takeover.
**Acceptance criteria:**
- [ ] Server sets the refresh token as an `HttpOnly; Secure; SameSite=Strict` cookie scoped to the `/api/v1/auth` path on `register`/`login`/`refresh`.
- [ ] `refresh` and `logout` read the token from the cookie (body fallback removed or deprecated).
- [ ] `AuthResponseDto` no longer returns the raw refresh token to JS; the access token stays in memory (not `localStorage`).
- [ ] Frontend `auth.service.ts` no longer persists the refresh token; refresh flow works via cookie (credentials included on `/auth` calls only).
- [ ] Logout clears the cookie server-side.
- [ ] CORS updated only as needed (credentials allowed **only** for the auth origin/path).

### LF-601 · Narrow 401→refresh→retry so transient errors don't force logout · `3`
**Type:** Frontend (supports LF-103)
**Context:** In `auth.interceptor.ts` the `catchError` wraps both `refresh()` and the retried request, so any transient error on retry ejects an authenticated user to `/welcome`.
**Acceptance criteria:**
- [ ] Only a **refresh failure** triggers logout+redirect; a failed *retried* request surfaces its own error without logging out.
- [ ] Single-flight refresh preserved; add debug logging/metric around refresh attempts.
- [ ] Test covers: transient 500 on retry → user stays logged in.

### LF-502 · Integration tests for token rotation, replay, and cookie flow · `5`
**Type:** Test
**Acceptance criteria:**
- [ ] Test: refresh rotates the token and revokes the old one.
- [ ] Test: replaying a revoked token revokes the whole active chain (reuse detection).
- [ ] Test: cookie-based refresh issues a new pair; logout invalidates it.

---

## Sprint 2 (Weeks 3–4) — Security Medium + regression  ·  *19 pts*

### LF-104 · Login brute-force protection (rate limit + temporary lockout) · `5`
**Type:** Security (Medium)
**Context:** `AuthController` login/register have no throttling; only Azure endpoints use `MeteredApi`.
**Acceptance criteria:**
- [ ] Per-IP **and** per-account sliding/fixed window limiter on `/auth/login` (and `/auth/register`).
- [ ] Progressive backoff or temporary lockout after N failures; returns `429` with `Retry-After`.
- [ ] Lockout is observable in logs; test covers threshold + reset.

### LF-105 · Registration anti-enumeration + race handling · `3`
**Type:** Security (Medium)
**Context:** Register returns "An account with this email already exists" (enumeration); a concurrent-registration race can surface a raw 500 instead of a clean 409.
**Acceptance criteria:**
- [ ] Duplicate-email behavior decided and implemented (generic response **or** documented accepted trade-off).
- [ ] `DbUpdateException` from the unique index is caught → `409 Conflict` (no 500 on race).
- [ ] Test covers the concurrent-insert race.

### LF-106 · Security headers + HSTS middleware · `3`
**Type:** Security (Medium)
**Acceptance criteria:**
- [ ] `UseHsts()` in Production; `Strict-Transport-Security` present.
- [ ] `X-Content-Type-Options: nosniff`, frame-ancestors/CSP (or `X-Frame-Options`), `Referrer-Policy` set.
- [ ] Verified via response-header assertion test.

### LF-107 · Access-token early revocation (jti + short denylist) or documented accepted risk · `5`
**Type:** Security (Medium)
**Context:** Logout revokes only the refresh token; the stateless JWT stays valid up to ~17 min (incl. skew). No `jti`/denylist.
**Acceptance criteria:**
- [ ] `jti` claim added; a short-TTL denylist (memory/cache) checked on requests, **or** a written decision to accept the 15-min window with rationale.
- [ ] "Logout all devices" path revokes all refresh tokens for the user.
- [ ] Test covers chosen behavior.

### LF-108 · Centralized exception handling with `ProblemDetails` · `3`
**Type:** Security/Backend (Medium)
**Context:** `AiController`/`SpeechController` don't catch exceptions; error shape is inconsistent across controllers.
**Acceptance criteria:**
- [ ] Global exception handler returns RFC-7807 `ProblemDetails`; no stack traces in Production.
- [ ] Domain exceptions map to correct status codes centrally (409/401/404/400).
- [ ] Controllers no longer need bespoke try/catch for the common cases.

---

## Sprint 3 (Weeks 5–6) — Learning Engine Phase B, part 1  ·  *19 pts*

### LF-201 · Real lesson foreign keys (`Lesson.Id` + FKs) · `5`
**Type:** Backend
**Acceptance criteria:**
- [ ] `Exercise` and `LessonProgress` reference `Lesson` via real FK (not string key only).
- [ ] Migration written; `ContentSeeder` still idempotent against natural key.
- [ ] Existing tests pass; seeding verified.

### LF-202 · Server-authoritative lesson unlock (`UserLessonProgress.Status`) · `8`
**Type:** Backend + Frontend
**Context:** Unlock currently relies on client index-based lock logic in the lesson map.
**Acceptance criteria:**
- [ ] Server tracks per-user lesson status (Locked/Available/Completed) and is the sole authority.
- [ ] Lesson map consumes server status; client index lock logic removed.
- [ ] Attempting a locked lesson via API is rejected server-side.
- [ ] Tests cover unlock progression.

### LF-205 · Exercise-key ↔ content integrity validation · `3`
**Type:** Backend
**Acceptance criteria:**
- [ ] Submitted `exerciseKey`/`ExerciseId` validated against content source; unknown keys → `404`/`400` (no silent scoring).
- [ ] Startup/seed validation flags content referencing missing keys.

### LF-501 · Architecture dependency-direction tests · `3`
**Type:** Architecture/Test
**Acceptance criteria:**
- [ ] Tests (e.g. NetArchTest) assert Domain has no outward deps, Application doesn't reference API, controllers don't reference EF/`DbContext` directly.
- [ ] CI fails on a violated boundary.

---

## Sprint 4 (Weeks 7–8) — Learning Engine Phase B, part 2  ·  *21 pts*

### LF-203 · Streak + daily-goal engine (`StreakLog` / `DailyGoal`) · `5`
**Acceptance criteria:**
- [ ] Streak computed server-side from activity; daily-goal target + progress tracked.
- [ ] Timezone handling defined; edge cases (missed day, multiple sessions) tested.

### LF-204 · Analytics event model + `POST /api/v1/events` · `5`
**Acceptance criteria:**
- [ ] Typed event schema (lesson start/complete, answer, streak, etc.); endpoint validates + persists.
- [ ] Pluggable sink (e.g. PostHog) behind an interface; no PII leakage.
- [ ] Tests cover ingestion + validation.

### LF-206 · Migration job + readiness split · `5`
**Type:** Backend/Ops
**Context:** Migrations run in-process on every boot (`DbBootstrapper` → `MigrateAsync`), which is risky under horizontal scale.
**Acceptance criteria:**
- [ ] Migrations run as a discrete step/job (not implicitly on every instance start).
- [ ] Liveness vs readiness endpoints separated; readiness reflects DB/migration state.

### LF-602 · SSR-safe API base URL for containerized envs · `3`
**Type:** Frontend
**Context:** API base URL is baked at build time, so SSR inside `docker compose` can't reach `localhost:8080`.
**Acceptance criteria:**
- [ ] Server-side render uses a runtime-injected internal API URL; browser uses the public URL.
- [ ] First SSR render of data pages (e.g. lesson map) succeeds in compose.

### LF-603 · Loading / empty / failure states for dashboard + lesson flows · `3`
**Type:** Frontend/UX
**Acceptance criteria:**
- [ ] Every data-driven view has explicit loading, empty, and error states.
- [ ] Errors are actionable (retry) and don't blank the screen.

---

## Sprint 5 (Weeks 9–10) — Platformization groundwork  ·  *19 pts*

### LF-301 · Spaced-repetition entities + scheduling (`UserKnowledgeItem` / SR) · `8`
**Acceptance criteria:**
- [ ] Knowledge-item model + review scheduling (SM-2 or chosen algorithm) designed and implemented behind the Application layer.
- [ ] Due-items query + review-outcome update; tests cover interval progression.

### LF-302 · Grammar concept model (`GrammarConcept`) · `3`
**Acceptance criteria:**
- [ ] Concepts modeled and linkable to lessons/exercises; seed a few A1 concepts.

### LF-305 · Richer recommendation signals · `5`
**Acceptance criteria:**
- [ ] Recommendations factor weak topics, SR due-items, and streak/goal state (not a single signal).
- [ ] Deterministic, testable ranking; tests cover ordering.

### LF-403 · Health / readiness / liveness split · `3`
**Type:** Operability
**Acceptance criteria:**
- [ ] Distinct `/health/live` and `/health/ready`; readiness checks DB connectivity.
- [ ] Compose/deploy probes wired to the right endpoints.

---

## Sprint 6 (Weeks 11–12) — Platform + observability + readiness  ·  *21 pts*

### LF-303 · CMS / content-authoring direction (design spike) · `5`
**Acceptance criteria:**
- [ ] Written decision: keep JSON-in-repo vs DB-backed CMS vs headless CMS, with trade-offs and a migration path.
- [ ] Chosen approach has a thin proof-of-concept or clear next-sprint ticket set.

### LF-304 · A2 course structure · `5`
**Acceptance criteria:**
- [ ] A2 content schema + skeleton (units/lessons) defined; seeder handles multiple levels.
- [ ] Level selection works end-to-end for A1 + A2 skeleton.

### LF-401 · Structured audit / security logs · `3`
**Acceptance criteria:**
- [ ] Auth events (login success/fail, refresh, logout, lockout) logged structured with correlation id; no secrets/PII in logs.

### LF-402 · Token-chain monitoring + suspicious-refresh alerts · `3`
**Acceptance criteria:**
- [ ] Reuse-detection events emit a metric/alert; dashboard or log-based alert defined.

### LF-404 · Deployment environment validation checks · `2`
**Acceptance criteria:**
- [ ] Startup validates required config (JWT key, connection string, CORS origins) and fails fast with a clear message in Production.

### LF-405 · Expand CI with security + config validation · `3`
**Acceptance criteria:**
- [ ] CI runs dependency/secret scanning and a config-lint (e.g. rejects committed working secrets).
- [ ] Pipeline fails on High findings.

---

## Deferred (not yet scheduled)

- **RBAC + navigation-permission layer** (roles, permissions, permission-based policies, `/navigation` endpoint) — schedule **only** when an admin/content-management surface is introduced. Design already outlined in the architecture review (permission-based policies over role-only or full external OIDC).
- Additional locales/languages beyond German.
- Component-library expansion (avoid premature investment).

---

## Per-epic planning template

For each epic, capture before starting:

- **Business outcome** — what user/operator value this unlocks.
- **Technical scope** — systems/files touched; explicitly out-of-scope.
- **Risks** — what could regress; mitigation.
- **Test coverage required** — unit/integration/e2e expectations.
- **Deployment / rollback notes** — migration order, feature flags, revert path.

---

*Plan aligns to the 30-60-90 roadmap and the security findings from the July 2026 architecture review. Story points assume one focused engineer at ~20 pts/sprint; rescale for your team without changing the sequencing.*
