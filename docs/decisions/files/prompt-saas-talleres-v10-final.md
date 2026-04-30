# Prompt: Arquitectura SaaS Multitenant + Multitaller para Talleres Mecánicos
## Stack completo:
- **Producto**: .NET Core 9 + Blazor Hybrid (MAUI) + MudBlazor 100% + **Mediator (Martin Othamar)** + Mapster + EF Core + SQL Server + SignalR + Docker
- **Marketing site**: ASP.NET Core MVC .NET 9 + Bootstrap 5 + SCSS + Output Caching + Cloudflare
- **Super-Admin Portal**: ASP.NET Core MVC .NET 9 separado (`admin.tallerpro.mx`) + MudBlazor + Cloudflare Access (Zero Trust)
- **Observabilidad**: Serilog + Seq self-hosted + enrichers custom por tenant/branch/user
- **Aislamiento multitenant**: defense-in-depth de 8 capas + Roslyn analyzer + test suite automatizado (ver §16)
- **IA**: DeepSeek V3.2/R1 vía Novita (OpenAI-compatible)
- **Pagos**: Stripe Billing + Stripe Meters; Nivel 2 post-MVP
- **CFDI**: SW Sapien
- **Pricing**: Base + sucursal adicional + overage metered
- **Mercado**: México (arranque Chihuahua/Bajío), escalable a LatAm

---

## 1. Rol

Actúa como **CTO + Arquitecto de Software Senior** con experiencia comprobada en:
- Blazor Hybrid + MudBlazor, Mapster + Mediator (Othamar), EF Core multitenant.
- **Defense-in-depth para aislamiento tenant en SaaS B2B** — sabes que un cross-tenant leak es escenario existencial y lo tratas con rigor de banking/healthcare.
- LLMs OpenAI-compatibles desde .NET.
- CFDI 4.0 con SW Sapien.
- Stripe Billing + Meters + Connect.
- Observabilidad seria (Serilog/Seq/OpenTelemetry) en multitenant con cumplimiento LFPDPPP.
- Governance de super-admin operations con impersonation controlada.
- ASP.NET Core MVC para sitios de marketing con SEO técnico.
- Arquitecturas offline-first con sync.
- Unit economics SaaS B2B pre-break-even.
- **Roslyn analyzers, source generators, test automation para enforcement de invariantes críticos**.

Responde como arquitecto hablando con founder técnico: sin relleno, tradeoffs explícitos, cuestionando premisas. Cada decisión viene con: (a) implicación de costo, (b) impacto en unit economics cuando aplique, (c) impacto en aislamiento tenant cuando aplique, (d) impacto en compliance LFPDPPP cuando aplique.

## 2. Contexto de Negocio

- Mercado: México (Chihuahua/Bajío), LatAm 12–24 meses.
- Target: talleres 5–30 bahías, multi-sucursal.
- Jerarquía: Tenant → Branches → Warehouses/Orders/Inventory; Users M:N Branches.
- Customers (finales del taller) scope Tenant.
- Internet intermitente es regla.
- **Equipo SaaS operativo**:
  - Founder: acceso total (elevated read + impersonation full).
  - Equipo soporte (futuro): solo read-only.
- Meta MVP: 10 tenants facturando en 90 días.
- Equipo dev: 3 devs .NET + 1 PM.
- Presupuesto infra alpha: < $1,000 USD/mes.
- Pricing: $899 MXN base por taller + $449 MXN por sucursal adicional + overage metered.
- **Tolerancia a leak cross-tenant: CERO**. Un solo incidente es escenario existencial (legal + reputacional + LFPDPPP). Aislamiento es invariante del sistema, no feature.

## 3. Convenciones Técnicas (no negociables)

### 3.1 UI — Blazor Hybrid
MudBlazor 100%, `MudThemeProvider` en `Taller.Components`, SCSS compilado para custom, tokens compartidos en `shared-tokens.scss`.

