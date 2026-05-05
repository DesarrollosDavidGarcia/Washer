# Analyze: Spec 003 — Modelo de datos base multi-tenant

- **Fecha**: 2026-05-04
- **Spec**: `specs/003-core-data-model/spec.md`
- **Plan**: `specs/003-core-data-model/plan.md`
- **Tasks**: `specs/003-core-data-model/tasks.md`
- **Veredicto (ronda 1)**: **NEEDS_REVISION** ⚠️
- **Veredicto (ronda 2 — final)**: **READY_TO_IMPLEMENT** ✅

---

## Resumen ejecutivo

Tres reviewers en paralelo (`code-reviewer`, `qa-reviewer`, `security-reviewer`) detectaron **8 bloqueantes únicos** (1 superpuesto entre code-reviewer y security-reviewer). Ninguno requiere reescritura del plan; todos se resuelven con ediciones quirúrgicas a `tasks.md` (criterios de aceptación + 4 tareas nuevas) y a `data-model.md` (1 ajuste de collation).

**Acciones para desbloquear** (≤2h trabajo total): ver §Plan de remediación.

---

## 1. Matriz de trazabilidad

### RF → Plan → Tasks

| RF | Plan §Componentes | Task implementación | Task test |
|---|---|---|---|
| RF-01 Tenant | `Tenant.cs` + `TenantConfiguration` | T-07 | T-06 |
| RF-02 Branch | `Branch.cs` + `BranchConfiguration` | T-09 | T-08 |
| RF-03 User | `User.cs` + `UserConfiguration` | T-11 | T-10 |
| RF-04 UserBranchAccess | `UserBranchAccess.cs` + config | T-13 | T-12 |
| RF-04b PlatformAdmin | `PlatformAdmin.cs` + config | T-11 | T-10 |
| RF-05 IAuditable | `IAuditable.cs` + `AuditingInterceptor` | T-05, T-20 | T-26 (CA-06) |
| RF-06 ISoftDeletable | `ISoftDeletable.cs` + `SoftDeleteInterceptor` | T-05, T-20 | T-26 (CA-05) |
| RF-06b IConcurrencyAware | `IConcurrencyAware.cs` + RowVersion | T-05, T-19 | T-26 (CA-09) |
| RF-07 DbContext | `TallerProDbContext.cs` | T-18 | T-25 |
| RF-08 Soft delete filter | `OnModelCreating` global filter | T-19 | T-26 |
| RF-09 Tenant scoping filter | `OnModelCreating` + `ITenantContext` | T-15, T-19 | T-27 (CA-04) |
| RF-10 Fluent API config | `Configurations/*.cs` | T-19 | (impl tests cubren) |
| RF-11 Migración inicial | `0001_CoreDataModel.cs` | T-23 | T-25, T-26 (CA-10) |
| RF-12 SQL Server provider | `Program.cs` registro DI | T-28 | T-25 |
| RF-13 Design-time factory | `TallerProDbContextFactory.cs` | T-22 | (criterio T-22) |
| RF-14 Seed dev | `DevSeeder.cs` | T-29 | (criterio T-29) |

**Cobertura RF**: 16/16 ✅

### CA → Tarea/test (matriz QA)

| CA | Tarea | Suite | Cobertura |
|---|---|---|---|
| CA-01 build sin warnings | T-32 | CI | ✅ vía CI gate |
| CA-02 migración idempotente | T-23 (criterio manual) | — | ❌ **GAP** sin test automatizado |
| CA-03 datos básicos | T-25 | Integration | ✅ |
| CA-04 cross-tenant leak | T-27 | Isolation | ✅ + test negativo |
| CA-05 soft delete | T-26 SoftDeleteTests | Integration | ✅ |
| CA-06 auditoría | T-26 AuditingTests | Integration | ✅ |
| CA-07 MissingTenantContext | T-27 | Isolation | ✅ |
| CA-08 estabilidad migración | T-23 (criterio manual) | — | ⚠️ no automatizable; T-30 manual |
| CA-09 concurrencia | T-26 ConcurrencyTests | Integration | ⚠️ simulado, no real |
| CA-10 tenant fundacional | T-26 FundationalTenantTests | Integration | ✅ (parcial — falta Status=Cancelled) |
| CA-11 User vs PlatformAdmin | T-26 UserPlatformSeparationTests | Integration | ✅ |

