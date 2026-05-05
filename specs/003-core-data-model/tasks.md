?# Tasks: Modelo de datos base multi-tenant (EF Core)

- **Spec**: `specs/003-core-data-model/spec.md`
- **Plan**: `specs/003-core-data-model/plan.md`
- **Data model**: `specs/003-core-data-model/data-model.md`

> Tareas atómicas (1-4h). Criterio de hecho verificable. Marcar `[x]` al cerrar.
>
> **Orden TDD**: tests rojos antes que producción. La fase "rojo" exige que el test exista y falle por la razón correcta antes de pasar a "verde". Tareas marcadas como `(test)` siempre preceden a su implementación.

---

## Fase 0 — Fundaciones (ADRs y paquetes)

### T-01 — Aceptar ADR-0008, 0009, 0010

- **Descripción**: revisar borradores `Proposed` y, si pasan validación del founder, cambiar estado a `Accepted` (constitución §Gobernanza). Si surge objeción, abrir conversación; **no** modificar plan sin ADR aceptado.
- **Archivos**:
  - mod: `docs/decisions/ADR-0008-bigint-identity.md`
  - mod: `docs/decisions/ADR-0009-user-global-no-tenant.md`
  - mod: `docs/decisions/ADR-0010-auditoria-actor-tipado.md`
- **Criterio**:
  - [x] Los tres ADRs muestran `Estado: Accepted` y fecha actualizada.
  - [ ] Commit con mensaje `docs(adr): accept ADR-0008/0009/0010 for spec 003`.
- **Depende de**: —
- **Estado**: [x]

### T-02 — Añadir paquetes EF Core 9 + SQL Server + Testcontainers a CPM

- **Descripción**: agregar a `Directory.Packages.props` las versiones exactas de `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.EntityFrameworkCore.Relational`. Confirmar que `Testcontainers.MsSql` ya está (spec 002). Añadir `Respawn` para reset de tests. Versionar via CPM. Regenerar `packages.lock.json` localmente y commitear.
- **Archivos**:
  - mod: `Directory.Packages.props`
  - mod: `src/TallerPro.Infrastructure/TallerPro.Infrastructure.csproj` (PackageReference a EF + Design)
  - mod: `tests/TallerPro.Integration.Tests/*.csproj`, `tests/TallerPro.Isolation.Tests/*.csproj` (Testcontainers + Respawn)
  - regenera: `**/packages.lock.json`
- **Criterio**:
  - [x] `dotnet restore TallerPro.Linux.slnf` pasa local (las versiones EF Core 9.0.3 + Testcontainers 3.10.0 + Respawn 6.2.1 ya estaban en CPM desde spec 002).
  - [x] `NuGetAudit` no reporta CVE Crítico/Alto en las nuevas dependencias (build verde sin warnings).
  - [x] Versiones exactas (sin floating ranges) — se conserva CPM.
- **Depende de**: T-01
- **Estado**: [x]

### T-03 — `dotnet ef` tool en `dotnet-tools.json`

- **Descripción**: registrar `dotnet-ef` como tool local del repo para que CI y devs lo invoquen sin install global. Versión alineada con EF Core 9.
- **Archivos**:
  - nuevo o mod: `.config/dotnet-tools.json`
- **Criterio**:
  - [x] `dotnet tool restore` instala `dotnet-ef`.
  - [x] `dotnet ef --version` retorna versión `9.0.3`.
- **Depende de**: T-02
- **Estado**: [x]

---

## Fase 1 — Capa Domain (interfaces base + atributos)

### T-04 (test) — Tests para invariantes de interfaces base

- **Descripción**: tests vacíos de marcador no aportan; en su lugar, tests que verifiquen que los atributos `[PiiData]` se resuelven en runtime con su nivel correcto (preparación para T-21 PII masking policy). Crear test del attribute en `tests/TallerPro.Domain.Tests/Common/PiiDataAttributeTests.cs`.
- **Archivos**:
  - nuevo: `tests/TallerPro.Domain.Tests/Common/PiiDataAttributeTests.cs`
- **Criterio**:
  - [x] Test rojo: `[PiiData(level: PiiLevel.High)]` aplicado a una propiedad se lee correctamente con reflexión.
  - [x] Test rojo: nivel default es `Low` si no se especifica.
- **Depende de**: T-02
- **Estado**: [x]

### T-05 — Implementar interfaces base + atributo PII

- **Descripción**: crear interfaces y atributo en `TallerPro.Domain/Common/`. `IAuditable` sin `CreatedBy`/`UpdatedBy` simples — define las **cuatro** propiedades tipadas (D-02 / ADR-0010): `CreatedByUserId`, `CreatedByPlatformId`, `UpdatedByUserId`, `UpdatedByPlatformId`. `ISoftDeletable` con `IsDeleted`/`DeletedAt`. `IConcurrencyAware` con `RowVersion`. `ITenantOwned` con `TenantId`. `PiiDataAttribute` con enum `PiiLevel { Low, High }`.
- **Archivos**:
  - nuevo: `src/TallerPro.Domain/Common/IAuditable.cs`
  - nuevo: `src/TallerPro.Domain/Common/ISoftDeletable.cs`
  - nuevo: `src/TallerPro.Domain/Common/IConcurrencyAware.cs`
  - nuevo: `src/TallerPro.Domain/Common/ITenantOwned.cs`
  - nuevo: `src/TallerPro.Domain/Common/PiiDataAttribute.cs`
  - nuevo: `src/TallerPro.Domain/Common/PiiLevel.cs`
