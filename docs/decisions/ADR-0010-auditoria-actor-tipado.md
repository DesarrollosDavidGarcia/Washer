# ADR-0010 — Auditoría con columnas FK tipadas (User vs PlatformAdmin)

- **Estado**: Accepted
- **Fecha**: 2026-05-04
- **Aceptado**: 2026-05-04
- **Spec**: `specs/003-core-data-model`
- **Decisores**: founder, security-reviewer

## Contexto

La spec 003 define `IAuditable` con `CreatedBy` y `UpdatedBy` para todas las entidades. La spec original modelaba estas columnas como `long?` (UserId/PlatformAdminId nullable), una sola columna por evento.

El security review (hallazgo ALTO-02) detectó que esto crea **ambigüedad referencial**: como `User.Id` y `PlatformAdmin.Id` son ambos `bigint IDENTITY` con secuencias independientes, el valor `CreatedBy = 5` puede referirse al `User(5)` o al `PlatformAdmin(5)`. Esto:

- Impide atribución forense unívoca.
- Hace imposible un join referencial directo en queries de UI ("creado por X").
- Incumple el espíritu de la auditoría como evidencia (un audit log ambiguo no es evidencia).

Opciones para resolver:

- **Opción A**: dos columnas FK tipadas (`CreatedByUserId`, `CreatedByPlatformId`) con CHECK constraint de exclusividad mutua.
- **Opción B**: una columna `CreatedBy long` + columna `CreatedByType tinyint` enum (`User=0`, `PlatformAdmin=1`, `System=2`). Sin FK referencial.
- **Opción C**: tabla polimórfica `Actors` con FK desde `Users` y `PlatformAdmins` heredando. Sobre-ingeniería.

## Decisión

**Adoptamos Opción A: cuatro columnas FK tipadas por entidad auditable**:

```
CreatedByUserId        bigint NULL  → FK auth.Users(Id)        ON DELETE NO ACTION
CreatedByPlatformId    bigint NULL  → FK auth.PlatformAdmins(Id) ON DELETE NO ACTION
UpdatedByUserId        bigint NULL  → FK auth.Users(Id)        ON DELETE NO ACTION
UpdatedByPlatformId    bigint NULL  → FK auth.PlatformAdmins(Id) ON DELETE NO ACTION
```

Constraints en cada tabla auditable:
- `CK_<Table>_CreatedByExclusive CHECK (CreatedByUserId IS NULL OR CreatedByPlatformId IS NULL)`
- `CK_<Table>_UpdatedByExclusive CHECK (UpdatedByUserId IS NULL OR UpdatedByPlatformId IS NULL)`

**Convención**:
- Acción de un `User` regular → set `CreatedByUserId`, `CreatedByPlatformId = NULL`.
- Acción de un `PlatformAdmin` → set `CreatedByPlatformId`, `CreatedByUserId = NULL`.
- Acción de sistema (seed, migración, job interno) → ambos `NULL`. El interceptor de auditoría puede emitir log adicional con `actorType = System`.

El `AuditingInterceptor` resuelve qué columna setear consultando `ICurrentUser` (interfaz nueva en `TallerPro.Security`), que expone `CurrentUserId: long?` y `CurrentPlatformAdminId: long?` con la garantía de que solo uno está poblado en cualquier request.

## Consecuencias

### Positivas

- **Atribución unívoca**: el actor está perfectamente identificado.
- **FK referencial intacta**: queries de auditoría pueden hacer `JOIN` natural a `Users` o `PlatformAdmins` sin lookups condicionales.
- **Defensa contra inserción inválida**: el CHECK constraint garantiza que la BD nunca tiene fila con ambos actores.
- **Reportes**: queries para "todas las acciones de admins de plataforma" filtran solo `CreatedByPlatformId IS NOT NULL`.

### Negativas

- **+2 columnas por entidad** (4 totales: Created y Updated × User y Platform). En SQL Server `bigint NULL` ocupa 8 bytes + flag de nulabilidad; impacto de espacio mínimo (~16 bytes por fila vs alternativa con tipo).
- **Más columnas en `IEntityTypeConfiguration<T>`**: configuración de FKs y CHECK más verbosa. Mitigable con extensión genérica `EntityTypeBuilder.ConfigureAuditable<T>()`.
- **Migraciones futuras**: si llega un tercer tipo de actor (ej. `IntegrationServiceAccount`), se añade otra columna sin romper esquema. Aceptable mientras los tipos sean pocos y estables.

### Negativas rechazadas (Opción B)

- **Pérdida de FK referencial**: una columna `long` apuntando a dos tablas distintas según un enum no se puede expresar como FK. Joins requieren CASE. Borrar un User no protege contra dejar audits huérfanos apuntando a Id reciclado de PlatformAdmin.
- **Riesgo de bug de aplicación**: si la app olvida setear `CreatedByType`, el `Id = 5` por default queda interpretable como User cuando podría ser admin. Errores silenciosos.

## Alcance del ADR

Aplica a **todas** las entidades que implementen `IAuditable` desde la migración 0001. Esta convención es estructural y no debe revisarse spec por spec.

Si en una spec futura se introduce un tercer tipo de actor (ej. servicio externo, bot), se añadirá una columna `<Op>By<Tipo>Id` adicional con su propio CHECK constraint. La regla mutuamente exclusiva pasa a ser "exactamente una FK no nula o todas nulas (sistema)".

## Referencias

- `specs/003-core-data-model/spec.md` RF-05
- `specs/003-core-data-model/plan.md` D-02
- `specs/003-core-data-model/data-model.md` (convenciones de auditoría)
- Security review (plan 003 §Hallazgos ALTO-02)
- Constitución §Restricciones #8 (soft delete y trazabilidad)
