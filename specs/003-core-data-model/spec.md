# Spec: Modelo de datos base multi-tenant (EF Core)

- **ID**: 003
- **Estado**: implemented
- **Fecha**: 2026-05-04
- **Implementada**: 2026-05-04

## Resumen

Establece el núcleo de persistencia de TallerPro: las cuatro entidades raíz (`Tenant`, `Branch`, `User`, `UserBranchAccess`), el `DbContext` central en `TallerPro.Infrastructure` con global query filters para soft delete y tenant scoping, los campos de auditoría base, y las migraciones iniciales. No incluye endpoints, autenticación ni lógica de impersonation (specs posteriores).

## Problema

- **Dolor**: sin un modelo de datos común, ninguna feature posterior (auth, órdenes, inventario, billing) puede arrancar; todas dependen de poder identificar tenant + branch + usuario y de un `DbContext` con aislamiento garantizado.
- **Afectados**: todos los flujos del producto. Specs 004+ están bloqueadas.
- **Coste de no hacerlo**: imposible empezar el MVP. Riesgo de cross-tenant leak si cada feature improvisa su propio scoping.

## Casos de uso

- **Actor**: desarrollador trabajando en una feature posterior (ej. crear orden de servicio).
- **Escenarios**:
  1. Dado un tenant `A` con dos sucursales `A1` y `A2`, cuando un usuario consulta entidades del tenant `A` desde un contexto sin `IgnoreQueryFilters`, entonces solo ve datos del tenant `A` (jamás del tenant `B`).
  2. Dado un usuario con acceso a `A1` y `A2`, cuando se elimina (soft delete) su acceso a `A2`, entonces los queries de listado dejan de retornarlo para `A2` pero el registro persiste para auditoría.
  3. Dado un super-admin de plataforma sin scope tenant, cuando consulta entidades del catálogo central, entonces puede ver todos los tenants (vía `IgnoreQueryFilters` controlado).
  4. Dado un cambio de modelo, cuando un dev ejecuta `dotnet ef migrations add`, entonces se genera una migración determinista revisable en el PR.

## Requisitos funcionales

### Entidades

- [ ] **RF-01**: Definir `Tenant` (schema `core`) con: `Id` (`bigint IDENTITY`), `Name`, `Slug` único (lowercase, regex `[a-z0-9-]+`, slugs reservados), `Status` (enum: Active/Suspended/Cancelled), `RowVersion`, campos de auditoría base.
- [ ] **RF-02**: Definir `Branch` (schema `core`) con: `Id` (`bigint IDENTITY`), `TenantId` (FK), `Name`, `Code` (alfanumérico, único por tenant), `RowVersion`, campos de auditoría base. Relación `Tenant 1 — N Branch`.
- [ ] **RF-03**: Definir `User` (schema `auth`) con: `Id` (`bigint IDENTITY`), `Email` único **global** (lowercase normalizado al guardar, collation case-insensitive), `DisplayName`, `RowVersion`, campos de auditoría. `User` es **global** (sin `TenantId` fijo). Hash de password, MFA → spec 004.
- [ ] **RF-04**: Definir `UserBranchAccess` (schema `auth`) con: `Id` (`bigint IDENTITY`), `UserId` (FK), `BranchId` (FK), `Role` (enum cerrado: `SuperAdmin`, `Admin`, `Cliente`), `RowVersion`, campos de auditoría. Llave única compuesta `(UserId, BranchId)` cuando no soft-deleted. Roles más finos dentro del tenant los gestiona cada cliente (fuera de alcance).
- [ ] **RF-04b**: Definir `PlatformAdmin` (schema `auth`) como **entidad separada** de `User` con: `Id` (`bigint IDENTITY`), `Email` único, `DisplayName`, `RowVersion`, campos de auditoría. Sin FK a `User`. Autoriza acceso al portal `admin.tallerpro.mx`.

### Auditoría y soft delete

- [ ] **RF-05**: Toda entidad de esta spec implementa `IAuditable` con `CreatedAt`, `UpdatedAt`, `CreatedBy` (UserId/PlatformAdminId nullable), `UpdatedBy` (idem). Timestamps **siempre en UTC**, columna `datetime2(7)`. Poblado automáticamente en `SaveChangesAsync` vía interceptor.
- [ ] **RF-06**: Toda entidad implementa `ISoftDeletable` con `IsDeleted` (bool default false) y `DeletedAt` (datetime2 nullable, UTC). `Remove(entity)` marca `IsDeleted=true` + `DeletedAt=UtcNow` en lugar de borrar físicamente. Hard delete prohibido para estas entidades.
- [ ] **RF-06b**: Toda entidad implementa `IConcurrencyAware` con `RowVersion` (`rowversion` SQL Server, `byte[]` + `[Timestamp]` EF). Updates con `RowVersion` desactualizado deben lanzar `DbUpdateConcurrencyException`.

