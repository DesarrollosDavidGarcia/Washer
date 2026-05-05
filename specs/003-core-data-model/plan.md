# Plan: Modelo de datos base multi-tenant (EF Core)

- **Spec**: `specs/003-core-data-model/spec.md`
- **Estado**: draft

## Arquitectura

### Capas afectadas

```
TallerPro.Domain                    (entidades + interfaces base)
    ↑
TallerPro.Application               (sin cambios — esta spec no añade casos de uso)
    ↑
TallerPro.Infrastructure            (DbContext, configuraciones, interceptores, migraciones)
    ↑
TallerPro.Security                  (ITenantContext + MissingTenantContextException)
    ↑
TallerPro.Analyzers                 (TP0001 mínimo viable — bloqueo IgnoreQueryFilters)
```

### Flujo de un `SaveChanges`

```
caller (test/seed)
   │
   ├─ tracked changes
   ▼
TallerProDbContext.SaveChangesAsync
   │
   ├─ AuditingInterceptor          → set CreatedAt/UpdatedAt (UTC), CreatedBy*/UpdatedBy* desde ICurrentUser
   ├─ SoftDeleteInterceptor         → convierte EntityState.Deleted en IsDeleted=true + DeletedAt=UtcNow
   ├─ FundationalTenantGuard        → bloquea soft delete del tenant fundacional (Id=1)
   ├─ TenantStampInterceptor        → set TenantId en entidades nuevas tenant-scoped
   ├─ EF Core: aplica RowVersion + lanza DbUpdateConcurrencyException si stale
   ▼
SQL Server
```

### Flujo de query con global filters

```
DbSet<Branch>.Where(...)
   │
   ├─ global filter soft delete:    e => !e.IsDeleted
   ├─ global filter tenant:         e => e.TenantId == _tenantContext.CurrentTenantId  ← throw si nulo
   ▼
SQL Server (parámetros @TenantId, @IsDeleted)
```

## Componentes

| Componente | Cambio | Tipo |
|---|---|---|
| `src/TallerPro.Domain/Common/IAuditable.cs` | nuevo | interfaz |
| `src/TallerPro.Domain/Common/ISoftDeletable.cs` | nuevo | interfaz |
| `src/TallerPro.Domain/Common/IConcurrencyAware.cs` | nuevo | interfaz |
| `src/TallerPro.Domain/Common/ITenantOwned.cs` | nuevo | interfaz |
| `src/TallerPro.Domain/Common/PiiDataAttribute.cs` | nuevo | atributo |
| `src/TallerPro.Domain/Tenants/Tenant.cs` | nuevo | entidad |
| `src/TallerPro.Domain/Tenants/TenantStatus.cs` | nuevo | enum |
| `src/TallerPro.Domain/Tenants/Branch.cs` | nuevo | entidad |
| `src/TallerPro.Domain/Auth/User.cs` | nuevo | entidad |
| `src/TallerPro.Domain/Auth/PlatformAdmin.cs` | nuevo | entidad |
| `src/TallerPro.Domain/Auth/UserBranchAccess.cs` | nuevo | entidad |
| `src/TallerPro.Domain/Auth/Role.cs` | nuevo | enum (`SuperAdmin`, `Admin`, `Cliente`) |
| `src/TallerPro.Security/ITenantContext.cs` | nuevo | interfaz |
| `src/TallerPro.Security/MissingTenantContextException.cs` | nuevo | excepción |
| `src/TallerPro.Security/ICurrentUser.cs` | nuevo | interfaz para auditoría |
| `src/TallerPro.Infrastructure/Persistence/TallerProDbContext.cs` | nuevo | DbContext |
| `src/TallerPro.Infrastructure/Persistence/TallerProDbContextFactory.cs` | nuevo | design-time factory |
| `src/TallerPro.Infrastructure/Persistence/Configurations/*.cs` | nuevo | 5 IEntityTypeConfiguration |
| `src/TallerPro.Infrastructure/Persistence/Interceptors/AuditingInterceptor.cs` | nuevo | interceptor |
| `src/TallerPro.Infrastructure/Persistence/Interceptors/SoftDeleteInterceptor.cs` | nuevo | interceptor |
| `src/TallerPro.Infrastructure/Persistence/Interceptors/FundationalTenantGuard.cs` | nuevo | interceptor |
| `src/TallerPro.Infrastructure/Persistence/Interceptors/TenantStampInterceptor.cs` | nuevo | interceptor |
| `src/TallerPro.Infrastructure/Persistence/Migrations/0001_CoreDataModel.cs` | nuevo | migración generada |
| `src/TallerPro.Infrastructure/Logging/PiiMaskingPolicy.cs` | nuevo | Serilog destructuring policy |
| `src/TallerPro.Analyzers/IgnoreQueryFiltersAnalyzer.cs` | nuevo | TP0001 mínimo |
| `src/TallerPro.Analyzers/AllowIgnoreQueryFiltersAttribute.cs` | nuevo | atributo de supresión |
| `tests/TallerPro.Integration.Tests/Persistence/*.cs` | nuevo | tests con Testcontainers |
| `tests/TallerPro.Isolation.Tests/CrossTenantLeakTests.cs` | nuevo | test crítico CA-04 |

