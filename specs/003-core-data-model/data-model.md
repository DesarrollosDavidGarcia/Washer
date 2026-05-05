# Data Model: Spec 003 — Modelo de datos base multi-tenant

- **Spec**: `specs/003-core-data-model/spec.md`
- **Plan**: `specs/003-core-data-model/plan.md`

> Tablas, columnas, tipos SQL Server, índices, FKs, constraints. Fuente de verdad para `IEntityTypeConfiguration<T>` y migración `0001_CoreDataModel`.

## Convenciones globales

- **PKs**: `Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY` (D-07 / ADR-0008).
- **Naming**: PascalCase para tablas, columnas, schemas, FKs, índices, constraints. Singular en entidades C#, **plural** en tablas SQL (`Tenants`, `Branches`, etc. — convención EF Core default).
- **Auditoría** (todas las entidades de esta spec):
  ```
  CreatedAt              datetime2(7) NOT NULL                  -- UTC
  UpdatedAt              datetime2(7) NOT NULL                  -- UTC
  CreatedByUserId        bigint NULL  → FK auth.Users(Id)       -- ON DELETE NO ACTION
  CreatedByPlatformId    bigint NULL  → FK auth.PlatformAdmins(Id)
  UpdatedByUserId        bigint NULL  → FK auth.Users(Id)
  UpdatedByPlatformId    bigint NULL  → FK auth.PlatformAdmins(Id)
  ```
  Constraint en cada tabla: `CK_<Table>_CreatedByExclusive CHECK (CreatedByUserId IS NULL OR CreatedByPlatformId IS NULL)` (idem `Updated`).
- **Soft delete**:
  ```
  IsDeleted              bit NOT NULL DEFAULT 0
  DeletedAt              datetime2(7) NULL                      -- UTC
  ```
- **Concurrency**:
  ```
  RowVersion             rowversion NOT NULL                    -- timestamp en SQL Server
  ```
- **Índices con filtro `WHERE IsDeleted = 0`** para todos los índices únicos (de lo contrario un soft delete + re-creación con mismo slug/email/etc. fallaría por conflicto).

## Schemas

```sql
CREATE SCHEMA core AUTHORIZATION dbo;
CREATE SCHEMA auth AUTHORIZATION dbo;
```

Schemas adicionales se agregarán en specs posteriores (`inventory`, `billing`, `audit`, ...).

---

## `core.Tenants`

| Columna | Tipo SQL | Null | Notas |
|---|---|---|---|
| Id | bigint IDENTITY | NO | PK |
| Name | nvarchar(120) | NO | Razón social / nombre comercial |
| Slug | varchar(40) | NO | Único global (vía índice filtrado); regex `^[a-z0-9](?:[a-z0-9-]{1,38}[a-z0-9])?$`; longitud 3..40 |
| Status | tinyint | NO | Enum `TenantStatus`: 0=Active, 1=Suspended, 2=Cancelled |
| CreatedAt | datetime2(7) | NO | UTC |
| UpdatedAt | datetime2(7) | NO | UTC |
| CreatedByUserId | bigint | YES | FK |
| CreatedByPlatformId | bigint | YES | FK |
| UpdatedByUserId | bigint | YES | FK |
| UpdatedByPlatformId | bigint | YES | FK |
| IsDeleted | bit | NO | default 0 |
| DeletedAt | datetime2(7) | YES | UTC |
| RowVersion | rowversion | NO | concurrency |

**Constraints**:
- `PK_Tenants` PRIMARY KEY (Id)
- `UQ_Tenants_Slug` UNIQUE INDEX en `Slug` filtrado WHERE `IsDeleted = 0`
- `CK_Tenants_CreatedByExclusive` CHECK (CreatedByUserId IS NULL OR CreatedByPlatformId IS NULL)
- `CK_Tenants_UpdatedByExclusive` CHECK (UpdatedByUserId IS NULL OR UpdatedByPlatformId IS NULL)
- `CK_Tenants_SlugLowercase` CHECK (Slug = LOWER(Slug))

**Índices**:
- `IX_Tenants_Status` ON (Status) WHERE IsDeleted = 0

**Seed (migración 0001)**: `Name='TallerPro Platform', Slug='tallerpro-platform', Status=0 (Active), CreatedAt=UtcNow del momento de migración, RowVersion=auto`. **Sin** `IDENTITY_INSERT` — el `Id` lo asigna el motor (será `1` en BD limpia, pero la identificación canónica es por `Slug`). `FundationalTenantGuard` busca por `Slug == 'tallerpro-platform'` (R-1).

---

## `core.Branches`