### DbContext y global filters

- [ ] **RF-07**: `TallerProDbContext` reside en `TallerPro.Infrastructure/Persistence/`. Acepta `ITenantContext` (de `TallerPro.Security`) por DI. Convención de nombres: **PascalCase** para tablas y columnas (default EF). Schemas múltiples: `core`, `auth` (esta spec); más en specs futuras.
- [ ] **RF-08**: Aplicar global query filter de **soft delete** a todas las entidades `ISoftDeletable`: `e => !e.IsDeleted`.
- [ ] **RF-09**: Aplicar global query filter de **tenant scoping** a entidades con `TenantId` (Branch, y futuras tenant-scoped). `Tenant`, `User`, `UserBranchAccess` y `PlatformAdmin` no son tenant-scoped (User es global; UserBranchAccess se filtra implícitamente por la FK a Branch que sí está scoped).
- [ ] **RF-10**: Configuración EF vía `IEntityTypeConfiguration<T>` separados por entidad (Fluent API). Prohibido data annotations para mapeo (excepto `[Timestamp]` para RowVersion donde aplique). Validaciones de aplicación vía **FluentValidation** (constitución §Restricciones #5).

### Migraciones e infraestructura

- [ ] **RF-11**: Generar migración inicial `0001_CoreDataModel` que cree los schemas `core` y `auth`, las cinco tablas (`Tenants`, `Branches`, `Users`, `UserBranchAccesses`, `PlatformAdmins`) con índices, FKs, RowVersion y la **semilla de tenant fundacional** (slug y nombre a definir en plan).
- [ ] **RF-12**: Soporte para SQL Server como provider primario. Connection string vía `appsettings.json` + override por env var `TALLERPRO_DB_CONNECTION`.
- [ ] **RF-13**: `IDesignTimeDbContextFactory<TallerProDbContext>` para que `dotnet ef` funcione sin necesidad de levantar la app.
- [ ] **RF-14**: Seed mínimo determinista (solo en dev/test, **no** en producción): 1 tenant `acme`, 2 branches, 1 user con acceso `Admin` a ambos branches, 1 platform admin de prueba. Distinto del tenant fundacional de RF-11 (este último sí va en producción desde día 1).

## Requisitos no funcionales

- **Rendimiento**:
  - Queries con global filter no deben degradar más de 5% vs query sin filter (validable en `TallerPro.Integration.Tests`).
  - Índices en `(TenantId)` de `Branch` y `(UserId, BranchId)` de `UserBranchAccess`.
- **Seguridad**:
  - **Cross-tenant leak: CERO** (constitución §6). Cada nueva entidad tenant-scoped debe agregar test en `TallerPro.Isolation.Tests`.
  - El `ITenantContext` no puede ser nulo cuando se accede a entidades tenant-scoped: debe lanzar `MissingTenantContextException` claramente.
  - Roslyn analyzers TP0001-TP0004 deben detectar uso de `IgnoreQueryFilters` no autorizado (lógica real es spec 005, pero la spec actual debe respetar la convención).
- **Observabilidad**:
  - Interceptor de auditoría logea (Serilog) cada `SaveChanges` con `EntityName`, `EntityId`, `Operation`, `TenantId`, `UserId` — sin PII.
  - Métrica `db.savechanges.duration` por entidad.
- **Accesibilidad**: N/A (sin UI).

## Criterios de aceptación

- [ ] **CA-01**: `dotnet build TallerPro.sln -c Release` compila sin warnings nuevos en local y CI.
- [ ] **CA-02**: `dotnet ef migrations script --idempotent` produce SQL aplicable contra una base limpia y contra una base ya migrada (idempotencia).
- [ ] **CA-03**: Test de integración (`TallerPro.Integration.Tests`) que arranca SQL Server vía Testcontainers, aplica migración inicial y crea 1 tenant + 2 branches + 1 user + 2 accesses; el seed pasa.
- [ ] **CA-04**: Test de aislamiento (`TallerPro.Isolation.Tests`) crea 2 tenants y verifica que un query de `Branch` con `ITenantContext = tenantA` retorna solo las sucursales de `tenantA`; nunca las de `tenantB`. Falla intencional si el global filter se desactiva.
- [ ] **CA-05**: Soft delete funciona: tras `Remove(branch)` + `SaveChanges`, el branch no aparece en queries normales pero sí con `IgnoreQueryFilters` (verificable en test).
- [ ] **CA-06**: Auditoría funciona: tras crear/modificar una entidad, `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy` quedan poblados sin código manual en el caller.
- [ ] **CA-07**: `MissingTenantContextException` se lanza cuando se consulta una entidad tenant-scoped sin tenant context resuelto.
- [ ] **CA-08**: Migración generada está commitada con nombre versionado y es revisable en diff (no auto-regenera al re-ejecutar `dotnet ef migrations add` sin cambios de modelo).
- [ ] **CA-09**: Concurrency: test que actualiza una entidad con `RowVersion` desactualizado lanza `DbUpdateConcurrencyException`.
- [ ] **CA-10**: Tenant fundacional: tras aplicar migración 0001 contra una BD limpia, existe el tenant fundacional (RF-11) con su slug y status `Active`.
- [ ] **CA-11**: `PlatformAdmin` no aparece como `User`: query `Users` no retorna platform admins; viven en su propia tabla.

## Fuera de alcance

- **Autenticación, password hashing, MFA, refresh tokens, impersonation** → spec 004.
- **Roles y permisos granulares dentro del tenant** (mecánico, recepción, gerente, etc.) → los gestiona cada cliente con su propio sub-sistema interno. Fuera del alcance de esta spec **y** de spec 004. Esta spec fija el enum `Role` cerrado a `SuperAdmin`/`Admin`/`Cliente`.
- **RBAC/policy-based authorization** del lado del API (claims, policies, requirements) → spec 004 o posterior.
- **Entidades de producto** (Vehicle, Customer, Order, InventoryItem, Part, Cfdi, Payment) → specs específicas posteriores.
- **Outbox / sync engine** del cliente offline (Hybrid + LocalDb) → spec dedicada.
- **Multi-DB / sharding por tenant** → no MVP. Single DB + tenant filter por columna.
- **Audit log inmutable** (tabla `AuditLog` separada con hashes) → spec 004 o posterior.
- **Read replicas / CQRS materialized views** → no MVP.

## Preguntas abiertas

> Todas las preguntas críticas resueltas en clarify ronda 1 (2026-05-04). Detalles finos delegados a `/speckit.plan`:
> - Regex/longitud exactas de `Slug`, `Code`, lista de slugs reservados.
> - Slug y nombre del tenant fundacional (RF-11).
> - Asignación final de `UserBranchAccess` al schema (`core` vs `auth`) — referencia cruzada con `Branch`.
> - Campos completos de `PlatformAdmin` y modelo de invitación (¿onboarding manual via SQL en producción?).
>
> Ver `clarify.md` para Q&A literal.

## Historial de clarificaciones

- **2026-05-04 — Ronda 1**: 13/13 preguntas resueltas. Decisiones clave:
  - PKs `bigint IDENTITY` (Q-01).
  - `User` global sin `TenantId` (Q-02).
  - `PlatformAdmin` como entidad separada en schema `auth` (Q-03 → RF-04b nueva).
  - Enum `Role` cerrado: `SuperAdmin`/`Admin`/`Cliente`; sub-roles intra-tenant fuera de alcance (Q-04).
  - `RowVersion` en las 4+1 entidades; nuevo RF-06b (Q-05).
  - PascalCase default EF (Q-06).
  - Schemas múltiples: `core` (Tenant, Branch) y `auth` (User, UserBranchAccess, PlatformAdmin) (Q-07).
  - Validaciones aplicadas (slug regex, code formato, email lowercase, UTC); detalle regex en plan (Q-08-Q-11).
  - Tenant fundacional en migración 0001 (Q-12 → actualizado RF-11).
  - Testcontainers SQL Server obligatorio; InMemory prohibido (Q-13).

## Referencias

- **Constitución**: `.specify/memory/constitution.md` §Identidad (jerarquía de cuentas), §Restricciones #8 (soft delete), #10 (aislamiento tenant), §Convenciones (migraciones EF).
- **Stack**: `.specify/memory/stack.md` — versiones EF Core, SQL Server, Testcontainers.
- **Spec previas**:
  - `specs/001-bootstrap-monorepo/` — sln + 18 proyectos.
  - `specs/002-scaffold-src-projects/` — `.csproj` listos en `src/`.
- **CLAUDE.md src**: convenciones de capa (`Domain ← Application ← Infrastructure`), prohibido `Console.WriteLine`, `MudBlazor 100%` (N/A esta spec).
- **ADR backlog candidatos**: ADR-0008 (Id strategy), ADR-0009 (User global vs tenant-scoped), ADR-0010 (BD naming convention) — a decidir según respuestas a Q-01/02/06.
