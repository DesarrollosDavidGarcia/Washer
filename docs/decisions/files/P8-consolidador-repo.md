# Prompt 8 (Opcional) — Consolidador Arquitectónico + Bootstrap del Repo

## Propósito

Este prompt toma las **7 respuestas generadas por P1-P7** y produce:
1. Un **documento de arquitectura unificado** (`ARCHITECTURE.md`) — la referencia maestra del proyecto.
2. La **estructura inicial del repo** con archivos seed listos para commit inicial.
3. Un **plan de ejecución Sprint 1** con 10-15 tickets concretos que el equipo puede tomar el lunes.
4. **CONTRIBUTING.md** + **README.md** del repo con convenciones.
5. **Índice maestro** de navegación entre documentos.

## Cuándo ejecutar este prompt

**Solo después** de haber ejecutado P1-P7 y tener las 7 respuestas guardadas. Este prompt NO genera arquitectura nueva — solo consolida, organiza y produce artefactos de inicio de proyecto.

**Modalidad de uso**: el usuario debe pegar las 7 respuestas previas como contexto al inicio de la sesión. Si el contexto es demasiado grande para una sola sesión, usar las versiones resumidas de los "ADR Updates" que cada prompt P1-P7 generó al final.

---

## Contexto del Proyecto

**TallerPro**: SaaS multitenant + multitaller para gestión de talleres mecánicos en México. Stack .NET Core 9 + Blazor Hybrid + MudBlazor + EF Core + SQL Server + Stripe + DeepSeek vía Novita. Equipo: founder + 3 devs .NET + 1 PM. Meta MVP: 10 tenants facturando en 90 días.

---

## Input Esperado (pegar antes de ejecutar este prompt)

Antes del prompt real, el usuario debe pegar en la sesión:

```
=== RESPUESTA P1 (Fundaciones) ===
[contenido completo o ADR Update]

=== RESPUESTA P2 (Aislamiento Tenant) ===
[contenido completo o ADR Update]

=== RESPUESTA P3 (Base de Datos) ===
[contenido completo o ADR Update]

=== RESPUESTA P4 (Auth + Impersonation) ===
[contenido completo o ADR Update]

=== RESPUESTA P5 (Pagos + Usage Tracking) ===
[contenido completo o ADR Update]

=== RESPUESTA P6 (IA + Catálogos + Observabilidad) ===
[contenido completo o ADR Update]

=== RESPUESTA P7 (Marketing + Deployment) ===
[contenido completo o ADR Update]
```

---

## Tu Rol

Actúa como **Staff Engineer + Technical Writer Senior** especializado en:
- Consolidar arquitecturas de software complejas en documentos navegables.
- Crear onboarding experiences para equipos que recién ingresan.
- Estructurar monorepos .NET para productividad.
- Escribir documentación técnica que los devs realmente leen.

Responde produciendo artefactos listos para commitear al repo inicial.

---

## Alcance de ESTE prompt (P8)

Entregar los **artefactos de inicio del proyecto** basados en las decisiones arquitectónicas ya tomadas en P1-P7.

**SÍ incluir**:
- Documento `ARCHITECTURE.md` unificado con todas las decisiones de P1-P7 organizadas lógicamente.
- Estructura de directorios y archivos del repo inicial.
- `README.md` raíz con quickstart, links a docs, badges.
- `CONTRIBUTING.md` con convenciones de código, PR policy, commit messages.
- `docs/` estructura con ADRs numerados.
- Sprint 1 plan con 10-15 tickets listos para GitHub Issues / Jira.
- Matriz de cross-references entre documentos.

**NO incluir**:
- Rehacer decisiones ya tomadas en P1-P7 (solo consolidar).
- Cuestionar el stack elegido.
- Generar código nuevo más allá del necesario para el repo seed (archivos de solución, gitignore, Dockerfiles base).

---

## Formato de Respuesta Esperado

### 1. Resumen Ejecutivo (400 palabras máx)

Qué es TallerPro, stack principal, modelo de negocio, timeline MVP, equipo, presupuesto, KPIs. Esto va al tope del `README.md`.