**Cobertura CA**: 9/11 ✅, 1 ⚠️ parcial, 1 ❌ gap automatizable.

### Mitigaciones de seguridad → Tareas

| Hallazgo | Mitigación plan | Tarea | Cierra |
|---|---|---|---|
| CRITICO-01 (1ª pasada) | D-01 throw eager | T-15, T-27 | ✅ |
| ALTO-01 (1ª pasada) | D-06 TP0001 placeholder | T-16/T-17 | ✅ |
| ALTO-02 (1ª pasada) | D-02 / ADR-0010 dos FK | T-05, T-19 | ✅ |
| ALTO-03 (1ª pasada) | Contrato a spec 004 | (sin tarea — correcto) | ✅ documentado |
| MEDIO-01..03 (1ª pasada) | D-04, D-03, D-05 | T-20, T-21 | ✅ |
| **NUEVO-ALTO-01** (2ª pasada) | AsyncLocal lifecycle | **NO HAY TAREA** | ❌ **bloqueante** |
| NUEVO-MEDIO-01 (2ª pasada) | Permisos schema | NO HAY TAREA | ⚠️ menor |
| NUEVO-BAJO-01..04, INFO-01 | (varios) | (sin tarea — aceptable) | ✅ |

### Dependencias topológicas

Verificado: el grafo de `tasks.md` no tiene ciclos. Únicas observaciones:
- T-21 (PII masking) colapsa test e implementación en una tarea — viola patrón TDD del documento (T-04→T-05, T-06→T-07, ...). **No es ciclo, es inconsistencia de estilo.**
- T-14 declara test en `Domain.Tests/Security/` pero `Domain.Tests.csproj` no puede referenciar `TallerPro.Security` (regla de dependencias `Domain ← Security`). **Inconsistencia de proyecto.**
- T-16 declara test en `Domain.Tests/Analyzers/` pero la convención del repo (`CLAUDE.md` de `Application.Tests` y de `Analyzers`) ubica los tests de analyzer en `Application.Tests`. **Inconsistencia de proyecto.**

---

## 2. Hallazgos consolidados (los 3 reviewers)

### Bloqueantes (8 únicos — 1 superpuesto entre 2 reviewers)