## Datos

Ver `data-model.md` para tablas/columnas/índices/constraints completos. Resumen:

- **Entidades nuevas (5)**: `Tenant`, `Branch`, `User`, `UserBranchAccess`, `PlatformAdmin`.
- **Schemas**: `core` (Tenant, Branch); `auth` (User, UserBranchAccess, PlatformAdmin).
- **Migración**: `0001_CoreDataModel` — crea schemas, tablas, índices, FKs, RowVersion, e inserta tenant fundacional `tallerpro-platform`.

## Contratos

No aplica en esta spec (sin endpoints, eventos ni interfaces públicas). Los contratos cross-spec relevantes son:

- **Para spec 004 (Auth)**: respuestas de login deben ser **timing-safe** y mensaje uniforme entre "email no existe" y "credenciales incorrectas" (mitiga ALTO-03 — enumeración de emails).
- **Para spec 005 (Analyzers)**: TP0001 se completa con la lógica plena en spec 005; esta spec entrega placeholder mínimo que ya bloquea `IgnoreQueryFilters()` sin atributo.

## Decisiones

### D-01: `ITenantContext.CurrentTenantId` lanza excepción si nulo (CRITICO-01)

`CurrentTenantId` es de tipo `long` (no nullable). Acceder a la propiedad cuando no hay tenant resuelto lanza `MissingTenantContextException`. **No** existe rama de fallback "sin tenant = ver todo" en los global filters. Las únicas vías para bypass son:
- `IgnoreQueryFilters()` con `[AllowIgnoreQueryFilters("razón")]` documentado.
- Un `IPlatformContext` separado (futuro) que solo super-admins autenticados pueden activar.

**Alternativas**: nullable + check defensivo (rechazada — patrón propenso a olvidos); excepción bajo demanda dentro del filter (rechazada — el filter se traduce a SQL y la excepción no propagaría).

