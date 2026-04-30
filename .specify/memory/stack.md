# Stack — TallerPro (workspace Washer)

> Versiones confirmadas en P1 (fundaciones). Cambios requieren ADR.

## Runtime

- **.NET 9** (SDK `9.0.x`, roll-forward `latestFeature` vía `global.json`).
- `LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`.

## Frontend — cliente de producto

- **Blazor Hybrid** (MAUI + BlazorWebView).
- Targets: **Windows** (MSIX + feed propio) y **Android** (APK + Play Console canal cerrado).
- **MudBlazor** — 100% de la UI.
- **SCSS** para tokens y estilos custom. Pipeline: `DartSassBuilder` (o equivalente). Tokens en `src/TallerPro.Components/Styles/_variables.scss`.
- **SignalR** para realtime; Redis backplane en beta+.

## Frontend — marketing + admin

- **Marketing site**: ASP.NET Core **MVC** .NET 9 + **Bootstrap 5** + SCSS + Cloudflare Free.
- **Super-Admin Portal** (`admin.tallerpro.mx`): MVC + MudBlazor, protegido por **Cloudflare Access** (Zero Trust).

## Backend / dominio

- **ASP.NET Core** — híbrido: Minimal APIs para cliente Hybrid, MVC para admin.
- **Mediator** (Martin Othamar, MIT, source generators). NO MediatR.
- **Mapster**. NO AutoMapper.
- **FluentValidation**.
- **Polly v8** (retry, circuit breaker, bulkhead).
- **Stripe.net** (Billing + Meters).

## Persistencia

- **SQL Server** (Azure SQL Database `S0→S1→S2→GP vCore→MI` según fase).
- **EF Core 9** con Global Query Filters por `TenantId` + **SQL Server Row-Level Security (RLS)**.
- Multitenancy: **Single DB + TenantId discriminator + RLS** (defense-in-depth 8 capas).
- **SQLite** local en cliente Hybrid (`TallerPro.LocalDb`) para modo offline + Outbox pattern.
- **Cloudflare R2** para blobs (fotos custodia, XMLs CFDI, backups) — zero egress.

## Observabilidad

- **Serilog** (único logger) + enrichers custom (`TenantId`, `BranchId`, `UserId`, `ImpersonatorId`).
- **Seq self-hosted** (≤40 GB/día gratis) para logs estructurados.
- **Sentry Developer** (5k events/mes) para exceptions.
- **Grafana Cloud Free** para métricas/traces.
- **UptimeRobot Free** para uptime.
- PII masking automático en pipeline de logging.

## IA

- **DeepSeek V3.2** vía **Novita.ai** (endpoint OpenAI-compatible).
- NuGet: **OpenAI** (oficial, MIT) apuntado a Novita.
- PII masking antes de enviar a Novita.
- Cost ceiling por tenant (rate-limit + hard cap por plan).

## Testing

- **xUnit** (unit, integration).
- **bUnit** (componentes Blazor / MudBlazor).
- **Shouldly** (aserciones). NO FluentAssertions.
- **NSubstitute** (mocks). NO Moq.
- **Testcontainers** para SQL Server en integración; SQLite in-memory para dominio.
- **Respawn** para reset entre tests.
- Suites: `Domain.Tests`, `Application.Tests`, `Integration.Tests`, `Isolation.Tests`, `E2E.Tests`.

## Herramientas

- **Formato**: `dotnet format` (respeta `.editorconfig`).
- **Análisis**: `AnalysisLevel=latest-recommended` + **Roslyn Analyzer propio** (`TallerPro.Analyzers` TP0001-TP0005) inyectado vía `Directory.Build.props`.
- **Central Package Management**: `Directory.Packages.props`.
- **CI/CD**: GitHub Actions (workflows `ci.yml`, `deploy-staging.yml`, `deploy-prod.yml`, `mobile-build.yml`, `isolation-tests.yml`).
- **Contenedores**: Docker multi-stage + docker-compose por ambiente (dev/staging/prod). Secrets con `sops` + `age`.
- **Code signing**: Azure Trusted Signing (MSIX Windows).

## Integraciones externas confirmadas

| Servicio | Uso | Librería / SDK |
|---|---|---|
| Stripe Billing + Meters | Suscripciones + pricing metered | Stripe.net |
| SW Sapien | Timbrado CFDI 4.0 | HTTP client + Polly |
| Meta WhatsApp Cloud API | Mensajería utility/service | HTTP client |
| Brevo (Sendinblue) | Email transaccional + marketing | SDK oficial o HTTP |
| Labsmobile | SMS fallback MX | HTTP client |
| Telegram Bot API | Canal alterno gratuito | `Telegram.Bot` |
| Novita.ai | LLM (DeepSeek) | OpenAI NuGet |
| Cloudflare (R2, Access, CDN, WAF) | Storage + Zero Trust + edge | SDK/API |
