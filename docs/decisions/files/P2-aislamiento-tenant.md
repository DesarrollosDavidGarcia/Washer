# Prompt 2 de 7 — Aislamiento Tenant (Zero Cross-Tenant Leaks)

## Contexto del Proyecto

**TallerPro**: SaaS multitenant + multitaller para talleres mecánicos en México. Arranque con 10 tenants en 90 días. Founder + 3 devs + 1 PM.

**Stack confirmado**: .NET Core 9 + Blazor Hybrid (MAUI) + MudBlazor + Mediator (Othamar) + Mapster + EF Core + SQL Server + SignalR + Stripe Billing/Meters + DeepSeek vía Novita + SW Sapien.

**Tolerancia a cross-tenant leak: CERO**. Un solo incidente es escenario existencial (legal LFPDPPP + reputacional + fiscal SAT).

**Decisiones fundacionales ya tomadas (asumir)**:
- Multitenancy: Single DB + `TenantId` discriminator + EF Core Global Query Filters + SQL Server Row-Level Security como defensa en profundidad.
- Patrón arquitectónico: Vertical Slice en `TallerPro.Application` + Clean Architecture en capas externas.
- Auth: ASP.NET Core Identity + Duende IdentityServer (o Microsoft Entra External ID — ambos soportan multi-taller bien).
- API style: Minimal APIs para cliente Hybrid + MVC clásico para admin portal.
- Proyecto `TallerPro.Analyzers` dedicado a Roslyn analyzers.
- Proyecto `TallerPro.Isolation.Tests` dedicado a test suite de aislamiento.

---

## Tu Rol

Actúa como **CTO + Arquitecto de Software Senior** especializado en:
- Defense-in-depth para aislamiento tenant en SaaS B2B sobre .NET + SQL Server.
- Roslyn analyzers, source generators, test automation para enforcement de invariantes.
- Incident response, runbooks operacionales, threat modeling STRIDE.
- Cumplimiento LFPDPPP México (notificación INAI Art. 64).

Responde como arquitecto hablando con founder técnico. Cada componente debe ser **código ejecutable**, no descripción.

---

## Alcance de ESTE prompt (P2)

Entregar la **arquitectura completa y código ejecutable** del sistema de aislamiento tenant. Este es el trabajo más crítico del proyecto. Se ejecuta en Sprint 1-2 antes que cualquier feature de negocio.

**SÍ incluir**:
1. Documentación de las 8 capas de defensa con código de cada una.
2. Roslyn Analyzer `TallerPro.Analyzers` completo y compilable.
3. Test Suite `TallerPro.Isolation.Tests` con al menos 10 test cases.
4. Runbook de incident response en markdown.
5. Threat Model STRIDE documento inicial.

**NO incluir**:
- DDL de base de datos (eso es P3, aquí solo referencias a tablas).
- Lógica de impersonation (eso es P4).
- Lógica de pagos (eso es P5).
- Lógica de IA (eso es P6).

---

## Formato de Respuesta Esperado

### 1. Principio de Aislamiento Tenant

Declaración del invariante del sistema en 3-5 oraciones. Debe ser la "oración memorable" que el equipo recita.

### 2. Las 8 Capas de Defensa — Código Ejecutable

Para cada capa, entrega:

**Capa 1: Autenticación JWT firmado**
- Config de `AddAuthentication().AddJwtBearer(...)` en Program.cs.
- Claims obligatorios: `sub`, `tenant_id`, `role`, `iat`, `exp`.
- Key rotation strategy.
- TTL 15 min access token + 30 días refresh.

**Capa 2: Middleware `ITenantContext`**
- Código C# completo de:
  - Interfaz `ITenantContext` con `CurrentTenantId`, `CurrentUserId`, `IsImpersonating`, `ImpersonatedTenantId`.
  - Clase `TenantContextMiddleware` que resuelve desde JWT.
  - Registro en DI como Scoped.
  - Orden correcto en el pipeline de middlewares.
- Resolución prioritaria: primero claim `impersonation.tenant_id` si existe, luego `tenant_id` normal, si ninguno → 401.