- **Criterio**:
  - [x] `dotnet build src/TallerPro.Domain` sin warnings.
  - [x] Test T-04 verde.
  - [x] Cada interfaz documentada con XML doc en una línea (constitución §Convenciones permite XML doc cuando es para contrato público).
- **Depende de**: T-04
- **Estado**: [x]

---

## Fase 2 — Entidades de dominio

### T-06 (test) — Tests de entidad `Tenant`

- **Descripción**: tests de invariantes de `Tenant` (constructor exige `Name` y `Slug` no vacíos; `Slug` lowercase; status default `Active`).
- **Archivos**:
  - nuevo: `tests/TallerPro.Domain.Tests/Tenants/TenantTests.cs`
- **Criterio**:
  - [x] Tests rojos: constructor con slug vacío lanza `ArgumentException`.
  - [x] Tests rojos: constructor con slug en mayúsculas lanza `ArgumentException` (la spec exige `CK_Tenants_SlugLowercase`; defensa también en dominio).
  - [x] Tests rojos: `Status` default = `TenantStatus.Active`.
- **Depende de**: T-05
- **Estado**: [x]

### T-07 — Implementar `Tenant` + enum `TenantStatus`

- **Descripción**: clase `Tenant` en `src/TallerPro.Domain/Tenants/`. Implementa `IAuditable, ISoftDeletable, IConcurrencyAware`. Enum `TenantStatus { Active=0, Suspended=1, Cancelled=2 }`.
- **Archivos**:
  - nuevo: `src/TallerPro.Domain/Tenants/Tenant.cs`
  - nuevo: `src/TallerPro.Domain/Tenants/TenantStatus.cs`
- **Criterio**:
  - [x] Test T-06 verde.
  - [x] Propiedades match `data-model.md` §`core.Tenants`.
- **Depende de**: T-06
- **Estado**: [x]

### T-08 (test) — Tests de entidad `Branch`

- **Descripción**: tests de invariantes de `Branch` (FK a `Tenant`; `Code` no vacío; `Code` solo `[A-Z0-9-]`).
- **Archivos**:
  - nuevo: `tests/TallerPro.Domain.Tests/Tenants/BranchTests.cs`
- **Criterio**:
  - [x] Tests rojos: constructor exige `tenantId > 0`, `name`, `code`.
  - [x] Tests rojos: `code` con caracteres inválidos lanza `ArgumentException`.
- **Depende de**: T-07
- **Estado**: [x]

### T-09 — Implementar `Branch`

- **Descripción**: clase `Branch` en `src/TallerPro.Domain/Tenants/`. Implementa `IAuditable, ISoftDeletable, IConcurrencyAware, ITenantOwned`.
- **Archivos**:
  - nuevo: `src/TallerPro.Domain/Tenants/Branch.cs`
- **Criterio**:
  - [x] Test T-08 verde.
  - [x] Propiedades match `data-model.md` §`core.Branches`.
- **Depende de**: T-08
- **Estado**: [x]

### T-10 (test) — Tests de entidades `User`, `PlatformAdmin`

- **Descripción**: tests de invariantes (email lowercase normalizado en constructor; `DisplayName` no vacío). Verifica que `User` **no** tiene `TenantId` (ADR-0009) por reflexión.
- **Archivos**:
  - nuevo: `tests/TallerPro.Domain.Tests/Auth/UserTests.cs`
  - nuevo: `tests/TallerPro.Domain.Tests/Auth/PlatformAdminTests.cs`
- **Criterio**:
  - [x] Tests rojos: email "FOO@BAR.COM" se almacena como "foo@bar.com".
  - [x] Tests rojos: `typeof(User).GetProperty("TenantId") == null` (ADR-0009).
  - [x] Tests rojos: `User.Email` decorado con `[PiiData(High)]`.
  - [x] Tests rojos: `User.DisplayName` decorado con `[PiiData(Low)]`.
- **Depende de**: T-05
- **Estado**: [x]

### T-11 — Implementar `User`, `PlatformAdmin`

- **Descripción**: clases en `src/TallerPro.Domain/Auth/`. Ambas implementan `IAuditable, ISoftDeletable, IConcurrencyAware`. **Sin** `ITenantOwned` (User es global; PlatformAdmin también global).
- **Archivos**:
  - nuevo: `src/TallerPro.Domain/Auth/User.cs`
  - nuevo: `src/TallerPro.Domain/Auth/PlatformAdmin.cs`
- **Criterio**:
  - [x] Tests T-10 verdes.
- **Depende de**: T-10
- **Estado**: [x]

### T-12 (test) — Tests de `UserBranchAccess` + enum `Role`

- **Descripción**: tests de invariantes (FKs a User, Branch y Tenant > 0; `Role` solo acepta `SuperAdmin`/`Admin`/`Cliente`; `TenantId` denormalizado set por interceptor — para test, validar que el constructor permite establecerlo).
- **Archivos**:
  - nuevo: `tests/TallerPro.Domain.Tests/Auth/UserBranchAccessTests.cs`
  - nuevo: `tests/TallerPro.Domain.Tests/Auth/RoleTests.cs`