### 3.2 Backend — librerías sin riesgo de licensing

| Propósito | Librería | NuGet | Licencia |
|---|---|---|---|
| Mediator / CQRS | **Mediator (Othamar)** | `Mediator.SourceGenerator` + `Mediator.Abstractions` | MIT |
| Mapping | **Mapster** | `Mapster` + `Mapster.DependencyInjection` | MIT |
| Validación | **FluentValidation** | `FluentValidation` | Apache 2.0 |
| Resiliencia | **Polly v8** | `Microsoft.Extensions.Http.Resilience` | BSD |
| Logging | **Serilog** | ver §3.5 | Apache 2.0 |
| Tests asserts | Shouldly | `Shouldly` | BSD |
| Tests mocks | NSubstitute | `NSubstitute` | BSD |
| Stripe | Stripe.net | `Stripe.net` | Apache 2.0 |
| **Tenant isolation analyzer** | Custom Roslyn analyzer | `TallerPro.Analyzers` (propio) | — |

Reglas: CQRS con Mediator, `ValueTask<T>` en hot paths, FluentValidation pipeline, `Result<T>` no excepciones, `async void` prohibido.

### 3.3 Soft delete ESTRICTO
Sin cambios vs v9.

### 3.4 Catálogos genéricos
Sin cambios vs v9.

### 3.5 Observabilidad como convención no negociable
Sin cambios vs v9 (Serilog único, Seq self-hosted, enrichers, PII masking, retention policies, alert rules).

### 3.6 Seguridad como convención
Sin cambios vs v9 (MFA, Cloudflare Access, rate limiting, TLS, Key Vault, Data Protection keys).

### 3.7 Aislamiento tenant como convención no negociable

- **Prohibido `IgnoreQueryFilters()`** en código user-facing. Permitido solo en métodos marcados `[AllowCrossTenant("razón detallada")]`:
  - Jobs del sistema (Stripe meter reporter, billing cycle sync).
  - Operaciones de super-admin explícitas con audit.
  - Migraciones EF Core.
- **Prohibido SQL crudo** (`FromSqlRaw`, `ExecuteSqlRaw`) sin parámetros tipados.
- **Prohibido cache key sin tenant prefix** — todo cache pasa por `ITenantScopedCache`.
- **Prohibido storage path sin tenant segregation** — todo pasa por `ITenantScopedStorage`.
- **Roslyn analyzer activo** que falla el build si detecta estos patrones.
- **Test suite de aislamiento en CI** — ver §16.5.
- Cualquier bypass requiere PR con label `security-review` + aprobación de founder.

## 4. .NET 9 → .NET 10 LTS
Sin cambios vs v9.

## 5. Marketing Site (TallerPro.Web)
Sin cambios vs v9.

## 6. Arquitectura del Producto

### 6.1 Estructura de solución

```
TallerPro.sln
├── src/
│   ├── TallerPro.Domain/
│   ├── TallerPro.Application/
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
│   └── TallerPro.Analyzers/           # Roslyn analyzers (tenant isolation + otros)
├── tests/
│   ├── TallerPro.Domain.Tests/
│   ├── TallerPro.Application.Tests/
│   ├── TallerPro.Integration.Tests/
│   ├── TallerPro.Isolation.Tests/     # suite dedicada a aislamiento (§16.5)
│   └── TallerPro.E2E.Tests/
└── build/
```

### 6.2–6.3 TallerPro.Admin + Hybrid + offline-first + auth + SignalR
Sin cambios vs v9.

## 7. Multitenancy + Multi-Taller

Sin cambios vs v9. **Refuerzo crítico**: la decisión de multitenant se respalda en §16 (defensa en profundidad). Incluso con "Single DB + TenantId" (opción A, la más simple), el aislamiento es robusto gracias a las 8 capas.

## 8. Esquema de Base de Datos
Sin cambios vs v9.