**Capa 3: EF Core Global Query Filters**
- Código de `ApplicationDbContext.OnModelCreating` con filtros automáticos aplicados a todas las entidades tenant-scoped.
- Detección de entidades via convention (ej: implementan `ITenantScoped`).
- Filtro adicional de soft delete (`DeletedAt IS NULL`).
- Trampa documentada: `IgnoreQueryFilters()` se bloquea vía Roslyn analyzer en capa 3.5.

**Capa 4: SQL Server Row-Level Security**
- Script SQL para crear funciones de filtro + security policies para las tablas críticas (ServiceOrders, Customers, Vehicles, Parts, CFDIs, Payments).
- Middleware que setea `SESSION_CONTEXT(N'TenantId', @tenantId)` al inicio de cada request/transacción.
- Cómo manejar el SESSION_CONTEXT para jobs y admin operations.

**Capa 5: Índices Compuestos tenant-first**
- Convención en DDL: todos los índices de entidades tenant-scoped empiezan con `TenantId`.
- Script de verificación que corre en CI (DDL diff) que detecta tablas nuevas sin índice tenant-first.

**Capa 6: Segregación cache + storage + logs**

Código C# completo de:
- Interfaz `ITenantScopedCache` con métodos `GetAsync<T>(key)`, `SetAsync<T>(key, value, ttl)`, `RemoveAsync(key)`. Implementación sobre `IDistributedCache` con prefijo automático `tenant:{TenantId}:`.
- Interfaz `ITenantScopedStorage` para blob/file storage con path auto-segregado `/{TenantId}/{BranchId}/...`.
- Serilog enricher `TenantEnricher` que agrega `TenantId` a cada log event desde `ITenantContext`.

**Capa 7: SignalR Groups por tenant**

Código C# completo de:
- Base class `TenantScopedHub<T>` que override `OnConnectedAsync` para auto-join al grupo `tenant:{TenantId}`.
- Prohibir send a grupos no-scoped mediante convención + code review.

**Capa 8: Anomaly Detection**

Código C# de:
- Background service `TenantIsolationAnomalyDetector` (IHostedService).
- Query diaria sobre `AuditLog` que detecta: "request autenticado como TenantId=A accedió a entidad con TenantId=B".
- Alert trigger: Slack webhook + email al founder.
- Tabla `IsolationAnomalies` para histórico.

### 3. Roslyn Analyzer — `TallerPro.Analyzers` Completo

Proyecto `.csproj` completo (con referencias, target framework, nuget packages).

Implementación completa de las 5 reglas:

**TP0001 — IgnoreQueryFilters sin `[AllowCrossTenant]`**
- Código completo del `DiagnosticAnalyzer`.
- `CodeFixProvider` que sugiere agregar el atributo.
- Tests del analyzer en `TallerPro.Analyzers.Tests`.

**TP0002 — SQL raw con interpolación de strings**
- Detecta `FromSqlRaw($"...{var}...")` y `ExecuteSqlRaw` similar.
- Sugiere `FromSqlInterpolated` o parámetros tipados.

**TP0003 — IDistributedCache / IMemoryCache directo**
- Detecta inyección o uso de estos tipos fuera de `TallerPro.Infrastructure`.
- Sugiere `ITenantScopedCache`.

**TP0004 — BlobServiceClient / IBlobStorage directo**
- Similar a TP0003 para storage.
- Sugiere `ITenantScopedStorage`.

**TP0005 — Controller sin `[Authorize]` ni `[AllowAnonymous]`**
- Detecta controllers/actions sin atributo explícito.
- Warning (no error) para permitir migración gradual.

Código del atributo `[AllowCrossTenant]`:
```csharp
namespace TallerPro.Security;
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class AllowCrossTenantAttribute : Attribute
{
    public string Reason { get; }
    public AllowCrossTenantAttribute(string reason) { ... }
}
```