### 2. `ARCHITECTURE.md` Unificado

Documento markdown navegable con estructura:

```
# TallerPro Architecture

## 1. Overview
   1.1 System Context
   1.2 Container Architecture (C4)
   1.3 Tech Stack Summary
   1.4 Non-Functional Requirements

## 2. Multitenancy & Tenant Isolation
   2.1 Strategy (Single DB + TenantId + RLS)
   2.2 The 8 Defense Layers
   2.3 Roslyn Analyzer Enforcement
   2.4 Isolation Test Suite

## 3. Authentication & Authorization
   3.1 Identity Provider
   3.2 Multi-Branch Authorization
   3.3 Super-Admin Operations & Impersonation
   3.4 Cloudflare Access

## 4. Data Model
   4.1 Entity Relationship Diagram
   4.2 Platform-level Tables
   4.3 Tenant-level Tables
   4.4 Branch-level Tables
   4.5 Usage Tracking Tables
   4.6 Immutable Audit Tables

## 5. Business Logic Layer
   5.1 Mediator (Othamar) + CQRS
   5.2 Mapster Mappings
   5.3 FluentValidation Pipeline
   5.4 Soft Delete Pattern
   5.5 Catalog System (Generic)

## 6. Payments & Billing
   6.1 Pricing Model (Base + Branch + Metered)
   6.2 Stripe Billing + Meters Integration
   6.3 Usage Tracking Architecture
   6.4 Unit Economics & KPIs

## 7. AI & Intelligence
   7.1 Novita Integration (DeepSeek)
   7.2 Use Cases
   7.3 RAG Pipeline
   7.4 PII Masking (Dual Layer)
   7.5 Cost Tracking

## 8. Observability
   8.1 Serilog Configuration
   8.2 Seq Self-Hosted Setup
   8.3 Enrichers & PII Masking Automatic
   8.4 Alert Rules
   8.5 Retention Policies

## 9. User Interfaces
   9.1 Product Client (Blazor Hybrid + MudBlazor)
   9.2 Marketing Site (MVC + Bootstrap)
   9.3 Super-Admin Portal (MVC + MudBlazor)
   9.4 Shared SCSS Tokens

## 10. Integrations
    10.1 CFDI (SW Sapien)
    10.2 WhatsApp (Meta Cloud API)
    10.3 Email (Brevo)
    10.4 SMS (Labsmobile)
    10.5 Telegram
    10.6 Payment Gateways (Stripe Billing now, Connect/MP later)

## 11. Offline-First Architecture
    11.1 Local SQLite in Hybrid Client
    11.2 Outbox Pattern
    11.3 Conflict Resolution Strategies
    11.4 Local Encryption

## 12. Deployment & DevOps
    12.1 Environments (Local, Staging, Prod)
    12.2 Docker Compose (Alpha)
    12.3 Azure Container Apps (Beta)
    12.4 AKS (GA)
    12.5 CI/CD GitHub Actions
    12.6 Client Distribution (MSIX + APK)

## 13. Security & Compliance
    13.1 LFPDPPP (Mexico)
    13.2 SAT CFDI 4.0
    13.3 MFA & Cloudflare Zero Trust
    13.4 Data Protection Keys
    13.5 Incident Response (Cross-Tenant Leak Runbook)
    13.6 Threat Model STRIDE

## 14. Roadmap
    14.1 MVP 8 Weeks Breakdown
    14.2 Post-MVP (Months 3-6)
    14.3 Post-GA (Months 6-12)

## 15. Risks & Mitigations
    15.1 Top 10 Technical Risks
    15.2 Business Risks

## 16. Glossary

## 17. Decision Log (Links to ADRs)
```

Cada sección debe ser **concisa** (150-300 palabras) y linkear a documentos detallados en `docs/` para el contenido completo de código/DDL/etc. Ejemplo: la sección "2.3 Roslyn Analyzer Enforcement" NO incluye todo el código del analyzer (que está en P2), solo un resumen + link a `docs/adr/007-roslyn-tenant-isolation-analyzer.md`.