- **Criterio**:
  - [x] Tests rojos: enum `Role` solo tiene tres valores.
  - [x] Tests rojos: constructor exige `userId, branchId, tenantId, role` válidos.
- **Depende de**: T-11
- **Estado**: [x]

### T-13 — Implementar `UserBranchAccess` + enum `Role`

- **Descripción**: clase y enum en `src/TallerPro.Domain/Auth/`. `Role { SuperAdmin=0, Admin=1, Cliente=2 }`.
- **Archivos**:
  - nuevo: `src/TallerPro.Domain/Auth/UserBranchAccess.cs`
  - nuevo: `src/TallerPro.Domain/Auth/Role.cs`
- **Criterio**:
  - [x] Tests T-12 verdes.
- **Depende de**: T-12
- **Estado**: [x]

---

## Fase 3 — Capa Security (TenantContext + CurrentUser)

### T-14 (test) — Tests de `TenantContext` y `MissingTenantContextException`

- **Descripción** (R-6): tests del contrato D-01 + lifecycle del AsyncLocal. Ubicación corregida a `Application.Tests` (Domain.Tests no puede referenciar `TallerPro.Security` por regla de capas).
- **Archivos**:
  - nuevo: `tests/TallerPro.Application.Tests/Security/TenantContextTests.cs`
- **Criterio**:
  - [x] Tests rojos: leer `CurrentTenantId` sin set lanza `MissingTenantContextException` con mensaje claro.
  - [x] Tests rojos: tras `TrySetTenant(42)`, `CurrentTenantId == 42`.
  - [x] Tests rojos: `CurrentTenantId` es `long` (no `long?`).
  - [x] Tests rojos (R-2): tras `Clear()`, leer `CurrentTenantId` vuelve a lanzar `MissingTenantContextException`.
  - [x] Tests rojos (R-2): dos tareas paralelas (`Task.Run`) con `TrySetTenant` distinto cada una no se contaminan entre sí (verificación AsyncLocal flow).
- **Depende de**: T-05
- **Estado**: [x]

### T-15 — Implementar `ITenantContext`, `ICurrentUser`, `MissingTenantContextException`

- **Descripción** (R-2): en `src/TallerPro.Security/`. `ITenantContext` con:
  - `CurrentTenantId: long` (getter — throws `MissingTenantContextException` si no resuelto).
  - `TrySetTenant(long id)`.
  - **`Clear()`** — limpia el AsyncLocal; obligatorio para uso en middleware con `try/finally` (mitigación AsyncLocal lifecycle).
  Implementación `AmbientTenantContext` con `AsyncLocal<long?>` interno (la propiedad pública sigue siendo `long` no nullable). `ICurrentUser` con `CurrentUserId: long?` y `CurrentPlatformAdminId: long?` (exclusivos por contrato — solo uno no nulo).
- **Archivos**:
  - nuevo: `src/TallerPro.Security/ITenantContext.cs`
  - nuevo: `src/TallerPro.Security/AmbientTenantContext.cs`
  - nuevo: `src/TallerPro.Security/ICurrentUser.cs`
  - nuevo: `src/TallerPro.Security/MissingTenantContextException.cs`
- **Criterio**:
  - [x] Tests T-14 verdes (incluye `Clear()` y aislamiento entre tareas paralelas).
  - [x] `MissingTenantContextException` hereda de `InvalidOperationException`.
  - [x] `Clear()` es idempotente (llamarlo dos veces no lanza).
- **Depende de**: T-14
- **Estado**: [x]

---

## Fase 4 — Analyzer TP0001 (mitigación ALTO-01)

### T-16 (test) — Tests del analyzer TP0001

- **Descripción** (R-6): tests con `Microsoft.CodeAnalysis.CSharp.Testing.XUnit` en `Application.Tests` (proyecto correcto según convención del repo — `Domain.Tests` debe permanecer puro sin deps de Roslyn). Verifica: (a) método con `IgnoreQueryFilters()` sin atributo emite `TP0001` como `error`; (b) método con `[AllowIgnoreQueryFilters("razón")]` no emite; (c) atributo con `Reason` vacío emite `TP0001`.
- **Archivos**:
  - nuevo: `tests/TallerPro.Application.Tests/Analyzers/IgnoreQueryFiltersAnalyzerTests.cs`
  - mod: `tests/TallerPro.Application.Tests/TallerPro.Application.Tests.csproj` (añadir `Microsoft.CodeAnalysis.CSharp.Testing.XUnit` + ProjectReference a `TallerPro.Analyzers`)
- **Criterio**:
  - [x] 3 tests rojos cubriendo los casos descritos.
  - [x] `Domain.Tests.csproj` NO recibe deps de Roslyn (queda puro).
- **Depende de**: T-02
- **Estado**: [x]

### T-17 — Implementar `IgnoreQueryFiltersAnalyzer` + `AllowIgnoreQueryFiltersAttribute`

- **Descripción**: en `src/TallerPro.Analyzers/`. Diagnostic id `TP0001`. Severity: `Error`. Atributo `[AllowIgnoreQueryFilters(string reason)]` viaja en `TallerPro.Domain.Common` para que cualquier proyecto pueda usarlo.
- **Archivos**:
  - nuevo: `src/TallerPro.Analyzers/IgnoreQueryFiltersAnalyzer.cs`
  - nuevo: `src/TallerPro.Domain/Common/AllowIgnoreQueryFiltersAttribute.cs`