**Por qué**: defense-in-depth (constitución §Principio #6, §Restricciones #10). CERO tolerancia a leaks.

### D-02: `CreatedBy`/`UpdatedBy` con dos columnas FK tipadas (ALTO-02)

Adoptamos **Opción A** del security review: cada entidad auditable tiene cuatro columnas en lugar de dos:

```
CreatedByUserId        bigint NULL  → FK a auth.Users(Id)
CreatedByPlatformId    bigint NULL  → FK a auth.PlatformAdmins(Id)
UpdatedByUserId        bigint NULL  → FK a auth.Users(Id)
UpdatedByPlatformId    bigint NULL  → FK a auth.PlatformAdmins(Id)
```

**Constraint** `CK_<Entity>_CreatedByExclusive`: `(CreatedByUserId IS NULL OR CreatedByPlatformId IS NULL)` — mutuamente excluyentes. Ambos nulos = acción de sistema (seed, migración).

**Alternativas**: columna `long` + enum `CreatedByType` (rechazada — pierde FK referencial; el security review lo notó como Opción B inferior).

**Por qué**: trazabilidad forense unívoca + FK intactas para joins de UI ("creado por X usuario / Y admin").

→ **ADR-0010**.

### D-03: `UserBranchAccess` con `TenantId` denormalizado (MEDIO-02)

`UserBranchAccess` añade columna `TenantId` (FK a `Tenants`) replicada de `Branch.TenantId`. El interceptor `TenantStampInterceptor` la setea desde `Branch.TenantId` al insertar; un trigger de check garantiza consistencia con `Branch.TenantId`. Con esto, el global filter de tenant aplica directamente a `UserBranchAccesses` y queries `dbContext.UserBranchAccesses.Where(...)` no pueden bypass tenant scoping.

**Alternativas**: solo filtrar implícitamente vía join a `Branch` (rechazada — el security review demostró que queries directas al DbSet rompen el invariante).

**Por qué**: evitar dependencia de "siempre hacer join". Defense-in-depth.

### D-04: Tenant fundacional `tallerpro-platform`, protegido contra soft delete (MEDIO-01)

Slug del tenant fundacional: `tallerpro-platform` (no `system`, no `admin`, no `tallerpro` solo). Status `Active`. Sembrado en migración 0001 con `Id = 1` (consistente con `bigint IDENTITY` que arranca en 1).

`FundationalTenantGuard` (interceptor) lanza `InvalidOperationException` si `SaveChanges` intenta soft delete o status change a `Cancelled` del tenant `Id = 1`.

**Slugs reservados** (rechazados en validación de creación):
```
system, admin, root, api, www, mail, tallerpro, tallerpro-platform,
test, demo, platform, super, internal, app, dashboard
```

### D-05: PII masking vía atributo `[PiiData]` + Serilog policy (MEDIO-03)

- `User.Email`, `PlatformAdmin.Email` → `[PiiData(level: High)]`.
- `User.DisplayName`, `PlatformAdmin.DisplayName` → `[PiiData(level: Low)]`.

`PiiMaskingPolicy` (Serilog) destructura objetos detectando el atributo:
- `High` → `***@***` (literal fijo, sin hash).
- `Low` → primer carácter + `***`.

> **Refinamiento ronda 2 (O-1, 2026-05-04)**: la propuesta inicial incluía un hash SHA-256 truncado a 8 chars como sufijo del `***@***` para correlación forense. Se eliminó tras code review: el `actorId` numérico ya viaja en el log estructurado y es suficiente para correlación; un hash de 32 bits tiene ~1.2% colisión con 10k usuarios, lo que rompe trazabilidad sin beneficio.

Los logs de auditoría del interceptor solo emiten `EntityName`, `EntityId`, `Operation`, `TenantId`, `ActorType` (User/PlatformAdmin/System), `ActorId`. **Nunca** emiten Email/DisplayName.

### D-06: TP0001 mínimo en spec 003 (ALTO-01)

`TallerPro.Analyzers` recibe en spec 003 el primer diagnóstico real:
- **TP0001 (error)**: `IgnoreQueryFilters()` invocado sin que el método contenedor tenga `[AllowIgnoreQueryFilters("razón")]`.
- Atributo `AllowIgnoreQueryFiltersAttribute` con propiedad `Reason` requerida (string no vacío).

Lógica completa de TP0002-TP0004 sigue diferida a spec 005. TP0005 (Console.WriteLine) ya existe desde spec 002.

### D-07: Identificadores `bigint IDENTITY` (Q-01)

Todas las PKs son `bigint IDENTITY(1,1)`. URLs públicas y APIs externas exponen `Slug`/`Code` (Tenant, Branch) o `Email` (User), nunca el `Id` numérico.

→ **ADR-0008**.

### D-08: `User` global sin `TenantId` (Q-02)

`User` no tiene columna `TenantId`. Acceso multi-tenant 100% vía `UserBranchAccess`. Email único global. **Restricción cross-spec**: spec 004 debe implementar respuestas de auth timing-safe + mensajes genéricos.

→ **ADR-0009**.

## Pruebas

### Unit (`TallerPro.Domain.Tests`)
- Validar invariantes de entidades: `Tenant.Slug` no nulo, `Branch.Code` no vacío, `Role` enum solo acepta valores definidos.
- `MissingTenantContextException` se lanza desde `TenantContext` cuando `CurrentTenantId` se accede sin set.

### Integración (`TallerPro.Integration.Tests` — Testcontainers SQL Server)
- **CA-02**: `dotnet ef migrations script --idempotent` produce SQL aplicable y reaplicable.
- **CA-03**: aplicar migración inicial; crear 1 tenant + 2 branches + 1 user + 2 accesses + 1 platform admin; queries básicas pasan.
- **CA-05**: soft delete oculta entidad en queries normales; visible con `IgnoreQueryFilters()`.
- **CA-06**: auditoría poblada sin código manual.
- **CA-09**: update con `RowVersion` desactualizado lanza `DbUpdateConcurrencyException`.
- **CA-10**: tras migración 0001, existe tenant fundacional `tallerpro-platform` con status `Active`.
- **CA-11**: query `Users` no retorna platform admins.
- **D-04 guard**: intento de soft delete del tenant fundacional lanza `InvalidOperationException`.
- **D-03 denormalización**: insertar `UserBranchAccess` setea `TenantId` automáticamente desde `Branch.TenantId`.

### Aislamiento (`TallerPro.Isolation.Tests` — Testcontainers SQL Server)
- **CA-04 (crítico)**: 2 tenants; `ITenantContext = tenantA`; query `Branch` retorna **solo** branches de tenantA. Falla intencional si el filtro se desactiva.
- **CA-04 b**: query directa a `UserBranchAccesses` con `tenantA` no retorna accesses cuyo `TenantId` sea `tenantB` (valida D-03).
- **CA-07**: query a entidad tenant-scoped sin `ITenantContext` resuelto → `MissingTenantContextException` propagada.

### Analyzer (`tests/TallerPro.Domain.Tests` o proyecto dedicado)
- Compilar código que use `IgnoreQueryFilters()` sin `[AllowIgnoreQueryFilters]` debe fallar build con TP0001.
- Compilar código con el atributo + `Reason` no vacío compila OK.

### Manual: `quickstart.md`
- Cómo levantar SQL Server local, aplicar migraciones, correr seed dev.

## Riesgos

| Riesgo | Impacto | Mitigación |
|---|---|---|
| `ITenantContext` mal cableado en DI → fallback silencioso | **Crítico** (cross-tenant leak) | D-01 + test CA-04/CA-07 explícito; throw eager |
| Lock files generados con RID inconsistente entre OS | Medio (CI roto) | Spec 002 ya commiteó lock files. Esta spec añade lock para `Infrastructure` con Testcontainers — correr `dotnet restore` localmente Windows + commitear |
| Migración 0001 no idempotente (re-aplicación rompe BD) | Alto (rollback complejo) | CA-02 verifica `--idempotent`; revisión de PR obligatoria |
| Bug en `FundationalTenantGuard` → tenant fundacional borrable | Alto (referencias huérfanas) | Test dedicado + integration test que intenta el delete |
| TP0001 demasiado estricto bloquea código legítimo | Medio (DX) | `[AllowIgnoreQueryFilters("razón")]` con razón documentada permite escape válido |
| Schemas múltiples confunden runbooks de DBA | Bajo | Documentar en `quickstart.md` qué tabla está en qué schema |
| `RowVersion` mal mapeado → conflictos espurios | Bajo | Test CA-09 + integración con concurrencia |

## Despliegue

- **Flags**: ninguna (cambio de schema, no de comportamiento).
- **Backfill**: N/A en MVP — primera migración. En producción futura, `0001_CoreDataModel` se aplicará en BD vacía (no hay datos previos a backfillear).
- **Rollback**: `dotnet ef database update 0` para revertir a estado previo a 0001 (BD vacía sin schemas). Aceptable solo en pre-producción. En producción la primera vez no aplica rollback (sin estado anterior). Migraciones futuras llevarán plan de rollback dedicado.
- **Connection string**: `appsettings.json` para dev; `TALLERPRO_DB_CONNECTION` env var para prod (override). Secrets gestionados con `sops + age` (constitución §Herramientas).

## Observabilidad

- **Logs (Serilog)**:
  - `AuditingInterceptor` emite log `Information` por entidad afectada en `SaveChanges`. Schema:
    ```
    { entityName, entityId, operation: Insert|Update|Delete,
      tenantId, actorType: User|PlatformAdmin|System, actorId,
      durationMs, rowVersion }
    ```
  - **Sin PII** (D-05).
- **Métricas** (Grafana Cloud Free):
  - `db.savechanges.duration_ms` (histograma por `entityName`, `operation`).
  - `db.savechanges.errors_total` (counter por `exceptionType`).
  - `db.concurrency_conflicts_total` (counter por `entityName`).
- **Alertas**:
  - `db.concurrency_conflicts_total > 5/min` → warn (posible bug de UI).
  - **Crítico**: cualquier query log que muestre `tenantId=null` accediendo a entidad tenant-scoped → page (debería ser imposible si D-01 funciona).

## Hallazgos de seguridad — security-reviewer (2026-05-04)

| Id | Severidad | Estado en plan |
|---|---|---|
| CRITICO-01 | Crítico | Mitigado (D-01) — `ITenantContext.CurrentTenantId` no nullable, throw eager |
| ALTO-01 | Alto | Mitigado (D-06) — TP0001 placeholder en spec 003, lógica plena spec 005 |
| ALTO-02 | Alto | Mitigado (D-02 / ADR-0010) — dos columnas FK tipadas |
| ALTO-03 | Alto | Mitigado (contrato a spec 004) — timing-safe documentado en sección Contratos |
| MEDIO-01 | Medio | Mitigado (D-04) — tenant fundacional `tallerpro-platform` + guard |
| MEDIO-02 | Medio | Mitigado (D-03) — `TenantId` denormalizado en `UserBranchAccess` |
| MEDIO-03 | Medio | Mitigado (D-05) — `[PiiData]` + Serilog policy |
| BAJO-01 | Bajo | Documentado (sin operaciones de transfer en spec 003; revisitar en spec con cambios de rol) |
| INFO-01 | Info | Documentado (User y PlatformAdmin son lifecycles independientes; offboarding en spec posterior) |

**Bloqueantes resueltos**: 0 pendientes. Plan READY para `/speckit.tasks`.

## ADRs propuestos

- **ADR-0008**: Estrategia de identificadores — `bigint IDENTITY` (decisión D-07 / Q-01). Borrador en `docs/decisions/ADR-0008-bigint-identity.md`.
- **ADR-0009**: `User` global sin `TenantId` (decisión D-08 / Q-02). Borrador en `docs/decisions/ADR-0009-user-global-no-tenant.md`.
- **ADR-0010**: Auditoría con columnas FK tipadas para discriminar User vs PlatformAdmin (decisión D-02). Borrador en `docs/decisions/ADR-0010-auditoria-actor-tipado.md`.

## Referencias

- `specs/003-core-data-model/spec.md`, `clarify.md`
- `.specify/memory/constitution.md` §Identidad, §Restricciones #6, #8, #10, #11
- `.specify/memory/stack.md` §Persistencia, §Testing
- `docs/decisions/ADR-0001-bootstrap-monorepo-y-cpm.md` D-2 (analyzer)
- Security review (este plan §Hallazgos)