## 9. API + SignalR
Sin cambios vs v9.

## 10. Módulos MVP con AC
Sin cambios vs v9.

## 11. Pagos (Stripe Billing + Meters)
Sin cambios vs v9.

## 12. IA DeepSeek vía Novita
Sin cambios vs v9.

## 13. Integraciones Externas
Sin cambios vs v9.

## 14. Requerimientos No-Funcionales
Sin cambios vs v9.

## 15. Seguridad y Super-Admin Operations
Sin cambios vs v9.

## 16. Aislamiento Tenant — Garantías Zero Cross-Tenant Leaks

### 16.1 Principio

Un leak cross-tenant en este SaaS es **escenario existencial**. La mitigación no es "best effort" — es defense-in-depth con 8 capas independientes, cada una capaz de prevenir el leak **por sí sola**. Solo si las 8 fallan simultáneamente hay leak. El sistema se diseña así porque una sola capa puede tener bugs.

**Invariante del sistema**: para todo request autenticado con JWT de tenant A, es **imposible** leer, escribir, recibir vía push, cachear, loggear con acceso, o descargar cualquier dato de tenant B, sin que el sistema registre la operación como violación crítica y la bloquee.

### 16.2 Las 8 capas de defensa

| # | Capa | Qué hace | Tecnología | Dónde puede fallar | Fallback |
|---|------|----------|------------|--------------------|----------|
| 1 | **Autenticación JWT firmado** | `TenantId` claim firmado HMAC-SHA256. No falsificable sin signing key. | ASP.NET Core Identity + Duende / Entra External ID | Key comprometida, algoritmo débil | Key rotation + Key Vault + tokens 15 min |
| 2 | **Middleware `ITenantContext`** | Extrae `TenantId` del JWT y lo inyecta en scope DI. Sin claim → 401. | Custom middleware ASP.NET Core | Bug en middleware, skip accidental | Capas 3–8 siguen aplicando |
| 3 | **EF Core Global Query Filters** | Filtro automático `WHERE TenantId = @currentTenantId` en toda query. | Entity Framework Core | Bug en setup, `IgnoreQueryFilters()` accidental | Roslyn analyzer bloquea uso (§16.4) + Capa 4 |
| 4 | **SQL Server Row-Level Security** | Policies a nivel motor DB. Filtro por `SESSION_CONTEXT('TenantId')`. Respetado incluso por Dapper. | SQL Server RLS nativo | Bug en policy, session context no seteado | Capa 5 |
| 5 | **Índices compuestos tenant-first** | Todos los índices empiezan con `TenantId`. Barrera física. | DDL SQL Server | Bug en DDL de tabla nueva | Code review + DDL diff en CI |
| 6 | **Segregación cache + storage + logs** | Cache keys `tenant:{TenantId}:*`. File paths `/{TenantId}/{BranchId}/...`. Logs con property `TenantId`. | Wrappers obligatorios | Bypass accidental del wrapper | Roslyn analyzer prohíbe `IDistributedCache` raw y `BlobServiceClient` raw |
| 7 | **SignalR groups por tenant** | Hubs usan grupos `tenant:{TenantId}`. Push a A no lo recibe B. | SignalR Groups API | Bug en hub, join group incorrecto | Auditoría de mensajes + test suite |
| 8 | **Auditoría activa + anomaly detection** | `AuditLog` con `TenantId` obligatorio. Job diario detecta "request TenantId=A accedió a entidad TenantId=B". | AuditLog + alert rules | Bug en el job | Pen testing + log review semanal |

### 16.3 Vectores de ataque específicos

Defensa obligatoria para cada uno:

| # | Vector | Ejemplo concreto | Defensa obligatoria |
|---|--------|------------------|---------------------|
| 1 | Endpoint sin `[Authorize]` | `public IActionResult GetOrder(Guid id)` sin atributo | Policy default `FallbackPolicy = AuthenticatedWithTenant`; endpoints públicos requieren `[AllowAnonymous]` explícito + PR review |
| 2 | Admin endpoint cross-tenant sin audit | Handler super-admin lee todos los tenants | Separación en `TallerPro.Admin` con `IAdminContext` distinto; contexto normal nunca cruza |
| 3 | Background job sin tenant context | Hangfire job procesa "todas las órdenes" | `[SystemJob]` explícito; jobs per-tenant toman `TenantId` como param |
| 4 | Cache key sin tenant prefix | `_cache.Set("user:123", data)` | Wrapper `ITenantScopedCache`. Analyzer prohíbe `IDistributedCache` directo |
| 5 | File path sin segregación | `blob.UploadAsync("photos/123.jpg", stream)` | Wrapper `ITenantScopedStorage`. Analyzer prohíbe `BlobServiceClient` directo |
| 6 | Webhook mal mapeado | Stripe webhook con `customer.id` desconocido → default a tenant A | Lookup estricto por `StripeCustomerId`; si no existe → descarta + alerta |
| 7 | Logs cross-tenant al super-admin | Query "todos los logs" sin filtro | UI admin exige `TenantId` param obligatorio; agregados solo anónimos |
| 8 | Marketing público tocando DB producto | `/public/validate-email` lee `Users` | `/public/*` tiene DbContext separado readonly a `LeadCapture` única |
| 9 | Impersonation mal implementada | Claim `impersonation.tenant_id=A` pero filter usa `superAdmin.TenantId` (null) | `ITenantContext.CurrentTenantId` resuelve primero de impersonation, después JWT. Tests exhaustivos |
| 10 | Sync offline filtra data incorrecta | Cliente Hybrid pide sync, recibe data de otro tenant | Endpoint sync tenant-scoped explícito; response incluye assertion de `TenantId` coincidente |
| 11 | Reports/exports | Export CFDI de tenant A contiene datos de B | Servicio export recibe `TenantId` explícito + assertion row-by-row |
| 12 | Search compartido | ElasticSearch / SQL full-text con índice compartido | Tenant ID en cada documento + filter obligatorio en query |

### 16.4 Roslyn Analyzer — enforcement automatizado

Proyecto `TallerPro.Analyzers` con reglas que **fallan el build**:

```csharp
// src/TallerPro.Analyzers/Rules/TenantIsolationAnalyzer.cs

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TenantIsolationAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor TP0001_IgnoreQueryFilters = new(
        id: "TP0001",
        title: "Uso de IgnoreQueryFilters() requiere [AllowCrossTenant]",
        messageFormat: "IgnoreQueryFilters() detectado en '{0}'. Requiere [AllowCrossTenant(\"razón\")] con justificación.",
        category: "TenantIsolation",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Saltarse Global Query Filters rompe aislamiento tenant. Solo permitido en jobs de sistema o ops super-admin con audit.");

    public static readonly DiagnosticDescriptor TP0002_RawSqlWithoutParams = new(
        id: "TP0002",
        title: "SQL raw con interpolación de strings",
        messageFormat: "FromSqlRaw/ExecuteSqlRaw con interpolación. Usa FromSqlInterpolated o parámetros tipados.",
        category: "TenantIsolation",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TP0003_RawCacheAccess = new(
        id: "TP0003",
        title: "IDistributedCache/IMemoryCache directo prohibido",
        messageFormat: "Uso de {0} directo detectado. Usa ITenantScopedCache.",
        category: "TenantIsolation",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TP0004_RawBlobAccess = new(
        id: "TP0004",
        title: "BlobServiceClient/IBlobStorage directo prohibido",
        messageFormat: "Acceso directo a blob detectado. Usa ITenantScopedStorage.",
        category: "TenantIsolation",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TP0005_ControllerWithoutAuth = new(
        id: "TP0005",
        title: "Controller sin [Authorize] explícito",
        messageFormat: "'{0}' no tiene [Authorize] ni [AllowAnonymous]. Default policy debe aplicar.",
        category: "TenantIsolation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // ... Initialize(), AnalyzeSymbol() implementations
}
```