| Columna | Tipo SQL | Null | Notas |
|---|---|---|---|
| Id | bigint IDENTITY | NO | PK |
| TenantId | bigint | NO | FK → core.Tenants(Id), ON DELETE NO ACTION |
| Name | nvarchar(120) | NO | |
| Code | varchar(20) | NO | Único por tenant; regex `^[A-Z0-9-]{2,20}$` |
| (auditoría + soft delete + RowVersion) | … | … | ver convenciones |

**Constraints**:
- `PK_Branches`
- `FK_Branches_Tenants` FOREIGN KEY (TenantId) REFERENCES core.Tenants(Id) ON DELETE NO ACTION
- `UQ_Branches_TenantId_Code` UNIQUE INDEX (TenantId, Code) WHERE IsDeleted = 0
- `CK_Branches_CreatedByExclusive`, `CK_Branches_UpdatedByExclusive`
- `CK_Branches_CodeFormat` CHECK (Code NOT LIKE '%[^A-Z0-9-]%')

**Índices**:
- `IX_Branches_TenantId` ON (TenantId) WHERE IsDeleted = 0 — soporte global filter de tenant scoping

---

## `auth.Users`

| Columna | Tipo SQL | Null | Notas |
|---|---|---|---|
| Id | bigint IDENTITY | NO | PK |
| Email | varchar(254) | NO | Único global (vía índice filtrado); lowercase normalizado al guardar; collation `Latin1_General_100_CI_AI_SC_UTF8` |
| DisplayName | nvarchar(120) | NO | |
| (auditoría + soft delete + RowVersion) | … | … | |

**Sin** `TenantId` (D-08 / ADR-0009).

**Constraints**:
- `PK_Users`
- `UQ_Users_Email` UNIQUE INDEX en `Email` filtrado WHERE IsDeleted = 0
- `CK_Users_EmailLowercase` CHECK (Email = LOWER(Email))
- `CK_Users_CreatedByExclusive`, `CK_Users_UpdatedByExclusive`

**PII**: `Email` (alto), `DisplayName` (bajo). Atributo `[PiiData]` en C#; Serilog enmascara al loguear.

---

## `auth.PlatformAdmins`

| Columna | Tipo SQL | Null | Notas |
|---|---|---|---|
| Id | bigint IDENTITY | NO | PK |
| Email | varchar(254) | NO | Único global; lowercase normalizado al guardar; collation `Latin1_General_100_CI_AI_SC_UTF8` (idéntica a `auth.Users.Email` para consistencia case-insensitive entre tablas) |
| DisplayName | nvarchar(120) | NO | |
| (auditoría + soft delete + RowVersion) | … | … | |

Sin FK a `auth.Users` — entidad totalmente separada (D-03 spec / ADR no requerido, decisión Q-03).

**Constraints**:
- `PK_PlatformAdmins`
- `UQ_PlatformAdmins_Email` UNIQUE INDEX filtrado WHERE IsDeleted = 0
- `CK_PlatformAdmins_EmailLowercase` CHECK (Email = LOWER(Email))
- `CK_PlatformAdmins_CreatedByExclusive`, `CK_PlatformAdmins_UpdatedByExclusive`

---

## `auth.UserBranchAccesses`

| Columna | Tipo SQL | Null | Notas |
|---|---|---|---|
| Id | bigint IDENTITY | NO | PK |
| UserId | bigint | NO | FK → auth.Users(Id) ON DELETE NO ACTION |
| BranchId | bigint | NO | FK → core.Branches(Id) ON DELETE NO ACTION |
| TenantId | bigint | NO | FK → core.Tenants(Id) — denormalizado (D-03), set por interceptor |
| Role | tinyint | NO | Enum `Role`: 0=SuperAdmin, 1=Admin, 2=Cliente |
| (auditoría + soft delete + RowVersion) | … | … | |

**Constraints**:
- `PK_UserBranchAccesses`
- `FK_UBA_Users` (UserId) → auth.Users(Id)
- `FK_UBA_Branches` (BranchId) → core.Branches(Id)
- `FK_UBA_Tenants` (TenantId) → core.Tenants(Id)
- `UQ_UBA_User_Branch` UNIQUE INDEX (UserId, BranchId) WHERE IsDeleted = 0 — un user solo tiene un access activo por branch
- `CK_UBA_CreatedByExclusive`, `CK_UBA_UpdatedByExclusive`
- `CK_UBA_RoleValid` CHECK (Role IN (0, 1, 2))
- **Trigger** `TR_UBA_TenantConsistency` (AFTER INSERT, UPDATE): valida que `TenantId == (SELECT TenantId FROM core.Branches WHERE Id = BranchId)` — defense-in-depth contra inconsistencia denormalizada.

**Índices**:
- `IX_UBA_TenantId` ON (TenantId) WHERE IsDeleted = 0 — soporte global filter
- `IX_UBA_UserId` ON (UserId) WHERE IsDeleted = 0 — listado de accesos de un user
- `IX_UBA_BranchId` ON (BranchId) WHERE IsDeleted = 0