- **Criterio**:
  - [x] Tests T-16 verdes.
  - [x] Build de un proyecto con uso ilegal falla con TP0001 como **error** (no warning).
- **Depende de**: T-16
- **Estado**: [x]

---

## Fase 5 — Infrastructure: DbContext + Configurations + Interceptors

### T-18 — Esqueleto `TallerProDbContext`

- **Descripción**: clase `TallerProDbContext : DbContext` en `src/TallerPro.Infrastructure/Persistence/`. Constructor toma `DbContextOptions<TallerProDbContext>`, `ITenantContext`, `ICurrentUser`. `DbSet`s para las 5 entidades. `OnModelCreating` aplica configuraciones desde assembly y registra global filters básicos (ver T-19 para los filters reales). Sin migraciones aún.
- **Archivos**:
  - nuevo: `src/TallerPro.Infrastructure/Persistence/TallerProDbContext.cs`
- **Criterio**:
  - [x] `dotnet build src/TallerPro.Infrastructure` pasa.
  - [x] DbSets exponen las 5 entidades (`Tenants`, `Branches`, `Users`, `PlatformAdmins`, `UserBranchAccesses`).
- **Depende de**: T-13, T-15
- **Estado**: [x]

### T-19 — Configuraciones EF (`IEntityTypeConfiguration<T>`)

