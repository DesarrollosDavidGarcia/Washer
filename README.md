# TallerPro

[![CI](https://github.com/DesarrollosDavidGarcia/Washer/actions/workflows/ci.yml/badge.svg)](https://github.com/DesarrollosDavidGarcia/Washer/actions/workflows/ci.yml)

**SaaS multitenant para gestión de talleres mecánicos en México.** Arranque en Chihuahua/Bajío; escalable a LatAm. Meta MVP: 10 tenants facturando en 90 días.

Stack: .NET 9 · Blazor Hybrid (MAUI Windows + Android) · MudBlazor · ASP.NET Core · EF Core · SQL Server · Serilog · Stripe · Mediator.

---

## Quickstart

```bash
# Prerrequisito: .NET 9 SDK (9.0.203+)
git clone https://github.com/DesarrollosDavidGarcia/Washer.git && cd Washer

# Linux / macOS (sin TallerPro.Hybrid — requiere workload MAUI)
dotnet restore TallerPro.Linux.slnf --locked-mode
dotnet build TallerPro.Linux.slnf -c Release
dotnet test TallerPro.Linux.slnf

# Windows (incluye TallerPro.Hybrid)
dotnet restore TallerPro.sln --locked-mode
dotnet build TallerPro.sln -c Release
dotnet test TallerPro.sln
```

> **TallerPro.Hybrid** requiere `dotnet workload install maui` para compilar targets MAUI. Sin el workload, usar `TallerPro.Linux.slnf`.

---

## Estructura del repo

```
src/
  TallerPro.Domain/          # Dominio puro — entidades, value objects
  TallerPro.Application/     # CQRS con Mediator — handlers, validators
  TallerPro.Infrastructure/  # EF Core, SQL Server, integraciones externas
  TallerPro.Api/             # ASP.NET Core Minimal APIs + SignalR
  TallerPro.Web/             # Marketing site (MVC + Bootstrap)
  TallerPro.Admin/           # Super-Admin Portal (MVC + MudBlazor)
  TallerPro.Components/      # Razor Class Library — componentes MudBlazor
  TallerPro.Hybrid/          # MAUI Blazor Hybrid — cliente Windows + Android
  TallerPro.LocalDb/         # SQLite offline + Outbox
  TallerPro.Observability/   # Serilog enrichers, PII masking
  TallerPro.Security/        # TenantContext, impersonation, auth
  TallerPro.Analyzers/       # Roslyn analyzers TP0001-TP0005
tests/
  TallerPro.Domain.Tests/
  TallerPro.Application.Tests/
  TallerPro.Integration.Tests/
  TallerPro.Isolation.Tests/ # Defense-in-depth de aislamiento tenant — CRÍTICO
  TallerPro.E2E.Tests/
.specify/                    # Spec-driven development: constitución, stack, templates
specs/                       # Artefactos por feature (spec, plan, tasks, analyze)
docs/decisions/              # ADRs
.claude/                     # Comandos y agentes de Claude Code
```

---

## Documentación

| Recurso | Ubicación |
|---|---|
| Constitución del proyecto | [`.specify/memory/constitution.md`](.specify/memory/constitution.md) |
| Stack y versiones | [`.specify/memory/stack.md`](.specify/memory/stack.md) |
| Decisiones arquitectónicas | [`docs/decisions/`](docs/decisions/) |
| Spec activa (bootstrap) | [`specs/001-bootstrap-monorepo/`](specs/001-bootstrap-monorepo/) |
| Guía de desarrollo | [`CLAUDE.md`](CLAUDE.md) |

---

## Flujo de desarrollo

```
constitution → specify → clarify → plan → tasks → analyze → implement
```

Slash commands en Claude Code: `/speckit.<fase>`. Sin `spec + plan + tasks + analyze(READY)` → no se toca `src/`.

---

## Convenciones

- **Commits**: Conventional Commits (`feat:`, `fix:`, `chore:`, `test:`, `docs:`).
- **PR policy**: ≥1 reviewer; 2 para `TallerPro.Analyzers/` o `TallerPro.Security/`.
- **Estilo**: `.editorconfig` enforced — `dotnet format` antes de cada commit.
- **Aislamiento tenant**: tolerancia cero a cross-tenant leaks. Todo endpoint tenant-scoped requiere test en `Isolation.Tests`.