### 3. Estructura del Repo Inicial

Árbol completo con comentarios de propósito de cada archivo:

```
tallerpro-saas/
├── .github/
│   ├── workflows/
│   │   ├── ci.yml
│   │   ├── deploy-staging.yml
│   │   ├── deploy-prod.yml
│   │   ├── mobile-build.yml
│   │   └── isolation-tests.yml
│   ├── ISSUE_TEMPLATE/
│   │   ├── bug_report.md
│   │   ├── feature_request.md
│   │   └── security_issue.md
│   ├── PULL_REQUEST_TEMPLATE.md
│   └── CODEOWNERS
├── docs/
│   ├── adr/
│   │   ├── 0001-use-dotnet-9.md
│   │   ├── 0002-blazor-hybrid-maui-over-pwa.md
│   │   ├── 0003-mudblazor-100-percent-ui.md
│   │   ├── 0004-mediator-othamar-over-mediatr.md
│   │   ├── 0005-mapster-over-automapper.md
│   │   ├── 0006-multitenancy-single-db-rls.md
│   │   ├── 0007-roslyn-tenant-isolation-analyzer.md
│   │   ├── 0008-serilog-seq-observability.md
│   │   ├── 0009-stripe-billing-meters.md
│   │   ├── 0010-deepseek-novita-ai.md
│   │   ├── 0011-mvc-over-nextjs-marketing.md
│   │   ├── 0012-cloudflare-access-admin-portal.md
│   │   ├── 0013-impersonation-governance.md
│   │   ├── 0014-offline-first-sqlite-outbox.md
│   │   └── ... (uno por decisión mayor)
│   ├── runbooks/
│   │   ├── tenant-leak-response.md
│   │   ├── super-admin-access.md
│   │   ├── deployment.md
│   │   ├── incident-response.md
│   │   └── backup-restore.md
│   ├── security/
│   │   ├── threat-model.md
│   │   ├── pen-testing-plan.md
│   │   └── lfpdppp-compliance.md
│   ├── operations/
│   │   ├── alert-rules.md
│   │   ├── observability-guide.md
│   │   └── retention-policies.md
│   ├── diagrams/
│   │   ├── c4-context.md (mermaid)
│   │   ├── c4-containers.md
│   │   ├── erd.md
│   │   ├── signup-flow.md
│   │   └── impersonation-flow.md
│   └── README.md (índice de docs/)
├── src/
│   ├── TallerPro.Domain/
│   │   ├── TallerPro.Domain.csproj
│   │   └── .gitkeep
│   ├── TallerPro.Application/
│   │   ├── TallerPro.Application.csproj
│   │   └── .gitkeep
│   ├── TallerPro.Infrastructure/
│   ├── TallerPro.Shared/
│   ├── TallerPro.Components/
│   ├── TallerPro.Hybrid/
│   ├── TallerPro.Api/
│   ├── TallerPro.LocalDb/
│   ├── TallerPro.Web/
│   ├── TallerPro.Admin/
│   ├── TallerPro.Observability/
│   ├── TallerPro.Security/
│   └── TallerPro.Analyzers/
├── tests/
│   ├── TallerPro.Domain.Tests/
│   ├── TallerPro.Application.Tests/
│   ├── TallerPro.Integration.Tests/
│   ├── TallerPro.Isolation.Tests/
│   └── TallerPro.E2E.Tests/
├── build/
│   ├── docker/
│   │   ├── api.Dockerfile
│   │   ├── web.Dockerfile
│   │   └── admin.Dockerfile
│   ├── docker-compose.dev.yml
│   ├── docker-compose.prod.yml
│   └── scripts/
│       ├── db-init.sql
│       ├── deploy-staging.sh
│       └── rollback.sh
├── .editorconfig
├── .gitattributes
├── .gitignore
├── Directory.Build.props
├── Directory.Packages.props  (central NuGet versions)
├── TallerPro.sln
├── global.json
├── nuget.config
├── ARCHITECTURE.md
├── CHANGELOG.md
├── CODE_OF_CONDUCT.md
├── CONTRIBUTING.md
├── LICENSE
├── README.md
└── SECURITY.md
```