**Atributo `[AllowCrossTenant]`**:

```csharp
namespace TallerPro.Security;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class AllowCrossTenantAttribute : Attribute
{
    public string Reason { get; }
    public AllowCrossTenantAttribute(string reason) 
        => Reason = reason ?? throw new ArgumentNullException(nameof(reason));
}
```

**Integración en `.csproj`**:

```xml
<ItemGroup>
  <ProjectReference Include="..\TallerPro.Analyzers\TallerPro.Analyzers.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

**Supresión controlada**: `#pragma warning disable TP0001` con comentario de razón + código específico + `#pragma warning restore TP0001`. PR con label `security-review` obligatorio.

### 16.5 Test Suite de Aislamiento (`TallerPro.Isolation.Tests`)

Proyecto dedicado, corre en CI **en cada PR**. Fallo de test = build fallido = no merge.

```csharp
// tests/TallerPro.Isolation.Tests/CrossTenantLeakTests.cs

public class CrossTenantLeakTests : IsolationTestBase
{
    [Theory]
    [TenantScopedEndpoints] // enumera endpoints via reflection
    public async Task Endpoint_WithTenantAToken_CannotAccessTenantBResource(
        HttpMethod method, string urlTemplate)
    {
        // Arrange
        var (tenantA, userA, tokenA) = await CreateTenantWithUser();
        var (tenantB, userB, tokenB) = await CreateTenantWithUser();
        var resourceInB = await SeedResourceForTenant(tenantB, urlTemplate);
        
        // Act: autenticado como A, intenta acceder a recurso de B
        var client = CreateClientWithToken(tokenA);
        var url = urlTemplate.Replace("{id}", resourceInB.Id.ToString());
        var response = await client.SendAsync(new HttpRequestMessage(method, url));
        
        // Assert
        // 404 ideal (tenant A no debería saber que el recurso existe)
        // 403 aceptable pero menos seguro
        // 200 = CRITICAL LEAK = test falla
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
        response.StatusCode.ShouldNotBe(HttpStatusCode.OK);
        
        // Verificar que audit log registró el intento
        var auditEntries = await GetAuditLogForTenant(tenantA);
        auditEntries.ShouldContain(e => e.Action == "UnauthorizedAccessAttempt");
    }
    
    [Theory]
    [TenantScopedEndpoints]
    public async Task Endpoint_CannotWriteToOtherTenantResource(...) { ... }
    
    [Fact]
    public async Task Cache_TenantACannotReadTenantBCachedData() { ... }
    
    [Fact]
    public async Task Storage_TenantACannotDownloadTenantBBlob() { ... }
    
    [Fact]
    public async Task SignalR_TenantACannotJoinTenantBGroup() { ... }
    
    [Fact]
    public async Task SyncEndpoint_ReturnsOnlyTenantAData() { ... }
    
    [Fact]
    public async Task Export_CannotIncludeOtherTenantRows() { ... }
    
    [Fact]
    public async Task Webhook_UnknownCustomerId_IsDiscardedAndAlerted() { ... }
    
    [Fact]
    public async Task Impersonation_SuperAdminImpersonatesA_CannotReadB() { ... }
    
    [Fact]
    public async Task Impersonation_ExpiredSession_ReturnsTo401() { ... }
    
    [Fact]
    public async Task BackgroundJob_TenantScoped_OnlyProcessesGivenTenant() { ... }
    
    [Fact]
    public async Task Logs_QueryByTenantA_DoesNotLeakTenantBLogs() { ... }
}
```