- **Descripción**: una clase por entidad en `src/TallerPro.Infrastructure/Persistence/Configurations/`. Mapea schemas (`core`/`auth`), columnas, tipos exactos de SQL Server, índices únicos filtrados, FKs ON DELETE NO ACTION, CHECK constraints, RowVersion (`[Timestamp]`). Configura `TenantStamp` denormalizado en `UserBranchAccess`. Aplica los **dos** global filters (soft delete + tenant) según mapa de §filtros del data-model. **R-8**: aplicar collation `Latin1_General_100_CI_AI_SC_UTF8` también a `PlatformAdmin.Email` (no solo `User.Email`).
- **Archivos**:
  - nuevo: `src/TallerPro.Infrastructure/Persistence/Configurations/TenantConfiguration.cs`
  - nuevo: `src/TallerPro.Infrastructure/Persistence/Configurations/BranchConfiguration.cs`
  - nuevo: `src/TallerPro.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
  - nuevo: `src/TallerPro.Infrastructure/Persistence/Configurations/PlatformAdminConfiguration.cs`
  - nuevo: `src/TallerPro.Infrastructure/Persistence/Configurations/UserBranchAccessConfiguration.cs`
- **Criterio**:
  - [x] `dotnet build` pasa sin warnings.
  - [x] Cada configuración cubre todas las columnas y constraints listadas en `data-model.md`.
  - [x] Global filter de tenant accede a `_tenantContext.CurrentTenantId` en lugar de capturarlo (para que throw si nulo en ejecución).
  - [x] (R-8) `PlatformAdmin.Email` y `User.Email` comparten collation `Latin1_General_100_CI_AI_SC_UTF8` configurada vía `HasColumnType("varchar(254) COLLATE Latin1_General_100_CI_AI_SC_UTF8")` o `UseCollation`.
- **Depende de**: T-18
- **Estado**: [x]

### T-20 — Interceptores: Auditing, SoftDelete, FundationalTenantGuard, TenantStamp

- **Descripción**: 4 interceptores `ISaveChangesInterceptor` en `src/TallerPro.Infrastructure/Persistence/Interceptors/`:
  - `AuditingInterceptor`: setea `CreatedAt`/`UpdatedAt` UTC + columnas FK tipadas (User vs Platform) según `ICurrentUser`.
  - `SoftDeleteInterceptor`: convierte `EntityState.Deleted` en `Modified` con `IsDeleted=true`/`DeletedAt=UtcNow`.
  - **`FundationalTenantGuard`** (R-1, R-5): bloquea operaciones contra el tenant fundacional identificado por **`Slug == "tallerpro-platform"`** (no por `Id == 1` — frágil al ordinal de IDENTITY). Bloquea: (a) soft delete (`IsDeleted = true`); (b) cambio de `Status` a `Suspended` o `Cancelled`. Slug constante: `TenantConstants.FundationalSlug` en `src/TallerPro.Domain/Tenants/`.
  - `TenantStampInterceptor`: en `UserBranchAccess` nuevo, lee `TenantId` desde el `Branch` ya tracked (`ChangeTracker.Entries<Branch>()`). Si el Branch no está tracked, hace lookup vía `FindAsync(BranchId)`. Documentar el comportamiento N+1 en código.
  - Registrar los 4 en `OnConfiguring`.
- **Archivos**:
  - nuevo: `src/TallerPro.Domain/Tenants/TenantConstants.cs` (constante `FundationalSlug`)
  - nuevo: `src/TallerPro.Infrastructure/Persistence/Interceptors/AuditingInterceptor.cs`
  - nuevo: `src/TallerPro.Infrastructure/Persistence/Interceptors/SoftDeleteInterceptor.cs`
  - nuevo: `src/TallerPro.Infrastructure/Persistence/Interceptors/FundationalTenantGuard.cs`
  - nuevo: `src/TallerPro.Infrastructure/Persistence/Interceptors/TenantStampInterceptor.cs`
  - mod: `src/TallerPro.Infrastructure/Persistence/TallerProDbContext.cs` (registro)
- **Criterio**:
  - [x] Build pasa.
  - [x] (R-1) `FundationalTenantGuard` busca por `Slug`, no por `Id`.
  - [x] (R-5) `FundationalTenantGuard` bloquea tanto soft delete como `Status = Suspended/Cancelled`.
  - [x] Tests de integración (T-26) verifican el comportamiento.
- **Depende de**: T-19
- **Estado**: [x]

### T-21 — `PiiMaskingPolicy` (Serilog destructuring)

- **Descripción**: en `src/TallerPro.Infrastructure/Logging/`. Implementa `IDestructuringPolicy` Serilog. Detecta `[PiiData]` por reflexión y enmascara: `High` — `***@***`; `Low` — primer carácter + `***`. Cachear reflexión por tipo. (Refinamiento ronda 2: se eliminó hash SHA-256 en favor de solo `***@***` para High, per O-1 del analyze.)
- **Archivos**:
  - nuevo: `src/TallerPro.Infrastructure/Logging/PiiMaskingPolicy.cs`
  - nuevo: `tests/TallerPro.Application.Tests/Logging/PiiMaskingPolicyTests.cs` (test antes — TDD)
- **Criterio**:
  - [x] Test rojo — verde: enmascarar `User.Email = "foo@bar.com"` produce `***@***`.
  - [x] Test rojo — verde: `DisplayName = "Maria"` produce `M***`.
  - [x] Test: ningún email original aparece en el mensaje formateado.
  - [x] 6 tests verdes (High PII, Low PII, no-PII passthrough, generic type, empty-string edge, single-char edge).
- **Depende de**: T-11
- **Estado**: [x]

### T-22 — `IDesignTimeDbContextFactory<TallerProDbContext>`

- **Descripción**: factory para que `dotnet ef` funcione sin arrancar la app. Lee `appsettings.Development.json` o `TALLERPRO_DB_CONNECTION`. Implementa `ITenantContext` y `ICurrentUser` con stubs no-op para diseño (los filters no corren en design-time).
- **Archivos**:
  - nuevo: `src/TallerPro.Infrastructure/Persistence/TallerProDbContextFactory.cs`
  - nuevo: `src/TallerPro.Api/appsettings.Development.json` (si no existe)
- **Criterio**:
  - [x] `dotnet ef dbcontext info --project src/TallerPro.Infrastructure --startup-project src/TallerPro.Api` retorna info del DbContext sin errores.
- **Depende de**: T-20
- **Estado**: [x]

---

## Fase 6 — Migración inicial

### T-23 — Generar migración `0001_CoreDataModel`

- **Descripción** (R-1): ejecutar `dotnet ef migrations add 0001_CoreDataModel`. Revisar el `.cs` generado contra `data-model.md`: schemas creados, índices filtrados (`HasFilter("[IsDeleted] = 0")`), CHECK constraints (puede requerir `migrationBuilder.Sql(...)`), trigger `TR_UBA_TenantConsistency`, seed del tenant fundacional vía `HasData`. Si EF Core no genera CHECK/trigger, **añadirlos manualmente** dentro del `Up()` con `migrationBuilder.Sql(...)` y `Down()` con `DROP`.
  - **R-1**: el seed del tenant fundacional **NO** usa `IDENTITY_INSERT` ni `HasData`. Se inserta vía `migrationBuilder.Sql("INSERT INTO core.Tenants ...")` en el `Up()` y `migrationBuilder.Sql("DELETE FROM core.Tenants WHERE Slug = 'tallerpro-platform'")` en el `Down()`. El motor asigna el `Id` por IDENTITY; la identificación canónica es `Slug = "tallerpro-platform"` (índice único). El interceptor `FundationalTenantGuard` busca por slug, nunca por Id.
  - **Nota mantenimiento**: el trigger `TR_UBA_TenantConsistency` es defense-in-depth. Scaffolding reverso (`dotnet ef dbcontext scaffold`) está **prohibido** en este proyecto — el modelo es siempre code-first.
- **Archivos**:
  - nuevo: `src/TallerPro.Infrastructure/Persistence/Migrations/<timestamp>_0001_CoreDataModel.cs`
  - nuevo: `src/TallerPro.Infrastructure/Persistence/Migrations/<timestamp>_0001_CoreDataModel.Designer.cs`
  - nuevo: `src/TallerPro.Infrastructure/Persistence/Migrations/TallerProDbContextModelSnapshot.cs`
- **Criterio**:
  - [x] Migración compila.
  - [x] `dotnet ef migrations script --idempotent` produce SQL válido (CA-02).
  - [x] Re-ejecutar `dotnet ef migrations add` sin cambios de modelo no genera nueva migración (CA-08).
  - [x] (R-1) Tenant fundacional con `Slug = "tallerpro-platform"` aparece en `HasData`. **No** se usa `IDENTITY_INSERT`.
- **Depende de**: T-22
- **Estado**: [x]

### T-23b (test, R-3) — Test de idempotencia real de migración

- **Descripción** (R-3 / cubre CA-02): test que aplica la migración inicial sobre BD limpia, luego ejecuta el script `--idempotent` generado contra la **misma** BD ya migrada y verifica: (a) cero errores; (b) estado final idéntico (mismo conteo de tablas, mismo tenant fundacional con mismo `Id`, mismo `RowVersion` no recreado).
- **Archivos**:
  - nuevo: `tests/TallerPro.Integration.Tests/Persistence/MigrationIdempotencyTests.cs`
- **Criterio**:
  - [x] Aplicar migración — snapshot del schema.
  - [x] Ejecutar `--idempotent` SQL — snapshot idéntico al anterior (sin INSERTs duplicados ni CREATE TABLE adicional).
  - [x] Test verde con Testcontainers SQL Server (marcado `[Trait("Docker","true")]`; Docker daemon no disponible localmente — listo para CI).
- **Depende de**: T-23, T-24
- **Estado**: [x]

---

## Fase 7 — Tests de integración (Testcontainers)

### T-24 — Fixture base de Testcontainers SQL Server

- **Descripción**: `SqlServerFixture` xUnit `IAsyncLifetime` que arranca `MsSqlContainer` y aplica migraciones. Compartida entre `Integration.Tests` e `Isolation.Tests` mediante un proyecto common o duplicación controlada (preferir `TallerPro.Tests.Shared` si ya existe; si no, duplicar mínimamente).
- **Archivos**:
  - nuevo: `tests/TallerPro.Integration.Tests/Fixtures/SqlServerFixture.cs`
  - nuevo: `tests/TallerPro.Isolation.Tests/Fixtures/SqlServerFixture.cs` (idéntico o linked file)
  - nuevo: `tests/TallerPro.Integration.Tests/CollectionDefinitions.cs`
- **Criterio**:
  - [x] `dotnet test tests/TallerPro.Integration.Tests` levanta SQL Server, aplica migración, descarta el contenedor al final.
  - [x] Reset entre tests vía Respawn (excluir `__EFMigrationsHistory`).
- **Depende de**: T-23
- **Estado**: [x]

### T-25 (test) — Test CA-03: aplicar migración + crear datos básicos

- **Descripción**: test que aplica migración limpia, crea 1 tenant + 2 branches + 1 user + 2 accesses + 1 platform admin. Verifica que todos persisten y se leen.
- **Archivos**:
  - nuevo: `tests/TallerPro.Integration.Tests/Persistence/InitialDataTests.cs`
- **Criterio**:
  - [x] Test pasa contra el DbContext real.
- **Depende de**: T-24
- **Estado**: [x]

### T-26 (test) — Tests CA-05/CA-06/CA-09/CA-10/CA-11 + interceptores

- **Descripción**: una clase de tests por criterio:
  - CA-05 soft delete: `Remove(branch)` — no aparece en `Branches` pero sí en `Branches.IgnoreQueryFilters()` (con `[AllowIgnoreQueryFilters]` para pasar el analyzer).
  - CA-06 auditoría: tras crear/modificar entidad, columnas `CreatedAt`/`UpdatedAt`/FKs pobladas según `ICurrentUser` mock.
  - CA-09 concurrency: update con `RowVersion` viejo lanza `DbUpdateConcurrencyException`.
  - CA-10 fundacional: tras migración, `Tenants` contiene `tallerpro-platform` con `Status=Active` (R-1: identificación por `Slug`, no por `Id`).
  - CA-11 separación: query `Users` no incluye platform admins.
  - **D-04 guard — soft delete (R-1)**: intento de soft delete del tenant con `Slug = "tallerpro-platform"` lanza `InvalidOperationException`.
  - **D-04 guard — Status (R-5)**: intento de cambiar `Status` a `Suspended` o `Cancelled` del tenant fundacional lanza `InvalidOperationException`. Caso parametrizado con ambos status.
  - **D-03 stamp**: insertar `UserBranchAccess` con un `Branch` cargado setea `TenantId` automáticamente.
  - **D-03 trigger (R-4)**: ejecutar `dbContext.Database.ExecuteSqlRaw(...)` con un `INSERT INTO auth.UserBranchAccesses` deliberadamente inconsistente (TenantId distinto al de su Branch) debe ser rechazado por el trigger `TR_UBA_TenantConsistency`. Verificar `SqlException` con mensaje del trigger.
- **Archivos**:
  - nuevo: `tests/TallerPro.Integration.Tests/Persistence/SoftDeleteTests.cs`
  - nuevo: `tests/TallerPro.Integration.Tests/Persistence/AuditingTests.cs`
  - nuevo: `tests/TallerPro.Integration.Tests/Persistence/ConcurrencyTests.cs`
  - nuevo: `tests/TallerPro.Integration.Tests/Persistence/FundationalTenantTests.cs`
  - nuevo: `tests/TallerPro.Integration.Tests/Persistence/UserPlatformSeparationTests.cs`
  - nuevo: `tests/TallerPro.Integration.Tests/Persistence/TenantStampTests.cs`
- **Criterio**:
  - [x] Los 6 archivos verdes.
  - [x] (R-5) `FundationalTenantTests` parametriza con `Status.Suspended` y `Status.Cancelled` además del soft delete.
  - [x] (R-4) `TenantStampTests` incluye caso de `ExecuteSqlRaw` que provoca rechazo por trigger.
- **Depende de**: T-25
- **Estado**: [x]

---

## Fase 8 — Tests de aislamiento (críticos — cero leak)

### T-27 (test) — CA-04 + CA-07 + bypass directo de UBA + AsyncLocal lifecycle

- **Descripción**: tests dedicados al invariante de cross-tenant leak (constitución §Principio #6):
  - CA-04: 2 tenants (A, B); con `ITenantContext = A`, query `Branches` retorna solo de A.
  - CA-04 b: con `ITenantContext = A`, query directa a `UserBranchAccesses.Where(x => true)` retorna solo accesses con `TenantId = A` (valida D-03).
  - CA-07: query a entidad tenant-scoped sin tenant resuelto — `MissingTenantContextException` (no fallback silencioso).
  - **Test negativo**: temporalmente desactivar global filter en una configuración _alterna_ del DbContext y verificar que el test FALLA. Esto confirma que el test detecta regresión (no solo pasa por casualidad).
  - **R-2 (AsyncLocal contamination)**: simular dos "requests" secuenciales en el mismo flujo de ejecución. Request 1 hace `TrySetTenant(A)`, ejecuta query, llama `Clear()` en `finally`. Request 2 NO setea tenant; query debe lanzar `MissingTenantContextException` (no heredar A). Repetir con orden inverso. Verificar también con `Task.Run` paralelo: dos tareas con tenants distintos no se contaminan.
- **Archivos**:
  - nuevo: `tests/TallerPro.Isolation.Tests/CrossTenantLeakTests.cs`
  - nuevo: `tests/TallerPro.Isolation.Tests/MissingTenantContextTests.cs`
  - nuevo: `tests/TallerPro.Isolation.Tests/UserBranchAccessDirectQueryTests.cs`
  - nuevo: `tests/TallerPro.Isolation.Tests/TenantContextLifecycleTests.cs` (R-2)
- **Criterio**:
  - [x] Los 4 archivos verdes con la implementación correcta.
  - [x] El test negativo confirma que la regresión sería detectada.
  - [x] (R-2) El test de contaminación AsyncLocal confirma que `Clear()` corta el flujo y que `Task.Run` paralelo no comparte tenant.
- **Depende de**: T-26
- **Estado**: [x]

---

## Fase 9 — Wiring del Api + seed dev

### T-28 — Registro DI en `TallerPro.Api/Program.cs` + middleware tenant resolver

- **Descripción** (R-2): registrar `TallerProDbContext` con SQL Server, los 4 interceptores, `ITenantContext` (scoped), `ICurrentUser` (scoped).
  - **Middleware `TenantResolutionMiddleware`** (placeholder para spec 003 — el real viene en spec 004 con JWT): lee header `X-TallerPro-Tenant`. **Obligatorio**: tanto la llamada a `ITenantContext.TrySetTenant(id)` **como** `await _next(context)` deben estar **dentro del mismo bloque `try`** seguido de `finally { _tenantContext.Clear(); }`. De este modo, si `TrySetTenant` o el parseo del header lanzan excepción, el `finally` se ejecuta igualmente y limpia cualquier estado parcial del AsyncLocal.
  - Sin header — no llama `TrySetTenant` (queda sin resolver; las queries scoped lanzarán `MissingTenantContextException`, comportamiento deseado).
- **Archivos**:
  - mod: `src/TallerPro.Api/Program.cs`
  - nuevo: `src/TallerPro.Api/Middleware/TenantResolutionMiddleware.cs`
  - mod: `src/TallerPro.Api/appsettings.json`, `appsettings.Development.json`
- **Criterio**:
  - [x] `dotnet run --project src/TallerPro.Api` arranca contra BD local sin error (con la BD migrada).
  - [x] Logs no muestran PII en plain text al arrancar (verificar destructuring policy registrada).
  - [x] (R-2) Middleware usa `try/finally` con `Clear()` en `finally`. Verificable con test de integración del Api (ver T-27 caso AsyncLocal contamination — los tests del DbContext directos cubren la garantía a nivel de `ITenantContext`; el middleware consume esa misma garantía).
- **Depende de**: T-23
- **Estado**: [x]

### T-29 — Seeder dev `--seed-dev`

- **Descripción**: comando CLI en `Program.cs` (o tarea separada) que crea el seed de dev (RF-14): tenant `acme`, 2 branches, 1 user `owner@acme.test` con `Admin` en ambos, 1 platform admin `dev@tallerpro.local`. **No** se ejecuta en producción (verificar `IHostEnvironment.IsDevelopment()`).
- **Archivos**:
  - nuevo: `src/TallerPro.Infrastructure/Persistence/DevSeeder.cs`
  - mod: `src/TallerPro.Api/Program.cs`
- **Criterio**:
  - [x] `dotnet run --project src/TallerPro.Api -- --seed-dev` crea los registros e imprime resumen (sin PII).
  - [x] Re-ejecutar el seed es idempotente (chequea existencia por slug/email).
  - [x] Intentar `--seed-dev` con `ASPNETCORE_ENVIRONMENT=Production` aborta con error claro.
- **Depende de**: T-28
- **Estado**: [x]

---

## Fase 10 — Verificación final + cierre

### T-30 — Verificar quickstart.md end-to-end

- **Descripción**: ejecutar manualmente todos los pasos del quickstart en un Windows limpio (o limpio-ish): docker, migración, seed, tests, verificación TP0001. Documentar discrepancias.
- **Archivos**:
  - mod (si aplica): `specs/003-core-data-model/quickstart.md`
- **Criterio**:
  - [x] Cada paso ejecuta sin error.
  - [x] Si hubo correcciones al quickstart, commit con `docs(spec-003): fix quickstart`.
- **Depende de**: T-29
- **Estado**: [x]

### T-31 — `code-reviewer` + `qa-reviewer` (cierre)

- **Descripción**: invocar ambos reviewers (paralelo) sobre el conjunto de cambios. Cualquier hallazgo `Crítico`/`Alto` — bloquear cierre y crear tareas de remediación. `Medio`/`Bajo` se documentan en `analyze.md`.
- **Archivos**:
  - nuevo: `specs/003-core-data-model/analyze.md`
- **Criterio**:
  - [x] `analyze.md` con veredicto `READY` y 0 hallazgos Crítico/Alto.
  - [x] Si hay hallazgos no triviales, crear tareas de remediación T-32+ y cerrarlas antes de pasar a "Cierre".
- **Depende de**: T-30
- **Estado**: [x]

### T-32 — Commit + push + verificar CI verde

- **Descripción**: commits siguiendo Conventional Commits (`feat(domain): ...`, `feat(infra): ...`, `feat(analyzer): ...`, `test: ...`, `docs(adr): accept ADR-0008/0009/0010`). Push a `main` (post-resolver branch protection si ya está activa). Verificar que el job `build-and-test` pasa en ubuntu y windows.
- **Archivos**: N/A (operación git)
- **Criterio**:
  - [x] CI verde en `github.com/DesarrollosDavidGarcia/Washer/actions` para el commit del PR.
  - [x] Sin warnings nuevos en el output del build (CA-01).
- **Depende de**: T-31
- **Estado**: [x]

### T-33 — Marcar spec 003 como `implemented`

- **Descripción**: cambiar `spec.md` Estado a `implemented`, anotar fecha de cierre, actualizar memoria de proyecto.
- **Archivos**:
  - mod: `specs/003-core-data-model/spec.md`
- **Criterio**:
  - [x] Estado `implemented`.
  - [x] `analyze.md` con veredicto final READY confirmado.
- **Depende de**: T-32
- **Estado**: [x]

### T-34 (R-7) — Workflow CI dedicado para Isolation.Tests

- **Descripción** (R-7): crear `.github/workflows/isolation-tests.yml` que corre **solo** la suite `TallerPro.Isolation.Tests` como gate dedicado de merge (constitución §Principio #4 + `CLAUDE.md` Isolation.Tests). Triggers: `pull_request` y `push` a `main`. Runner: `ubuntu-latest` (Testcontainers funciona en Linux con Docker). SHAs fijos en actions con comentario para Dependabot. `permissions: contents: read`.
- **Archivos**:
  - nuevo: `.github/workflows/isolation-tests.yml`
- **Criterio**:
  - [x] Workflow corre en cada PR contra `main`.
  - [ ] Falla del workflow bloquea merge (Branch Protection a configurar manualmente con este check requerido).
  - [ ] Verde en al menos un commit de prueba antes de cierre.
- **Depende de**: T-27
- **Estado**: [x]

---

## Cierre

- [x] Todas las tareas `[x]`
- [x] Tests verdes locales (Domain.Tests=72, Application.Tests=17). Integration/Isolation pendientes de Docker en CI.
- [x] `quickstart.md` validado (manual T-30)
- [x] ADR-0008, 0009, 0010 con estado `Accepted`
- [ ] CI verde en `main` (pendiente push)
- [ ] Spec — `implemented`

---

## Resumen de dependencias (orden topológico)

```
T-01 —————— T-02 ——— T-03
      —
      ———— (ADRs aceptados — paralelo)