Para cada archivo root relevante, incluye su contenido inicial listo para commitear.

### 4. Archivos Clave del Repo

#### 4.1 `README.md` Raíz

Markdown completo con:
- Badges (build status, coverage, .NET version, license).
- Elevator pitch del producto.
- Quickstart para devs: clone → restore → run local (10 minutos).
- Links a ARCHITECTURE.md, CONTRIBUTING.md, docs/.
- Team section.
- Licencia y contacto.

#### 4.2 `CONTRIBUTING.md`

Convenciones del proyecto:
- Setup local completo (prerequisites, clone, restore, DB migrations, secrets, run).
- Git flow: trunk-based con feature branches cortas (< 2 días).
- Convenciones de commits: Conventional Commits (`feat:`, `fix:`, `docs:`, etc.).
- PR policy: mínimo 1 reviewer, 2 para cambios en `TallerPro.Security/` o `TallerPro.Analyzers/`, founder required para cambios en impersonation/pricing.
- Convenciones de código: `.editorconfig` enforced, MudBlazor 100% (no raw HTML/CSS excepto SCSS), Mediator (Othamar) para CQRS, Mapster para mappings, Serilog structured logging.
- PR labels: `security-review`, `needs-design-review`, `needs-dba-review`, `breaking-change`.
- Testing: todo PR con cambios en endpoints tenant-scoped debe incluir test en `TallerPro.Isolation.Tests`.
- Migraciones EF Core: no manual SQL; todo via `dotnet ef migrations add`.

#### 4.3 `SECURITY.md`

- Cómo reportar vulnerabilidades (email seguro + PGP key).
- SLA de respuesta por severidad.
- Política de disclosure responsable.
- Scope (qué se considera in-scope vs out-of-scope).
- Hall of Fame (opcional, post-GA).

#### 4.4 `.editorconfig`

Config completa para C#, Razor, SCSS, JS con reglas:
- `dotnet_diagnostic.TP0001.severity = error` (Roslyn analyzer).
- `dotnet_diagnostic.TP0002.severity = error`.
- `dotnet_diagnostic.TP0003.severity = error`.
- `dotnet_diagnostic.TP0004.severity = error`.
- `dotnet_diagnostic.TP0005.severity = warning`.
- Naming conventions .NET estándar.
- Indentación, line endings, tabs vs spaces.

#### 4.5 `Directory.Build.props`

Configuración central de todos los proyectos:
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors></WarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
  <ItemGroup>
    <!-- Analyzer compartido aplica a todos los proyectos -->
    <ProjectReference Include="$(MSBuildThisFileDirectory)src\TallerPro.Analyzers\TallerPro.Analyzers.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false"
                      Condition="'$(MSBuildProjectName)' != 'TallerPro.Analyzers'" />
  </ItemGroup>
</Project>
```

#### 4.6 `Directory.Packages.props`

Central Package Management con todas las versiones de NuGet packages usadas en el proyecto.

#### 4.7 `global.json`

```json
{
  "sdk": {
    "version": "9.0.100",
    "rollForward": "latestFeature"
  }
}
```

#### 4.8 `.gitignore`

Template de `dotnet new gitignore` + exclusiones custom:
- `*.user`, `*.suo`, `.vs/`, `bin/`, `obj/`.
- `*.env`, `appsettings.Development.json`, `appsettings.Local.json`.
- `logs/`, `*.log`.
- `coverage/`, `TestResults/`.
- Secrets locales.

### 5. ADRs (Architecture Decision Records)

Plantilla ADR Nygard style para cada decisión mayor:

```markdown
# ADR-NNNN: [Título corto de la decisión]

## Status
[Proposed | Accepted | Deprecated | Superseded by ADR-MMMM]

## Date
YYYY-MM-DD

## Context
[Qué problema se está resolviendo]

## Decision
[Qué se decidió]

## Alternatives Considered
- Opción A: [por qué se descartó]
- Opción B: [por qué se descartó]