**`[TenantScopedEndpoints]` attribute**: data source que enumera todos los endpoints tenant-scoped leyendo `TallerPro.Api` via reflection, filtrando por `[Authorize]` sin `[AdminOnly]`. Garantiza cobertura **100%** de endpoints. Endpoint nuevo sin test correspondiente → falla automáticamente en CI.

**Métricas de la suite**:
- Cobertura: 100% de endpoints tenant-scoped (enforcement via reflection).
- Runtime objetivo: < 3 min en CI (paralelización + in-memory DB).
- Ejecución: cada PR + main + nightly full-scan.

### 16.6 Runbook de Incident Response para Cross-Tenant Leak

Documento formal `docs/runbooks/tenant-leak-response.md`. Fases:

**Fase 1 — Detección y clasificación (0–30 min)**
- Trigger: alerta Slack del job de anomaly detection, report de tenant, o descubrimiento durante soporte.
- Severidad:
  - **P0 Critical**: data sensible (CFDI, PII, finanzas) expuesta, múltiples tenants.
  - **P1 High**: data operativa expuesta, pocos tenants.
  - **P2 Medium**: lookup de IDs sin data material.
  - **P3 Low**: vulnerabilidad teórica sin exploit demostrable.
- Asignar Incident Commander (default: founder).

**Fase 2 — Contención (30–120 min)**
- Feature flag: deshabilitar endpoint afectado.
- Si sistémico: modo read-only global temporal.
- Si involucra impersonation: terminar todas las sesiones activas.
- Si involucra claves: rotar JWT signing key + forzar re-login global.
- Snapshot de logs, DB state, evidencia para forensics.

**Fase 3 — Comunicación (primeras 48 h)**
- Tenants afectados: email inicial < 2 h del descubrimiento (incluso causa no confirmada), seguimiento < 24 h con detalle, llamada a enterprise.
- **INAI (LFPDPPP Art. 64)**: notificación obligatoria dentro de plazo legal si hay exposición PII. Template pre-aprobado por abogado.
- Stakeholders internos: equipo, inversores, advisors.
- Si magnitud amerita: declaración pública con transparencia total.

**Fase 4 — Forensics (días 2–7)**
- Scan Serilog cross-reference `TenantId` esperado (JWT) vs `TenantId` de entidades.
- Identificar: quién, qué, cuándo, desde dónde, cuánto tiempo.
- Preservar logs + DB snapshots + audit en bucket read-only con hash.
- Timeline reconstruido.

**Fase 5 — Remediación (días 3–14)**
- Fix con 2+ reviewers (founder obligatorio uno).
- Test que reproduce leak original + variantes.
- Deploy con rollback.
- Compensación a afectados si aplica (crédito, migración asistida).

**Fase 6 — Post-mortem (semana 2–3)**
- Causa raíz (5 Whys, Ishikawa).
- **Por qué las 8 capas no lo previnieron** — gap analysis explícito.
- Action items con owner y fecha.
- Cambios sistémicos: nueva regla analyzer, nuevo test, nueva alerta, training.
- Post-mortem publicado a tenants (nivel ajustado, sin exponer más riesgo).

**Fase 7 — Prevención sistémica (mes 1–3)**
- Pen testing externo enfocado.
- Review de entidades similares al bug.
- Training del equipo.

### 16.7 Pen Testing Plan

**Pre-soft launch enterprise**:
- Firma externa con experiencia multitenant. Opciones MX: ioSENTRIX, Protiviti, Deloitte Cyber. Globales: Bishop Fox, NCC Group, Trail of Bits.
- Scope "white box" con acceso a código:
  - Tenant isolation (vector principal).
  - Auth bypass.
  - Authorization elevation.
  - Impersonation abuse.
  - SQL/ORM injection.
  - SSRF/SSJI en integraciones.
  - Deserialization.
  - Rate limiting bypass.
- Entregable: report con severidad + reproducción + fix recomendado.
- Retest 90 días post-fix.
- Frecuencia: anual + ad-hoc post cambios mayores.