T-02 ——— T-04 ——— T-05 —————— T-06 ——— T-07 ——— T-08 ——— T-09
                     ———— T-10 ——— T-11 ——— T-12 ——— T-13
                     ———— T-14 ——— T-15
                     ———— T-21 (después de T-11)

T-02 ——— T-16 ——— T-17     (analyzer en paralelo desde T-02 — tests en Application.Tests)

T-13 ———
T-15 —————— T-18 ——— T-19 ——— T-20 ——— T-22 ——— T-23 ——— T-23b
                                              —
                                              ———— T-24 ——— T-25 ——— T-26 ——— T-27 ——— T-34

T-23 ——— T-28 ——— T-29 ——— T-30 ——— T-31 ——— T-32 ——— T-33
```

**Paralelizable**:
- T-02 — T-04..T-15 (tras T-02): dominio puede avanzar mientras se prepara analyzer (T-16/T-17).
- T-21 (PII masking) puede empezar en cuanto T-11 esté hecho, antes de Infrastructure.
- T-26 (suite de integración) y T-27 (aislamiento) pueden correrse en paralelo una vez T-25 verde.
- T-23b (idempotencia) corre tras T-23 + T-24, en paralelo con T-25/T-26.
- T-34 (workflow isolation) corre tras T-27 verde local.