Configuración en `.csproj` de los proyectos que deben usar el analyzer:
```xml
<ItemGroup>
  <ProjectReference Include="..\TallerPro.Analyzers\TallerPro.Analyzers.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

### 4. Test Suite — `TallerPro.Isolation.Tests` Completo

Estructura del proyecto de tests + código de:

**`IsolationTestBase`** — base class con:
- Setup de WebApplicationFactory con in-memory SQL Server o Testcontainers.
- Helpers: `CreateTenantWithUser()`, `CreateJwtToken(tenantId, userId, role)`, `SeedResourceForTenant()`, `GetAuditLogForTenant()`.
- Cleanup entre tests.

**`[TenantScopedEndpoints]` data attribute** — enumera endpoints tenant-scoped del ensamblado `TallerPro.Api` vía reflection. Código completo incluyendo cómo filtrar por `[Authorize]` y excluir endpoints `[AdminOnly]`.

**Al menos 10 test cases** representativos de los 12 vectores, con código completo:
1. `Endpoint_WithTenantAToken_CannotAccessTenantBResource` (Theory con `[TenantScopedEndpoints]`).
2. `Endpoint_CannotWriteToOtherTenantResource`.
3. `Cache_TenantACannotReadTenantBCachedData`.
4. `Storage_TenantACannotDownloadTenantBBlob`.
5. `SignalR_TenantACannotJoinTenantBGroup`.
6. `SyncEndpoint_ReturnsOnlyTenantAData`.
7. `Export_CannotIncludeOtherTenantRows`.
8. `Webhook_UnknownCustomerId_IsDiscardedAndAlerted`.
9. `BackgroundJob_TenantScoped_OnlyProcessesGivenTenant`.
10. `Logs_QueryByTenantA_DoesNotLeakTenantBLogs`.

**Configuración GitHub Actions** para que la suite corra en cada PR:
- Workflow `.github/workflows/isolation-tests.yml`.
- Fail the build on any test failure.
- Runtime budget: < 3 min total.

### 5. Runbook de Incident Response (Cross-Tenant Leak)

Documento markdown completo `docs/runbooks/tenant-leak-response.md` con 7 fases detalladas:

**Fase 1 — Detección y clasificación (0-30 min)**: triggers, severidades P0-P3, asignación de Incident Commander.

**Fase 2 — Contención (30-120 min)**: feature flags, read-only mode, session termination, key rotation, snapshot evidence.

**Fase 3 — Comunicación (primeras 48 h)**: template de email inicial < 2h, template de seguimiento < 24h, llamada a enterprise, plantilla de notificación INAI (LFPDPPP Art. 64), comunicación interna, estrategia pública si aplica.

**Fase 4 — Forensics (días 2-7)**: queries Serilog específicos para cross-reference, preservación de evidencia, timeline reconstruction.

**Fase 5 — Remediación (días 3-14)**: fix con 2+ reviewers, tests de regresión, deploy plan, compensación a afectados.

**Fase 6 — Post-mortem (semana 2-3)**: 5 Whys, Ishikawa, gap analysis de las 8 capas, action items, publicación a tenants.

**Fase 7 — Prevención sistémica (mes 1-3)**: pen testing, review de patrones similares, training.

Cada fase con checklist ejecutable.

### 6. Threat Model STRIDE

Documento markdown `docs/security/threat-model.md` con:

Por cada componente crítico (API, Hybrid, Admin Portal, Webhooks, Impersonation, Sync), tabla STRIDE:

| Componente | Spoofing | Tampering | Repudiation | Info Disclosure | DoS | Elevation |
|---|---|---|---|---|---|---|
| API | ... | ... | ... | ... | ... | ... |
| ... | | | | | | |

Cada celda con: descripción de amenaza, probabilidad (Low/Med/High), impacto (Low/Med/High), mitigación actual, owner.

### 7. Plan de Pen Testing

Documento breve con:
- Fases (pre-soft-launch, post-GA, anual).
- Scope white-box.
- Firmas sugeridas con justificación (MX + globales).
- Presupuesto estimado.
- Entregables esperados.

---

## Restricciones de la Respuesta

- **Código ejecutable, no pseudocódigo**. Cada snippet debe compilarse sin errores con referencias NuGet correctas.
- Usa C# moderno (.NET 9): primary constructors, file-scoped namespaces, `ValueTask<T>`, collection expressions.
- Prioriza snippets compilables sobre prosa.
- Cada decisión: alternativa descartada con razón.
- Longitud target: ~15,000 tokens de respuesta.
- Convenciones obligatorias del proyecto: Mediator (Othamar) no MediatR, Mapster no AutoMapper, Serilog, FluentValidation, Shouldly en tests, NSubstitute en mocks.

---

## Al final de tu respuesta

Genera un bloque **"ADR Update — Tenant Isolation"** con las decisiones específicas que este prompt cementó (para consistencia con P3-P7).
