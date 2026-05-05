# ADR-0008 — Estrategia de identificadores: `bigint IDENTITY`

- **Estado**: Accepted
- **Fecha**: 2026-05-04
- **Aceptado**: 2026-05-04
- **Spec**: `specs/003-core-data-model`
- **Decisores**: founder, security-reviewer

## Contexto

La spec 003 establece las primeras tablas con persistencia (`Tenants`, `Branches`, `Users`, `UserBranchAccesses`, `PlatformAdmins`). Antes de generar la migración 0001 hay que fijar la estrategia de PKs porque cambiarla después implica migración destructiva.

Opciones consideradas:

1. **`Guid` (UNIQUEIDENTIFIER)**: clustered con NEWSEQUENTIALID o non-clustered con NEWID. Pro: globales, generables en cliente, no fugan cardinalidad. Contra: 16 bytes vs 8 de bigint, fragmentación si no son secuenciales, debugging menos amigable.
2. **`Guid v7` (ordenado por tiempo)**: pro de Guid + insertion order amigable. Contra: requiere librería externa o generación custom; menos soporte tooling.
3. **`bigint IDENTITY`**: 8 bytes, IDENTITY del motor, clustered natural. Pro: performance, simple, soporte universal. Contra: filtran cardinalidad en URLs, requieren slug/code para exposición pública.
4. **`ULID`**: 16 bytes ordenados por tiempo. Pro de Guid + ordering. Contra: tooling SQL inmaduro, requiere conversión.

## Decisión

**Adoptamos `bigint IDENTITY(1,1)` para todas las PKs.**

URLs públicas y respuestas de API NUNCA exponen el `Id` numérico. Para identificación externa se usan:
- `Tenant.Slug` (público, lower-case, regex controlado).
- `Branch.Code` (alfanumérico, único por tenant).
- `User.Email` (PII, lookup interno; nunca en URLs públicas).
- Para entidades futuras sin slug/code natural, se introducirá una columna `PublicId` (Guid v7 generado en aplicación) en la spec correspondiente. Decisión revisitable.

## Consecuencias

### Positivas

- **Performance**: índices clustered eficientes; FKs y joins económicos.
- **Tamaño**: 50% menos espacio que Guid en PKs y FKs.
- **Tooling**: debugging con SQL Profiler/SSMS trivial; secuencias predecibles para tests.
- **Migraciones**: `HasData(Id = 1)` para el tenant fundacional es directo; con Guid hay que pre-generar y commitear el valor.

### Negativas

- **Cardinalidad expuesta** si un endpoint accidentalmente devuelve `Id`. Mitigación: el Roslyn analyzer (spec 005 logica plena) puede detectar uso de `Id` en respuestas API; mientras tanto, code review.
- **No generables en cliente**: el cliente Hybrid offline no puede crear Ids antes de sincronizar. Mitigación: usar Outbox pattern + correlación con Guid temporal (cliente) → mapping a `Id` real al sync (spec dedicada del sync engine).
- **Imposibilidad de merge cross-environment**: dos entornos generarán secuencias distintas. No es problema mientras la BD sea la fuente de verdad de cada ambiente.

## Alternativas rechazadas

- **Guid v7 / ULID**: aportarían generación cliente y ordering, pero el costo de tooling y el 2× tamaño en espacio no se justifica para una single-DB multi-tenant cuyo cliente offline ya tiene su propia estrategia (LocalDb + Outbox).

## Referencias

- `specs/003-core-data-model/plan.md` D-07
- `specs/003-core-data-model/data-model.md` (convenciones globales)
- Constitución §Restricciones (no menciona PK strategy explícitamente; este ADR la fija)