## Consequences
### Positive
- ...

### Negative
- ...

### Risks
- ...

## References
- [Links a docs externos, PRs, discusiones]
```

Lista de 14 ADRs numerados con su contenido completo siguiendo la plantilla.

### 6. Sprint 1 — Plan de Ejecución

Tabla con 10-15 tickets listos para GitHub Issues:

| # | Título | Tipo | Asignado | Estimación | Dependencias | Criterios Aceptación |
|---|---|---|---|---|---|---|
| 1 | Setup inicial del monorepo | Infra | Dev 1 | 2d | - | `git clone && dotnet build` funciona en Linux/Windows/Mac. CI verde. |
| 2 | Crear `TallerPro.Analyzers` con TP0001-TP0005 | Security | Dev 2 | 3d | 1 | 5 reglas implementadas + 10 tests del analyzer verdes. Integrado en CI. |
| 3 | Crear `TallerPro.Isolation.Tests` base + 5 tests críticos | Security | Dev 2 | 2d | 2 | Suite corre en < 2 min en CI. 100% verde. |
| 4 | Migración EF Core inicial con DDL completo de P3 | DB | Dev 1 | 2d | 1 | `dotnet ef database update` deja DB funcional. Seed corre OK. |
| 5 | Middleware `ITenantContext` + `IBranchContext` + `IImpersonationContext` | Security | Dev 2 | 2d | 4 | Tests unitarios al 100%. Integrado con pipeline de auth. |
| 6 | Setup Serilog + Seq + enrichers custom | Observability | Dev 3 | 2d | 1 | Logs aparecen en Seq con TenantId. PII masking automático funciona. |
| 7 | Setup `TallerPro.Api` esqueleto con auth + health endpoints | Backend | Dev 3 | 2d | 5 | `/health` responde 200. JWT auth validado. Swagger OK. |
| 8 | Setup `TallerPro.Hybrid` target Windows con login funcional | Frontend | Dev 1 | 3d | 7 | App abre en Windows 11. Login funcional contra API. |
| 9 | Setup `TallerPro.Web` marketing skeleton | Marketing | Dev 3 | 2d | 1 | Homepage render OK. Bootstrap + SCSS compilando. Plausible integrado. |
| 10 | Setup `TallerPro.Admin` con Cloudflare Access | Admin | Dev 2 | 2d | 7 | `admin.tallerpro.mx` protegido con Cloudflare Access. Dashboard vacío render OK. |
| 11 | CI/CD pipelines completos | DevOps | Dev 1 | 2d | 1 | PR checks funcionan. Deploy a staging manual funcional. |
| 12 | Docker Compose alpha para dev local | DevOps | Dev 3 | 1d | 11 | `docker compose up` levanta stack completo en < 60s. |
| 13 | Seed de catálogos iniciales + Plans + PlanMeters | DB | Dev 1 | 1d | 4 | DB tiene datos listos para crear primer tenant manualmente. |
| 14 | Documentación ARCHITECTURE.md + ADRs 0001-0014 | Docs | PM + Founder | 2d | - | Onboarding de dev nuevo toma < 1 hora. |
| 15 | Setup Slack webhooks + Brevo + Cloudflare Zero Trust | Ops | Founder | 1d | - | Todas las integraciones operativas con keys en Key Vault. |

**Total Sprint 1**: ~27 días-persona. Con 3 devs + 1 PM + founder, cabe en 1-2 semanas.

**Definición de Done del Sprint 1**: 
- Repo clonable y compilable.
- CI verde.
- DB creable con DDL completo + seed.
- Tenant manual creable desde DB.
- Login funcional end-to-end (Hybrid → API).
- Aislamiento tenant enforced por Roslyn + tests.
- Observabilidad funcional (logs estructurados en Seq).
- Todos los portales (app, marketing, admin) render OK aunque vacíos.

### 7. Sprint 2-8 Roadmap Alto Nivel

Tabla con foco de cada sprint:

| Sprint | Objetivo | Entregables Clave |
|---|---|---|
| 2 | Catálogos + ServiceOrders core | Sistema catálogos + CRUD órdenes + clientes + vehículos |
| 3 | Inventario + Custodia + Cotización | ... |
| 4 | CFDI + Pagos + Stripe Billing setup | ... |
| 5 | IA Recepción + WhatsApp ingest | ... |
| 6 | Usage Tracking + Stripe Meters integración | ... |
| 7 | Sync offline robusto + Admin Portal completo | ... |
| 8 | Pruebas con 3 tenants piloto + Launch prep | ... |

### 8. Matriz de Cross-References

Tabla: "Si quieres entender X, lee Y":

| Tema | Documento principal | Documentos relacionados |
|---|---|---|
| Tenant isolation | `docs/adr/0006` + `docs/adr/0007` | `ARCHITECTURE.md#2`, `docs/runbooks/tenant-leak-response.md` |
| Impersonation | `docs/adr/0013` | `ARCHITECTURE.md#3.3`, `docs/runbooks/super-admin-access.md` |
| Pricing model | `ARCHITECTURE.md#6` | `docs/adr/0009` |
| ... | | |

