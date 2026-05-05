# ADR-0009 — `User` global sin `TenantId`

- **Estado**: Accepted
- **Fecha**: 2026-05-04
- **Aceptado**: 2026-05-04
- **Spec**: `specs/003-core-data-model`
- **Decisores**: founder, security-reviewer

## Contexto

La constitución (§Identidad, jerarquía de cuentas) establece `User 1..N → UserBranchAccess (M:N)`, lo que ya sugiere que un `User` puede tener acceso a múltiples tenants vía el pivote `UserBranchAccess`. La pregunta abierta Q-02 de la spec 003 pedía confirmación explícita: ¿el `User` pertenece a un único tenant fijo, o es global?

Opciones:

1. **`User` con `TenantId` fijo**: cada User pertenece a un solo tenant. Para acceder a otro tenant se crea otra fila User con el mismo email + TenantId distinto. Pro: aislamiento físico de identidades. Contra: viola la regla "User 1..N", duplica datos del usuario, complica password reset y MFA (varias filas por persona).
2. **`User` global (sin `TenantId`)**: una fila por persona, identificada por email único global. El acceso a tenants se modela 100% vía `UserBranchAccess`. Pro: refleja la jerarquía de la constitución; soporta dueño con varios talleres. Contra: enumeración de emails cross-tenant (mitigable con timing-safe responses); lifecycle del User no está asociado a un tenant.

## Decisión

**`User` es global. No tiene columna `TenantId`. Email único global (vía índice filtrado WHERE `IsDeleted = 0`).**

El acceso a tenants se materializa exclusivamente vía la tabla pivote `UserBranchAccess`, que sí tiene `TenantId` denormalizado (decisión D-03 del plan 003) para que el global query filter de tenant aplique también a esa tabla.

## Consecuencias

### Positivas

- **Modelo coherente con constitución**: `User 1..N → UserBranchAccess (M:N)` se cumple literalmente.
- **Una persona = una identidad**: un dueño con tres talleres tiene un email, un password, un MFA, un lifecycle.
- **Onboarding multi-tenant simple**: invitar a un usuario existente a un tenant nuevo es solo un `INSERT` en `UserBranchAccess`.
- **Auth simple**: spec 004 implementa un único flujo de login que resuelve los tenants accesibles tras autenticar.

### Negativas

- **Enumeración de emails cross-tenant** (security review ALTO-03): un endpoint de auth que diferencie "email no existe" vs "credenciales incorrectas" filtra existencia de usuario sin importar tenant. **Mitigación obligatoria en spec 004**: respuestas timing-safe (mismo tiempo de cómputo en ambos casos) + mensaje genérico ("credenciales inválidas").
- **Lifecycle ambiguo**: si un User pierde acceso a su último tenant, el `User` sigue existiendo. Requiere policy explícita en spec 004 sobre cuándo soft-delete del User se considera apropiado.
- **PII centralizada**: el User es un único punto donde converge la PII de la persona. La defensa-in-depth de PII masking (constitución §Restricciones #11) es crítica.
- **Permisos cross-tenant**: un User con `Admin` en tenant A y `Cliente` en tenant B no debe ver datos de A cuando opera en B. Esto lo garantiza `ITenantContext` + global filters + `UserBranchAccess.TenantId` denormalizado.

### Restricciones para specs futuras

- **Spec 004 (Auth)**: timing-safe + mensajes uniformes en login, registro y password reset.
- **Spec 004 (Auth)**: el JWT debe contener el `TenantId` activo (seleccionado por el user al autenticar, si tiene acceso a más de uno) y NO la lista completa de tenants accesibles.
- **Cualquier spec con UI de "switcher" de tenants**: el switch debe revalidar `UserBranchAccess` server-side antes de emitir el nuevo token.

## Alternativas rechazadas

- **`User` con `TenantId`**: rompe constitución, duplica PII, complica MFA y password reset.
- **Tabla `UserPlatform` (global) + `UserTenant` (scoped)** como capas separadas: añade complejidad sin beneficio claro; las relaciones cross-tenant ya las modela `UserBranchAccess`.

## Referencias

- `specs/003-core-data-model/spec.md` RF-03
- `specs/003-core-data-model/plan.md` D-08
- `specs/003-core-data-model/clarify.md` Q-02
- Constitución §Identidad (jerarquía de cuentas)
- Security review (plan 003 §Hallazgos ALTO-03)
