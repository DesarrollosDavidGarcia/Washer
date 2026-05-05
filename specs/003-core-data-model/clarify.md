# Clarify: Spec 003 — Modelo de datos base multi-tenant

- **Fecha**: 2026-05-04
- **Spec**: `specs/003-core-data-model/spec.md`
- **Estado tras clarify**: 13/13 preguntas resueltas. Ambigüedades críticas: ninguna. Detalles finos delegados a `/speckit.plan`.

## Ronda 1 — 2026-05-04

### Q-01 — Tipo de identificador
**Pregunta**: ¿`Guid v7`, `long` autoincrement, o `ULID` para PKs?
**Respuesta**: `long` autoincrement.
**Implicación**: PKs `bigint IDENTITY` en SQL Server. URLs públicas no expondrán PKs (se usará Slug/Code en su lugar). FKs son `bigint`. Sin overhead de Guid clustered.

---

### Q-02 — `User.TenantId` fijo o global
**Pregunta**: ¿`User` pertenece a un tenant, o es global y accede a múltiples vía `UserBranchAccess`?
**Respuesta**: **Global** (sin `TenantId` fijo).
**Implicación**:
- `User` no tiene columna `TenantId`.
- El acceso multi-tenant se modela 100% vía `UserBranchAccess` (Branch ya pertenece a un Tenant).
- Email único **global** (no único por tenant).
- Login resuelve los tenants accesibles consultando `UserBranchAccess` del usuario.
- Esto permite a un mismo dueño (persona física) gestionar varios talleres con el mismo email.

---

### Q-03 — Super-admin de plataforma
**Pregunta**: ¿flag en `User`, entidad separada, o claim?
**Respuesta**: **Entidad separada**.
**Implicación**:
- Nueva entidad `PlatformAdmin` (schema `auth`).
- Sin relación FK directa con `User` (un platform admin no es un User regular del producto).
- Acceso al portal `admin.tallerpro.mx` se autoriza por presencia en `PlatformAdmin`, no por flag.
- Refuerza separación de superficies (ataque a `User` no escala a super-admin).

---

### Q-04 — Enum `Role` de `UserBranchAccess`
**Pregunta**: lista provisional o completa.
**Respuesta**: enum fijo y cerrado: `SuperAdmin`, `Admin`, `Cliente`.
**Implicación**:
- 3 valores. Roles más granulares dentro del taller (mecánico, recepción, gerente, etc.) **los gestiona cada cliente (tenant)** con su propio sub-sistema de roles → fuera del alcance de esta spec y de spec 004.
- `SuperAdmin`: presente solo en accesos otorgados por plataforma (poco común).
- `Admin`: dueño/operador del tenant.
- `Cliente`: usuario regular del tenant (alcance se refina con sub-roles del propio tenant).

---

### Q-05 — Concurrency tokens
**Pregunta**: ¿agregar `RowVersion` ahora?
**Respuesta**: **Sí**, en las 4 entidades.
**Implicación**:
- Columna `RowVersion` (`rowversion` en SQL Server, `byte[]` + `[Timestamp]` en EF) en `Tenant`, `Branch`, `User`, `UserBranchAccess`.
- Tests de integración deben validar que un update con `RowVersion` desactualizado lanza `DbUpdateConcurrencyException`.

---

### Q-06 — Naming convention BD
**Pregunta**: PascalCase, snake_case, o lowercase_underscore.
**Respuesta**: **PascalCase** (default EF).
**Implicación**: tablas (`Tenants`, `Branches`, `Users`, `UserBranchAccesses`) y columnas (`Id`, `CreatedAt`, etc.) en PascalCase. Sin convention plugin de naming.

---

### Q-07 — Schemas
**Pregunta**: schema único `dbo`, o múltiples.
**Respuesta**: **Múltiples**.
**Implicación**:
- Asignación tentativa (a confirmar en plan):
  - `core` → `Tenant`, `Branch`
  - `auth` → `User`, `UserBranchAccess`, `PlatformAdmin`
- Schemas adicionales se crearán según specs futuras (ej. `inventory`, `billing`, `audit`).
- Migración 0001 debe crear los schemas explícitamente.

---

### Q-08/Q-09/Q-10/Q-11 — Validaciones de campos
**Pregunta**: regex de slug, formato de Branch.Code, casing de email, timezone de auditoría.
**Respuesta**: **aplicar validaciones**, detalles concretos en `/speckit.plan`.
**Implicación** (reglas mínimas confirmadas, detalles finos al plan):
- `Tenant.Slug`: solo lowercase + dígitos + guion (`[a-z0-9-]+`), longitud limitada, lista de slugs reservados.
- `Branch.Code`: alfanumérico, longitud limitada, único por tenant.
- `User.Email`: normalizado a lowercase al guardar; collation case-insensitive en BD.
- Auditoría timestamps: **UTC** siempre, `datetime2(7)`.
- FluentValidation (constitución §Restricciones #5) para validaciones de aplicación; restricciones de BD vía `IEntityTypeConfiguration<T>`.

---

### Q-12 — Tenant fundacional en migración 0001
**Pregunta**: ¿semilla de un primer tenant en la migración inicial?
**Respuesta**: **Sí**.
**Implicación**:
- Migración 0001 inserta un tenant fundacional (slug y nombre a confirmar en plan; tentativo `tallerpro` o `system`).
- Este tenant existe desde el día 1 y sirve para registros internos / accesos de plataforma si se necesitan.
- Distinto del seed dev/test (RF-14): aquel solo corre en entornos no productivos.

---

### Q-13 — Provider de tests
**Pregunta**: Testcontainers SQL Server o EF InMemory.
**Respuesta**: **Testcontainers SQL Server**.
**Implicación**:
- `Testcontainers.MsSql` ya está en CPM (spec 002).
- `TallerPro.Integration.Tests` y `TallerPro.Isolation.Tests` arrancan contenedor SQL Server por fixture compartida.
- EF InMemory **prohibido** en estos proyectos (no soporta global filters reales, schemas, ni rowversion fielmente).
- `TallerPro.Domain.Tests` y `TallerPro.Application.Tests` siguen sin BD (lógica pura).

---

## Notas para `/speckit.plan`

1. **Detalles regex/formato pendientes**: definir longitudes y patrones exactos de `Slug`, `Code`, `Email` y la lista de slugs reservados.
2. **Asignación schemas**: confirmar `core` vs `auth` para `UserBranchAccess` (referencia cruzada con `Branch` que está en `core`).
3. **Tenant fundacional**: definir slug, nombre, status y cuál estrategia de seed (sql-script vs `HasData`).
4. **`PlatformAdmin`**: definir campos (es nuevo, no estaba en spec original).
5. **Threat modeling obligatorio** (constitución §Gobernanza): invocar `security-reviewer` en plan para PII (email), tenant scoping (filtros) y `PlatformAdmin` (separación de superficies).

## Auto-delegación

- `security-reviewer`: **NO invocado en clarify** — el alcance de auth queda explícitamente fuera de spec 003 (diferido a spec 004). Se invoca obligatoriamente en `/speckit.plan` (constitución §Gobernanza) para threat modeling de PII y tenant scoping.
- `ui-ux-designer`: no aplica (sin UI).