---

## Diagrama relacional (ASCII)

```
                  ┌─────────────────────┐
                  │   core.Tenants      │
                  │  Id (PK)            │
                  │  Slug, Name, Status │
                  └─────────┬───────────┘
                            │ 1
                            │
                       ┌────┴────┐
                       │ N       │ N (denormalizado)
                       │         │
              ┌────────▼────┐    │
              │ core.Branches│   │
              │  Id (PK)     │   │
              │  TenantId(FK)│   │
              │  Code, Name  │   │
              └──────┬───────┘   │
                     │ 1         │
                     │           │
                     │ N         │
              ┌──────▼───────────▼─────────┐
              │  auth.UserBranchAccesses    │
              │   Id (PK)                   │
              │   UserId (FK)               │
              │   BranchId (FK)             │
              │   TenantId (FK denorm)      │
              │   Role enum                 │
              └──────┬──────────────────────┘
                     │ N
                     │ 1
              ┌──────▼──────────┐      ┌────────────────────────┐
              │   auth.Users    │      │  auth.PlatformAdmins   │
              │   Id (PK)       │      │   Id (PK)              │
              │   Email (UQ)    │      │   Email (UQ)           │
              │   DisplayName   │      │   DisplayName          │
              └─────────────────┘      └────────────────────────┘
                                          (sin FK a Users — separación intencional)
```

## Filtros globales (resumen aplicado por DbContext)

| Entidad | Soft delete | Tenant scoping |
|---|---|---|
| `Tenant` | Sí | No (es la raíz) |
| `Branch` | Sí | Sí (`TenantId == _ctx.CurrentTenantId`) |
| `User` | Sí | No (global) |
| `PlatformAdmin` | Sí | No (global) |
| `UserBranchAccess` | Sí | Sí (`TenantId == _ctx.CurrentTenantId`) |

## Migración inicial — `0001_CoreDataModel`

Pasos en orden:

1. `CREATE SCHEMA core`, `CREATE SCHEMA auth`.
2. Crear `auth.Users`, `auth.PlatformAdmins` (sin FKs a otras tablas → primero).
3. Crear `core.Tenants` (sin FK aún a Users/PlatformAdmins de auditoría — se agregan al final como `ALTER TABLE`).
4. Crear `core.Branches` (FK a Tenants).
5. Crear `auth.UserBranchAccesses` (FKs a Users, Branches, Tenants).
6. `ALTER TABLE` para añadir las FKs de auditoría (`CreatedByUserId`, `CreatedByPlatformId`, etc.) en cada tabla — se hacen al final para evitar dependencia circular en creación.
7. Crear índices únicos filtrados, índices de tenant scoping, índices de FK.
8. Crear trigger `TR_UBA_TenantConsistency`.
9. **Seed del tenant fundacional** (R-1 — sin IDENTITY_INSERT):
   ```sql
   INSERT INTO core.Tenants (Name, Slug, Status, CreatedAt, UpdatedAt, IsDeleted)
   VALUES ('TallerPro Platform', 'tallerpro-platform', 0, SYSUTCDATETIME(), SYSUTCDATETIME(), 0);
   ```

**Nota** sobre mecanismo de seed: usar **`migrationBuilder.Sql(...)`** dentro del `Up()` de la migración 0001 (no `HasData`). Razón: `HasData` exige un `Id` literal en el modelo snapshot que reintroduciría dependencia del valor numérico — exactamente la fragilidad que R-1 elimina. Con `migrationBuilder.Sql` el motor asigna el `Id` por IDENTITY y la identificación canónica permanece `Slug = 'tallerpro-platform'`. El `Down()` correspondiente debe hacer `DELETE FROM core.Tenants WHERE Slug = 'tallerpro-platform'`.

**Scaffolding**: `dotnet ef dbcontext scaffold` está **prohibido** en este proyecto. El modelo es siempre code-first; el trigger SQL `TR_UBA_TenantConsistency` no sería re-generado por scaffolding y se perdería en sucesivas migraciones.

## Validaciones FluentValidation (en `TallerPro.Application` cuando aplique; placeholders aquí)

> Esta spec no implementa casos de uso aún. Cuando spec 004+ los añadan, las validaciones a nivel de aplicación deben respetar las reglas BD definidas aquí.

- `TenantSlugValidator`: regex `^[a-z0-9](?:[a-z0-9-]{1,38}[a-z0-9])?$`, no en lista reservada.
- `BranchCodeValidator`: regex `^[A-Z0-9-]{2,20}$`, único por tenant.
- `EmailValidator`: regex RFC 5321 simplificada + lowercase.

## Lista de slugs reservados (D-04)

```
system, admin, root, api, www, mail, tallerpro, tallerpro-platform,
test, demo, platform, super, internal, app, dashboard, support,
billing, status, public, static
```