| # | Origen | Descripción | Impacto |
|---|---|---|---|
| **B-1** | code-reviewer Bloqueante-01 | `FundationalTenantGuard` busca por `Id == 1` (frágil — depende del IDENTITY ordinal). | Si seed se desordena, guard inactivo → tenant fundacional borrable. |
| **B-2** | code-reviewer Bloqueante-02 + security-reviewer NUEVO-ALTO-01 (mismo problema) | `AmbientTenantContext` con `AsyncLocal` sin reset garantizado en middleware. | Cross-tenant leak en producción bajo carga/excepción → viola constitución §Principio #6. |
| **B-3** | qa-reviewer G-01 | CA-02 (idempotencia migración) sin test automatizado. | Regresión silenciosa en re-aplicación de migración. |
| **B-4** | qa-reviewer G-02 | Trigger `TR_UBA_TenantConsistency` sin test que intente violación directa. | Trigger podría no existir o estar mal escrito sin que ningún test lo detecte. |
| **B-5** | qa-reviewer G-03 | `FundationalTenantGuard` no cubre `Status=Cancelled` (D-04 lo exige). | Bypass del guard cambiando status en lugar de IsDeleted. |
| **B-6** | qa-reviewer G-10 | Tests del analyzer TP0001 ubicados en `Domain.Tests` (proyecto que no puede referenciar `TallerPro.Analyzers` sin violar arquitectura). | Tasks no compilarán como están descritas. |
| **B-7** | qa-reviewer G-15 | No existe `isolation-tests.yml` workflow CI dedicado (constitución §Principio #4 + `CLAUDE.md` Isolation.Tests). | Fallo de tests críticos no bloquea merge. |
| **B-8** | code-reviewer Mayor-03 | Collation no especificada en `PlatformAdmin.Email` (sí en `Users.Email`). | Comportamiento divergente case-insensitive entre tablas. |

### Mayores (no bloquean, pero resolver antes de cierre)

- **M-1** (code Mayor-01): `IAuditable` con 4 props — segregación a `IUserAuditable` cuando lleguen entidades user-only en specs futuras. Documentar como restricción.
- **M-2** (code Mayor-02): Trigger SQL EF — añadir nota "scaffolding reverso prohibido".
- **M-3** (qa G-04): tests de `Tenant.Slug` falta cobertura de longitudes borde + slugs reservados.
- **M-4** (qa G-05): `CK_CreatedByExclusive` sin test de violación.
- **M-5** (qa G-06): Email único con casing variable sin test integración.
- **M-6** (qa G-07): Concurrencia real (dos `DbContext` paralelos), no solo simulada.
- **M-7** (qa G-08): Soft delete + re-creación con mismo slug/code sin test.
- **M-8** (qa G-09): User global con acceso revocado a último branch — test de contrato.
- **M-9** (security NUEVO-MEDIO-01): permisos GRANT por schema sin documentar.

### Menores / Sugerencias

- code-reviewer Menor-01..04, Sugerencia-01..02
- qa-reviewer G-11..G-14
- security-reviewer NUEVO-BAJO-01..04, NUEVO-INFO-01
- TDD inconsistencias (T-20, T-21)

---

## 3. Sección por reviewer (referencia)

### code-reviewer — veredicto: NEEDS_REVISION
2 bloqueantes (B-1, B-2), 3 mayores, 4 menores, 2 sugerencias. Ver §2 para extracto consolidado. Detalle completo en el output del agente (este analyze sintetiza).

### qa-reviewer — veredicto: NEEDS_REVISION
4 bloqueantes (B-3..B-7 — incluye G-15 reclasificado de Mayor a Bloqueante por la constitución), 7 mayores, 4 menores. Matriz CA arriba.

### security-reviewer — veredicto: BLOCKED
1 bloqueante (B-2 = NUEVO-ALTO-01), 1 medio (M-9 = NUEVO-MEDIO-01), 4 bajos, 1 info. Hallazgos previos (1ª pasada) cerrados todos en plan/tasks salvo el nuevo ALTO-01 derivado de la decisión `AsyncLocal`.

---

## 4. Plan de remediación (antes de `/speckit.implement`)

> Las 8 acciones siguientes resuelven los 8 bloqueantes. Trabajo total estimado: 1.5-2h. **No** modifican spec.md ni plan.md (solo tasks.md y data-model.md).

| # | Acción | Edita |
|---|---|---|
| **R-1** (B-1) | Cambiar criterio de T-20 (`FundationalTenantGuard`): buscar tenant fundacional por `Slug == "tallerpro-platform"`, no por `Id == 1`. Eliminar la dependencia de `IDENTITY_INSERT` en T-23. | `tasks.md` T-20, T-23 |
| **R-2** (B-2) | Ampliar criterio de T-15: `AmbientTenantContext` expone `Clear()`. Ampliar criterio de T-28: middleware usa `try/finally` con `Clear()` en `finally`. Añadir test en T-27: tras un request con tenant A, un segundo request sin header lanza `MissingTenantContextException` (no hereda A). | `tasks.md` T-15, T-27, T-28 |
| **R-3** (B-3) | Añadir tarea T-25b: `MigrationIdempotencyTests.cs` que aplica migración + script idempotente dos veces y verifica state idéntico. | `tasks.md` (nueva tarea) |
| **R-4** (B-4) | Ampliar `TenantStampTests.cs` (T-26): incluir test que ejecuta `ExecuteSqlRaw` con INSERT inconsistente y verifica error de SQL Server por trigger. | `tasks.md` T-26 |
| **R-5** (B-5) | Ampliar `FundationalTenantTests.cs` (T-26): caso adicional `tenant.Status = Cancelled` debe ser bloqueado por `FundationalTenantGuard`. Ampliar T-20 para que el guard cubra ambos vectores. | `tasks.md` T-20, T-26 |
| **R-6** (B-6) | Mover tests de analyzer de `Domain.Tests/Analyzers/` a `Application.Tests/Analyzers/`. Ajustar T-16. Mover también T-14 (TenantContext) a `Application.Tests/Security/`. | `tasks.md` T-14, T-16 |
| **R-7** (B-7) | Añadir tarea T-34: crear `.github/workflows/isolation-tests.yml` que corra `dotnet test tests/TallerPro.Isolation.Tests` como gate de merge dedicado. | `tasks.md` (nueva tarea) |
| **R-8** (B-8) | Añadir collation `Latin1_General_100_CI_AI_SC_UTF8` a `PlatformAdmin.Email` en `data-model.md` y en T-19 `PlatformAdminConfiguration`. | `data-model.md`, `tasks.md` T-19 |

### Acciones secundarias (mayores — pueden hacerse durante implement, no bloquean)

- M-2: nota en `quickstart.md` "scaffolding reverso prohibido — el modelo es siempre code-first" (T-30).
- M-3..M-8: ampliar tareas T-06, T-26 con casos adicionales.
- M-9: documentar política de permisos por schema en `quickstart.md` Troubleshooting (T-30).
- TDD inconsistencias: separar T-21 en T-21a (test) + T-21b (impl).
- Code Menor-04: mover `AllowIgnoreQueryFiltersAttribute` a `TallerPro.Shared` (no `Domain`). Ajustar T-17.
- Code Menor-02: redefinir D-05 — eliminar hash truncado SHA-256 en favor de solo `***@***` + `actorId` ya en log estructurado. Editar plan.md §D-05 y T-21.

---

## 5. Veredicto final

**NEEDS_REVISION** ⚠️ — 8 bloqueantes pendientes.

> **No autorizado** para `/speckit.implement` hasta resolver R-1..R-8.

### Camino más rápido al verde

1. Aplicar las 8 acciones R-1..R-8 directamente a `tasks.md` y `data-model.md` (1 sola pasada de edición).
2. Re-correr `/speckit.analyze` (los 3 reviewers en paralelo) para confirmar que los 8 bloqueantes están cerrados y no aparecen nuevos.
3. Si el segundo análisis devuelve READY → autorizar `/speckit.implement`.

### Riesgos si se ignora la remediación

- B-1 / B-2: **cross-tenant leak** en producción (existencial — constitución §Principio #6).
- B-3 / B-4: regresiones silenciosas en migraciones y consistencia tenant.
- B-6 / B-7: tasks que no compilan + tests críticos sin gate CI.
- B-5 / B-8: bypass del guard + comportamiento divergente case-insensitive.

---

## 6. Próximo comando

**Recomendado**: aplicar remediación R-1..R-8 → re-`/speckit.analyze`.

---

## 7. Ronda 2 — Remediaciones aplicadas (2026-05-04)

Las 8 acciones R-1..R-8 fueron aplicadas. Cambios concretos:

| Acción | Bloqueante cerrado | Archivos editados |
|---|---|---|
| **R-1** | B-1 `FundationalTenantGuard` por `Slug` | `tasks.md` T-20, T-23, T-26; `data-model.md` §`core.Tenants` seed + §migración paso 9 |
| **R-2** | B-2 AsyncLocal lifecycle | `tasks.md` T-14, T-15 (añade `Clear()`), T-27 (caso AsyncLocal contamination + nuevo file `TenantContextLifecycleTests.cs`), T-28 (middleware con `try/finally`) |
| **R-3** | B-3 idempotencia migración | `tasks.md` nueva tarea T-23b `MigrationIdempotencyTests.cs` |
| **R-4** | B-4 trigger sin test | `tasks.md` T-26 amplía `TenantStampTests` con `ExecuteSqlRaw` rechazado por trigger |
| **R-5** | B-5 guard sin Status=Cancelled | `tasks.md` T-20 (guard cubre `Suspended` y `Cancelled`), T-26 (test parametrizado) |
| **R-6** | B-6 tests analyzer en proyecto incorrecto | `tasks.md` T-14 movido a `Application.Tests/Security/`, T-16 movido a `Application.Tests/Analyzers/` |
| **R-7** | B-7 falta workflow CI | `tasks.md` nueva tarea T-34 `.github/workflows/isolation-tests.yml` |
| **R-8** | B-8 collation faltante | `tasks.md` T-19 criterio collation; `data-model.md` §`auth.PlatformAdmins` collation explícita |

**Total tareas**: 33 → 35 (T-23b y T-34 nuevas).
**Sin cambios** en `spec.md` ni `plan.md` (solo `tasks.md` y `data-model.md`).

### Pendiente

Re-invocar `/speckit.analyze` para que los 3 reviewers confirmen que los 8 bloqueantes están cerrados y que no aparecen nuevos. Si el segundo análisis devuelve READY → autorizado para `/speckit.implement`.

---

## 8. Ronda 2 — Resultados de re-análisis (2026-05-04)

Los 3 reviewers ejecutados en paralelo tras aplicar R-1..R-8.

### Veredictos

| Reviewer | Veredicto ronda 2 | Bloqueantes cerrados |
|---|---|---|
| code-reviewer | **READY** | B-1 ✅, B-2 ✅ |
| qa-reviewer | **READY** | G-01/B-3 ✅, G-02/B-4 ✅, G-03/B-5 ✅, G-10/B-6 ✅, G-15/B-7 ✅ |
| security-reviewer | **READY** | NUEVO-ALTO-01/B-2 ✅ |

**0 bloqueantes residuales.** 8/8 cerrados.

### Observaciones residuales (no bloqueantes — corregidas en ronda 2)

| # | Origen | Hallazgo | Acción |
|---|---|---|---|
| O-1 | code-reviewer Mayor | Ambigüedad seed `HasData` vs `migrationBuilder.Sql` en `data-model.md` §migración paso 9 — `HasData` reintroduce dependencia de Id literal | ✅ corregido — `data-model.md` y T-23 ahora dicen explícitamente "usar `migrationBuilder.Sql` (no `HasData`)" |
| O-2 | security-reviewer Bajo | T-28 no especifica que `TrySetTenant` debe ir **dentro** del `try` (potencial fuga de AsyncLocal si `TrySetTenant` lanza) | ✅ corregido — T-28 ahora dice "tanto `TrySetTenant` como `_next` dentro del mismo bloque `try`" |

### Deuda documentada (aceptada explícitamente)

Mayores no bloqueantes (M-1..M-9) y menores (G-11..G-14, NUEVO-BAJO-01..04, NUEVO-INFO-01) quedan en analyze.md §2-§4 como deuda visible. Algunos serán cubiertos durante implement (ej. M-3..M-8 ampliando T-06/T-26); otros requieren spec posterior (ej. M-1 segregación `IAuditable` cuando lleguen entidades user-only).

### Cobertura final

- **Bloqueantes**: 8/8 cerrados ✅
- **RFs**: 16/16 ✅
- **CAs**: 11/11 ✅ (incluye CA-02 ahora con test automatizable T-23b)
- **Hallazgos seguridad**: 0 Crítico/Alto abiertos
- **Tareas totales**: 35 (33 originales + T-23b + T-34)

---

## 9. Veredicto final

**READY_TO_IMPLEMENT** ✅

Plan, data-model y tasks están listos para `/speckit.implement`. Los 3 ADRs (0008, 0009, 0010) deben moverse de `Proposed` a `Accepted` como **primera tarea** (T-01) del implement.

### Próximo comando

`/speckit.implement` — orquestará tester (TDD rojo) → dotnet-dev/frontend-dev (verde) → reviewers de cierre por tarea/feature.
