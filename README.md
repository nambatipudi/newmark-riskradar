# NewmarkRiskRadar

[![CI](https://github.com/nambatipudi/newmark-riskradar/actions/workflows/ci.yml/badge.svg)](https://github.com/nambatipudi/newmark-riskradar/actions/workflows/ci.yml)

An active underwriting and triage engine for commercial real estate loan portfolios. RiskRadar ingests raw loan data, runs deterministic financial mathematics, and categorizes loans into actionable risk tiers — Critical Default, Elevated Risk, and Performing — with a Pro-Forma Stress Test Simulator.

## Documentation

- [Business Case](BUSINESS_CASE.md) — Why RiskRadar? Rationale and business context
- [Prompts](PROMPTS.md) — Claude Code prompts used during development

## Projects

Two strictly decoupled projects with independent build pipelines, talking only over HTTP.

- **[NewmarkRiskRadar-API](NewmarkRiskRadar-API/)** — .NET 8 backend, Clean Architecture. The `Domain` project has zero external dependencies; all money and ratios are `decimal`, never floating point.
- **[NewmarkRiskRadar-UI](NewmarkRiskRadar-UI/)** — Next.js 16 App Router frontend. React Server Components stream the data, Zod validates every payload, and all filter state lives in the URL so a scenario is shareable.

---

## Prerequisites

| Tool | Version | Needed for |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0+ | Running the API without Docker |
| [Node.js](https://nodejs.org) | 20+ | Running the UI without Docker |
| Rancher Desktop or Docker Desktop | any current | Running the whole stack in containers |

Install on **macOS**:

```bash
brew install --cask dotnet-sdk
brew install node
```

Install on **Windows** (PowerShell):

```powershell
winget install Microsoft.DotNet.SDK.8
winget install OpenJS.NodeJS.LTS
```

---

## Option 1: Run everything with Docker (recommended)

Identical on macOS and Windows. Run from the repository root:

```bash
docker compose up --build
```

| Service | URL |
|---|---|
| Dashboard (UI) | http://localhost:3000 |
| API | http://localhost:5000 |
| Swagger | http://localhost:5000/swagger |
| Health check | http://localhost:5000/health |

Stop the stack with `Ctrl+C`, then `docker compose down`. The SQLite database lives in the
`sqlite_data` named volume, so it survives restarts; `docker compose down -v` wipes it.

> **macOS note:** port 5000 is also used by the AirPlay Receiver. If the API appears to start but
> returns `403`, turn off *System Settings → General → AirDrop & Handoff → AirPlay Receiver*, or
> remap the port to `5001:8080` in `docker-compose.yml`.

---

## Option 2: Run locally without Docker

Two terminals, API first.

### Terminal 1 — API

**macOS / Linux**

```bash
cd NewmarkRiskRadar-API
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5000 \
  dotnet run --project src/Newmark.RiskRadar.Api/Newmark.RiskRadar.Api.csproj --no-launch-profile
```

**Windows (PowerShell)**

```powershell
cd NewmarkRiskRadar-API
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5000"
dotnet run --project src\Newmark.RiskRadar.Api\Newmark.RiskRadar.Api.csproj --no-launch-profile
```

On first start the API creates `src/Newmark.RiskRadar.Api/App_Data/riskradar.db` and seeds
50 synthetic institutional CRE loans. Delete that file to force a reseed.

### Terminal 2 — UI

**macOS / Linux**

```bash
cd NewmarkRiskRadar-UI
cp .env.example .env.local
npm install
npm run dev
```

**Windows (PowerShell)**

```powershell
cd NewmarkRiskRadar-UI
Copy-Item .env.example .env.local
npm install
npm run dev
```

Open http://localhost:3000. The UI proxies `/api/*` to the API through a Next.js rewrite, so the
browser never calls the API host directly and there is no CORS to configure.

---

## Running the tests

**222 tests** across both projects.

| Suite | Count | Command |
|---|---|---|
| Domain (pure financial math, no I/O) | 81 | `dotnet test` |
| Application (mocked repository + fixed clock) | 34 | `dotnet test` |
| API (integration, real HTTP + SQLite) | 18 | `dotnet test` |
| UI (Vitest + React Testing Library) | 89 | `npm test` |

**macOS / Linux**

```bash
cd NewmarkRiskRadar-API && dotnet test Newmark.RiskRadar.sln
cd ../NewmarkRiskRadar-UI && npm test && npm run lint && npm run typecheck
```

**Windows (PowerShell)**

```powershell
cd NewmarkRiskRadar-API; dotnet test Newmark.RiskRadar.sln
cd ..\NewmarkRiskRadar-UI; npm test; npm run lint; npm run typecheck
```

### Continuous integration

[`.github/workflows/ci.yml`](.github/workflows/ci.yml) runs on every push and pull request to
`main`: the .NET suites in Release, then the UI lint, typecheck, tests and production build, and
finally a build of both container images to prove the Dockerfiles still work. Test results are
uploaded as an artefact.

---

## What to look at first

1. **Stress Test Simulator** — drag the rate slider. The server re-prices every loan at that
   pro-forma take-out rate, holding NOI flat, and re-runs triage. At 9.5% the book's weighted DSCR
   falls from 1.27x to 0.91x and critical exposure climbs from $1.1B to $1.8B.
2. **Shareable scenarios** — every filter is a URL parameter, so
   `/?assetClass=Office&stressRate=8.0&risk=Critical` can be bookmarked or emailed.
3. **Streaming** — the shell and controls render immediately; the metrics, chart and table stream
   in behind independent Suspense boundaries, each with its own error boundary.
4. **Deep dive** — click any row for the full position, including why it carries its risk rating.
5. **Sync Tape** — simulates a servicing tape drop: the API rewrites the book inside a transaction
   and the dashboard refreshes.

## Architecture notes

- `Newmark.RiskRadar.Domain` has no `PackageReference` and no `ProjectReference` at all. EF Core and
  ASP.NET cannot leak into the financial rules.
- Every monetary value and ratio is `decimal`. Amortization uses a decimal power loop rather than
  `Math.Pow`, and DSCR is persisted as integer basis points because SQLite stores `decimal` as TEXT,
  which sorts lexicographically.
- Divide-by-zero is a guarded domain concern: `InvalidFinancialInputException` carries the offending
  parameter name and is mapped to a `400 application/problem+json` by the global exception middleware.
- The UI treats the API as untrusted input. Every response is parsed through a Zod schema before it
  reaches a component.

## API reference

| Method | Endpoint | Purpose |
|---|---|---|
| `GET` | `/api/v1/portfolio/summary` | Book volume, weighted DSCR, WALT, critical exposure |
| `GET` | `/api/v1/analytics/maturity-wall` | Balance by maturity year, split by risk bucket |
| `GET` | `/api/v1/loans/triage` | Paginated loans with risk badges |
| `POST` | `/api/v1/system/sync` | Regenerate the servicing tape |

All three `GET` endpoints accept `?stressRate=8.5` (a percentage) to apply a pro-forma scenario.
`/api/v1/loans/triage` also accepts `page`, `pageSize`, `risk`, `assetClass` and `search`.