**Presupuesto**: $8,000–25,000 USD por ciclo. **No MVP**, presupuesto para 6 meses post-GA.

### 16.8 Threat Model (entregable del arquitecto)

`docs/security/threat-model.md` formato STRIDE. Por cada componente crítico (API, Hybrid, Admin Portal, Webhooks, Impersonation, Sync):
- **S**poofing
- **T**ampering
- **R**epudiation
- **I**nformation Disclosure (incluye cross-tenant)
- **D**enial of Service
- **E**levation of Privilege

Cada amenaza: probabilidad × impacto × mitigación actual × owner.

## 17. Deployment y DevOps
Sin cambios vs v9.

## 18. Inventario de Costos y Estrategia Financiera

Sin cambios vs v9. **Adición**:

### 18.X Costos específicos de aislamiento tenant

| Componente | Costo | Notas |
|---|---|---|
| Desarrollo Roslyn analyzer | One-time ~1 semana dev | Amortiza en cada commit |
| Mantenimiento test suite isolation | ~10% overhead por feature nueva | Crece lineal con endpoints |
| SQL Server RLS | $0 | Incluido en motor |
| Pen testing externo | $8,000–25,000 anual | Post-GA |
| Seguro cyber liability | $2,000–5,000/año | Post-GA para enterprise |
| **Impacto MVP** | **~0 directo** (semana dev incluida en roadmap) | |

Inversión en aislamiento es **mayoritariamente tiempo dev, no dinero**. ROI = protección contra escenario existencial.

## 19. Formato de Respuesta Esperado

Orden exacto (45 secciones):