### 9. Onboarding de Dev Nuevo

Checklist markdown `docs/onboarding.md`:

Día 1:
- [ ] Leer `README.md` (15 min).
- [ ] Leer `ARCHITECTURE.md` secciones 1-5 (1 hora).
- [ ] Setup local: clone, restore, DB migrations, run (2 horas).
- [ ] Correr suite de tests completa incluyendo `TallerPro.Isolation.Tests` (30 min).
- [ ] Crear tenant de prueba manual y login end-to-end (30 min).

Día 2-3:
- [ ] Leer ADRs 0001-0014 (2 horas).
- [ ] Pair con dev senior en un ticket simple (4 horas).
- [ ] Leer `CONTRIBUTING.md` + `SECURITY.md` (30 min).
- [ ] Primer PR pequeño mergeado (docs fix o test adicional).

Semana 2:
- [ ] Leer runbooks relevantes a su área.
- [ ] Tomar primer ticket técnico.

### 10. Bloque Final: `ADR Final Summary`

Resumen ejecutivo de **TODAS** las decisiones arquitectónicas consolidadas del proyecto (desde P1 hasta P7), en formato de lista numerada con máximo 30 bullets. Este bloque es la "cheat sheet" que el founder imprime y pega en la pared.

Ejemplo de bullets:
1. Stack producto: .NET 9 + Blazor Hybrid (MAUI Windows + Android) + MudBlazor 100%.
2. CQRS con Mediator (Othamar), mapping con Mapster, validación con FluentValidation, logging con Serilog → Seq.
3. Multitenancy: Single DB + TenantId + EF Global Query Filters + SQL Server RLS (defense-in-depth 8 capas).
4. Aislamiento enforced via Roslyn Analyzer `TallerPro.Analyzers` (TP0001-TP0005) + `TallerPro.Isolation.Tests` en CI.
5. ...
30. ...

---

## Restricciones de la Respuesta

- **Artefactos listos para commitear**, no descripciones.
- `ARCHITECTURE.md` debe ser **navegable** con TOC, anchors, y links internos.
- Cada archivo root (README, CONTRIBUTING, SECURITY, etc.) debe tener contenido completo, no placeholder.
- ADRs siguen plantilla Nygard estricta.
- Sprint 1 plan debe ser **tomable el lunes** — cada ticket con criterios de aceptación claros.
- Longitud target: ~15,000-20,000 tokens.

---

## Post-ejecución

Con los artefactos de este prompt, el founder puede:

1. **Crear repo GitHub** con estructura inicial.
2. **Commit inicial** con todos los archivos del punto 3-4.
3. **Crear 15 GitHub Issues** del punto 6.
4. **Proyecto GitHub Projects** con swim lanes por dev.
5. **Invitar a los 3 devs + PM**.
6. **Kick-off Monday** con ARCHITECTURE.md como reading previo.

**Timeline realista**: Sprint 1 completo en 1-2 semanas laborales. MVP completo en 8 semanas según roadmap P1.