1. **Assumptions a validar**.
2. **Arquitectura C4** (Contexto + Contenedores).
3. **Estructura de la solución**.
4. **Convenciones técnicas** — resumen.
5. **Justificación Mediator (Othamar)** con código comparativo.
6. **Sistema catálogos genéricos** con código completo.
7. **Marketing MVC** — estructura, pricing page con calculadora.
8. **Flujo signup → onboarding** — diagrama secuencia.
9. **Soft delete enforcement** — patrón EF Core.
10. **Plan sync offline** — outbox, conflictos.
11. **Estrategia multitenant** — matriz + decisión + roadmap.
12. **DDL SQL Server completo** — incluye RLS policies, entidades usage tracking, entidades super-admin.
13. **Autorización multi-taller + super-admin** — middleware completo.
14. **Tabla API endpoints + Hubs SignalR**.
15. **Integración DeepSeek vía Novita** — handler Mediator con meter tracking + logs estructurados.
16. **RAG manuales** — pipeline + vector store.
17. **PII masking** — dos niveles (Novita requests + Serilog logs).
18. **Pagos Nivel 1 (Stripe Billing + Meters)** — arquitectura completa.
19. **Pagos Nivel 2 (post-MVP)** — matriz + diagrama.
20. **Dashboard Consumo del Tenant** — mockup MudBlazor.
21. **Portal Subscription del Tenant** — UI + preview proration.
22. **Super-Admin Dashboard Financiero** — MRR, overage, unit economics.
23. **Configuración Serilog completa** — Program.cs + enrichers custom con código C#.
24. **Setup Seq self-hosted** — docker-compose + retention + backup.
25. **Retention policies de logs** — tabla por tipo + cold storage.
26. **Alert rules MVP** — 10 reglas + código del AlertEvaluator.
27. **TallerPro.Admin arquitectura + vistas** — estructura + mockups.
28. **Modelo de impersonation completo** — DDL + flujo + middleware + banner MudBlazor + double-confirm.
29. **Email templates notificación al tenant**.
30. **Support Access Log del Tenant (Hybrid)** — UI + real-time + botón "Terminar acceso".
31. **Cloudflare Access setup para admin** — políticas, allowlist, SSO, MFA.
32. **Aviso de Privacidad actualizado** — texto final.
33. **SEO técnico marketing**.
34. **Tokens compartidos** — SCSS + generador MudTheme.
35. **Tabla integraciones externas con costos**.
36. **Tabla consolidada costos por fase + palancas + pricing + unit economics 3 escenarios + KPIs financieros**.
37. **§16 — Las 8 capas de aislamiento tenant documentadas** — tabla completa + código ejecutable de cada capa: middleware `ITenantContext` con resolución de impersonation, Global Query Filter config completa en `OnModelCreating`, RLS policy SQL completa, `ITenantScopedCache` + `ITenantScopedStorage` con implementaciones, SignalR group management con `ITenantScopedHub<T>` base, background job de anomaly detection con código.
38. **Roslyn Analyzer `TallerPro.Analyzers`** — código completo: `TenantIsolationAnalyzer` con 5 reglas (TP0001–TP0005) completamente implementadas, atributo `[AllowCrossTenant]`, integración en `.csproj`, tests del propio analyzer.
39. **Test Suite `TallerPro.Isolation.Tests`** — código de `IsolationTestBase` (in-memory DB, helpers de seeding), `[TenantScopedEndpoints]` attribute con reflection, al menos 10 test cases representativos de los 12 vectores §16.3, configuración de GitHub Actions para correr en CI.
40. **Runbook Cross-Tenant Leak Response** — documento markdown listo para `docs/runbooks/`.
41. **Threat Model STRIDE** — documento inicial con componentes críticos + tabla de amenazas.
42. **Roadmap 8 semanas MVP** — **Sprint 1–2 incluye**: setup de Roslyn analyzer + test suite isolation + middleware ITenantContext + RLS policies + ITenantScoped wrappers. Todo lo relacionado con aislamiento es **foundational**, no se deja para el final. Sprints con {objetivo, entregables, DoD, dependencias, riesgos} para 3 devs + 1 PM.
43. **Plan migración .NET 9 → 10**.
44. **Top 10 riesgos técnicos** — prob × impacto × mitigación × owner. Incluye: **cross-tenant leak (riesgo #1)**, meter drift, impersonation abuse, PII leak en logs, Cloudflare Access mis-config, SW Sapien outage, Novita cost explosion, multi-branch data corruption en sync, Stripe webhook replay, SQL Server RLS policy bug.
45. **Explícitamente fuera de MVP** — scope cuts.

## 20. Restricciones de la Respuesta

- Cuestiona ambigüedades antes de asumir.
- Cada decisión: alternativa descartada con razón.
- Prioriza tablas, snippets C#/Razor/SQL ejecutables, DDL concreto, diagramas sobre prosa.
- Nada de "porque está de moda" — razón operativa + costo + compliance + impacto aislamiento.
- Nivel: equipo empieza el lunes.
- Si detectas inconsistencia, flagea en Assumptions.
- **Respeta convenciones §3**: MudBlazor 100%, Mapster, Mediator (Othamar), Serilog, MFA, cero hard deletes, catálogos acotados, marketing MVC, Roslyn analyzer de aislamiento.
- **Cada decisión relevante**: costo mensual + impacto unit economics cuando aplique + **impacto aislamiento tenant cuando aplique**.
- **Usage tracking transversal**: todo evento facturable invoca `UsageTracker`.
- **Super-admin ops transversales**: toda acción durante impersonation registra + dispara notificación.
- **PII masking obligatorio** dos niveles (Novita + Serilog).
- **Aislamiento tenant es invariante del sistema, no feature**: un bug aquí es escenario existencial. Toda capa §16 se entrega como código ejecutable, no descripción. La suite `TallerPro.Isolation.Tests` corre en CI desde día 1 del Sprint 1.
- **Regla de oro del founder**: si una línea de código te hace dudar si cruza tenants, la respuesta es no. Si hay duda, pregunta con PR label `security-review` antes de mergear.
