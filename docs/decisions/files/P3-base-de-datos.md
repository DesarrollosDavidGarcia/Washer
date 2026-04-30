# Prompt 3 de 7 — Base de Datos Completa (DDL Ejecutable)

## Contexto del Proyecto

**TallerPro**: SaaS multitenant + multitaller para talleres mecánicos en México. Stack: .NET Core 9 + EF Core 9 + SQL Server + Blazor Hybrid.

**Jerarquía**: Tenant → Branches → Orders/Inventory/Warehouses. Users M:N Branches. Customers scope Tenant.

**Decisiones fundacionales ya tomadas (asumir)**:
- **Multitenancy**: Single DB con `TenantId` discriminator + EF Core Global Query Filters + SQL Server Row-Level Security (defense in depth).
- **Soft delete estricto**: toda entidad de negocio tiene `DeletedAt`, `DeletedBy`, `DeleteReason`. Excepciones inmutables (JAMÁS deleted): `InventoryMovements`, `CFDIs`, `Payments`, `AuditLog`, `SystemAuditLog`, `AIEvents`, `MeterEvents`, `ImpersonationAudits`, `SupportAccessLog`.
- **Concurrencia optimista** con `RowVersion` en entidades con escritura concurrente.
- **Pricing model**: Base por taller + sucursal adicional + overage metered sobre pools (CFDI, WhatsApp, IA, SMS, storage).
- **Usage tracking** con Stripe Meters y contadores locales reseteables por ciclo de facturación.
- **Impersonation governance**: founder acceso total, equipo futuro read-only, notificación al tenant por email.
- **Catálogos genéricos** para entidades estructura `{Code, Name, Description, DisplayOrder, ExtraJson}`.

---

## Tu Rol

Actúa como **DBA + Arquitecto de Datos Senior** especializado en:
- SQL Server multitenant en producción real con 1000+ tenants.
- Estrategias de indexación compuesta para multitenancy.
- SQL Server Row-Level Security nativo.
- EF Core 9 con Global Query Filters y RLS simultáneos.
- Auditoría inmutable, soft delete patterns, concurrencia optimista.
- Compliance CFDI 4.0 SAT + LFPDPPP.

Responde como DBA entregando DDL que un dev senior puede ejecutar el lunes contra un SQL Server 2022+ instance.

---

## Alcance de ESTE prompt (P3)

Entregar el **DDL SQL Server completo y ejecutable** del sistema. Este DDL es la base sobre la que P4-P7 construirán la lógica.

**SÍ incluir**:
- CREATE SCHEMA, CREATE TABLE con PKs, FKs, constraints, checks.
- Índices compuestos (siempre con `TenantId` primero en entidades tenant-scoped).
- Índices para búsqueda frecuente (por Status, por CustomerId, por Plate, etc.).
- `RowVersion` en entidades con concurrencia.
- Campos de soft delete donde aplique.
- Row-Level Security policies para tablas críticas.
- Script de seed data inicial (CatalogTypes, Plans, PlanMeters, roles base).
- Comentarios SQL donde la decisión no sea obvia.
- Migration strategy con EF Core (cómo versionar, cómo hacer rollback).

**NO incluir**:
- Código C# (solo DDL puro).
- Lógica de negocio (CHECK constraints mínimos).
- Stored procedures (preferimos lógica en capa aplicación).
- Triggers (usamos EF Core + Mediator handlers).

---

## Entidades Requeridas

### Nivel plataforma (sin `TenantId`)

1. **Tenants** — cuenta del SaaS
   - Campos mínimos: `Id` (PK), `Name`, `StripeCustomerId`, `StripeSubscriptionId`, `PlanId` (FK), `Status` (`Trial`, `Active`, `PastDue`, `Canceled`, `Suspended`), `TrialEndsAt`, `CurrentPeriodStart`, `CurrentPeriodEnd`, `BillingCycleAnchorDay`, `RFC`, `RazonSocial`, `AvisoPrivacidadAcceptedAt`, `SignupUTMSource`, `SignupUTMCampaign`, `SignupUTMMedium`, `CreatedAt`, soft delete fields, `RowVersion`.

2. **Plans** — planes de suscripción (solo 1 activo en MVP)
   - `Id`, `Name`, `BasePrice` (MXN), `AdditionalBranchPrice` (MXN), `StripeBasePriceId`, `StripeBranchPriceId`, `IsActive`, `CreatedAt`.

3. **PlanMeters** — configuración de meters del plan
   - `Id`, `PlanId` (FK), `MeterType` (`CFDI_STAMPS`, `WHATSAPP_UTILITY`, `WHATSAPP_MARKETING`, `SMS`, `AI_BASIC`, `AI_ADVANCED`, `STORAGE_GB`), `IncludedQuota` (por sucursal), `OverageUnitPriceMxn`, `StripePriceId`, `StripeMeterId`.

4. **SuperAdmins**
   - `Id`, `Email`, `FullName`, `Role` (`Founder`, `Support`, `Billing`), `IsActive`, `MfaEnabled` (siempre 1), `MfaSecret` (cifrado), `LastLoginAt`, `CreatedAt`, `RowVersion`.

5. **SystemAuditLog** — inmutable
   - `Id`, `SuperAdminId` (nullable), `Action`, `TargetType`, `TargetId`, `Payload` (JSON), `IPAddress`, `UserAgent`, `Timestamp`, `TraceId`.

6. **CatalogTypes** — seed por deploy
   - `Id`, `Code` (unique, ej: `PAYMENT_METHOD`), `Name`, `Scope` (`Tenant`, `Branch`, `System`), `IsSystem`, `MetadataSchema` (JSON), `SortMode` (`Manual`, `Alphabetical`, `Code`).

### Nivel tenant (con `TenantId`)

7. **Branches** — sucursales del tenant
   - `Id`, `TenantId`, `Name`, `Address`, `Timezone`, `RFC` (nullable — para cuando cada sucursal factura con RFC propio), `CSDCertificate` (nullable, cifrado), `Status`, `StripeConnectAccountId` (nullable, post-MVP), `MercadoPagoAccountId` (nullable, post-MVP), `CreatedAt`, soft delete, `RowVersion`.

8. **Users** — usuarios del tenant
   - `Id`, `TenantId`, `Email`, `PasswordHash`, `FullName`, `Phone`, `Status`, `MfaEnabled`, `MfaSecret` (cifrado, nullable), `LastLoginAt`, `CreatedAt`, soft delete, `RowVersion`.

9. **Roles** — roles del tenant (custom per tenant)
   - `Id`, `TenantId`, `Name`, `Permissions` (JSON), `IsSystem` (rol base del sistema).

10. **UserBranchAccess** — M:N con rol por sucursal
    - `UserId`, `BranchId`, `RoleId`, `IsActive`, `AssignedAt`, `AssignedByUserId`. PK compuesto.

11. **Customers** — clientes finales del taller (scope Tenant)
    - `Id`, `TenantId`, `Name`, `Phone`, `Email`, `RFC`, `UsoCFDI`, `RegimenFiscal`, `PreferredChannel` (`Email`, `WhatsApp`, `SMS`), `CreatedAt`, soft delete.

12. **Vehicles**
    - `Id`, `TenantId`, `CustomerId`, `Plate`, `VIN`, `Make`, `Model`, `Year`, `Color`, `CurrentOdometer`, `LastServiceAt`, `CreatedAt`, soft delete.

13. **CatalogItems** — instancias de catálogo genérico
    - `Id`, `CatalogTypeId`, `TenantId`, `BranchId` (nullable según Scope), `Code`, `Name`, `Description`, `DisplayOrder`, `ExtraData` (JSON), `IsActive`, audit + soft delete, `RowVersion`.

### Nivel branch (con `TenantId` + `BranchId`)

14. **ServiceOrders**
    - `Id`, `TenantId`, `BranchId`, `OrderNumber` (único por branch), `CustomerId`, `VehicleId`, `Status` (`Draft`, `Quoted`, `Approved`, `InProgress`, `Completed`, `Delivered`, `Canceled`), `OpenedAt`, `ClosedAt`, `AssignedAdvisorId`, `AssignedMechanicId`, `EstimatedCost`, `FinalCost`, `Notes`, `CreatedAt`, soft delete, `RowVersion`.

15. **ServiceOrderItems**
    - `Id`, `ServiceOrderId`, `Type` (`Part`, `Labor`), `PartId` (nullable), `Description`, `Quantity`, `UnitPrice`, `Total`, `CreatedAt`, soft delete.

16. **Quotations** — versionables
    - `Id`, `ServiceOrderId`, `Version`, `Content` (JSON snapshot), `SentAt`, `SentChannel`, `CustomerResponseAt`, `CustomerResponse` (`Accepted`, `Rejected`, `ChangesRequested`), `CreatedAt`.

17. **Budgets** — presupuesto aprobado (aparta inventario)
    - `Id`, `ServiceOrderId`, `QuotationId`, `ApprovedAt`, `ApprovalMethod` (`DigitalSignature`, `WhatsAppConfirmation`), `ApprovalEvidence` (JSON).

18. **Warehouses** — almacenes por sucursal
    - `Id`, `TenantId`, `BranchId`, `Name`, `IsDefault`, `CreatedAt`, soft delete.

19. **Parts** — catálogo de refacciones (scope Tenant — compartido entre sucursales)
    - `Id`, `TenantId`, `SKU` (unique por tenant), `Name`, `OEMCode`, `Brand`, `CategoryId` (FK a CatalogItems donde CatalogTypeCode='PART_CATEGORY'), `UnitOfMeasureId` (FK), `DefaultPrice`, `CompatibilityNotes`, `CreatedAt`, soft delete, `RowVersion`.

20. **StockLevels** — stock por warehouse (branch-scoped)
    - `PartId`, `WarehouseId`, `QtyOnHand`, `QtyReserved`, `MinLevel`, `MaxLevel`, `LastCountAt`, `RowVersion`. PK compuesto.

21. **InventoryMovements** — **INMUTABLE** (nunca deleted)
    - `Id`, `TenantId`, `BranchId`, `WarehouseId`, `PartId`, `Type` (`In`, `Out`, `Adjustment`, `Transfer`), `Quantity`, `Reason`, `RefType` (`Order`, `PurchaseOrder`, `Manual`), `RefId`, `UserId`, `Timestamp`.

22. **VehicleCustodies** — custodia física del auto
    - `Id`, `TenantId`, `BranchId`, `ServiceOrderId`, `VehicleId`, `EntryAt`, `EntryPhotosUrls` (JSON array), `EntryChecklist` (JSON), `EntryFuelLevel`, `EntryOdometer`, `EntrySignatureUrl`, `LocationInShop`, `ExitAt` (nullable), `ExitChecklist`, `ExitSignatureUrl`, `CreatedAt`, soft delete.

23. **CFDIs** — **nunca deleted, solo cancel**
    - `Id`, `TenantId`, `BranchId`, `ServiceOrderId`, `RFCEmisor`, `UUID` (SAT), `Serie`, `Folio`, `Total`, `XML`, `PDF` (url a blob), `Status` (`Timbrado`, `Canceled`), `TimbradoAt`, `CanceledAt`, `CancelReason` (`01`, `02`, `03`, `04`), `ReplacedByUUID` (nullable), `CreatedAt`.

24. **Payments** — **nunca deleted, solo reverse con Refund row**
    - `Id`, `TenantId`, `BranchId`, `ServiceOrderId`, `CfdiId` (nullable), `Amount`, `Method`, `GatewayProvider` (`Manual`, `StripeConnect`, `MercadoPago`), `GatewayRef` (nullable), `Status`, `PaidAt`, `CreatedAt`.

25. **Notifications**
    - `Id`, `TenantId`, `BranchId`, `CustomerId`, `Channel` (`Email`, `WhatsApp`, `SMS`, `Telegram`), `Template`, `Status`, `SentAt`, `DeliveredAt`, `FailureReason`, `CreatedAt`.

26. **AIEvents** — **inmutable**
    - `Id`, `TenantId`, `BranchId` (nullable), `UserId`, `UseCase`, `Model`, `PromptTokens`, `CompletionTokens`, `CostUsd`, `LatencyMs`, `FeedbackRating` (nullable), `CreatedAt`.

27. **OutboxMessages** — pattern outbox para sync offline
    - `Id`, `AggregateId`, `Type`, `Payload` (JSON), `Status`, `RetryCount`, `CreatedAt`, `ProcessedAt`.

28. **AuditLog** — **inmutable**
    - `Id`, `TenantId`, `BranchId` (nullable), `UserId`, `SuperAdminId` (nullable, si fue impersonation), `ImpersonationAuditId` (nullable, FK), `Action`, `EntityType`, `EntityId`, `Before` (JSON), `After` (JSON), `IPAddress`, `UserAgent`, `Timestamp`, `TraceId`.
    - Check constraint: `SuperAdminId IS NOT NULL IMPLIES ImpersonationAuditId IS NOT NULL`.

### Usage tracking (para Stripe Meters)

29. **UsageCounters** — reset cíclico
    - `Id`, `TenantId`, `BranchId` (nullable), `MeterType`, `PeriodStart`, `PeriodEnd`, `IncludedQuota`, `UsedQuantity`, `OverageQuantity`, `LastUpdatedAt`, `RowVersion`. Unique: `(TenantId, BranchId, MeterType, PeriodStart)`.

30. **MeterEvents** — **inmutable**
    - `Id`, `TenantId`, `BranchId` (nullable), `MeterType`, `Quantity`, `Unit`, `ResourceId` (ej: CfdiId, AIEventId), `UserId`, `Timestamp`, `IdempotencyKey` (unique con `MeterType`, `TenantId`), `StripeMeterEventId` (nullable), `ReportStatus` (`Pending`, `Reported`, `Failed`, `Duplicate`), `ReportAttempts`, `LastReportError`, `CreatedAt`.

31. **UsageAlerts** — tracking de thresholds
    - `Id`, `TenantId`, `MeterType`, `PeriodStart`, `ThresholdPercent` (80, 100, 120, 150, 200), `TriggeredAt`, `NotificationSent`, `NotificationChannel`. Unique: `(TenantId, MeterType, PeriodStart, ThresholdPercent)`.

32. **BranchQuantityHistory** — para auditoría de proration Stripe
    - `Id`, `TenantId`, `Quantity`, `EffectiveFrom`, `EffectiveTo` (nullable = current), `StripeSubscriptionItemId`, `ChangeReason`, `ChangedByUserId`, `CreatedAt`.

### Super-admin y operaciones (detalle completo en P4, aquí solo schema)

33. **SuperAdminSessions**
    - `Id`, `SuperAdminId`, `StartedAt`, `ExpiresAt`, `EndedAt`, `IPAddress`, `UserAgent`, `MfaVerifiedAt`, `CloudflareAccessJwtJti`, `IsImpersonating`, `ActiveImpersonationId`.

34. **ImpersonationAudits** — **inmutable**
    - `Id`, `SuperAdminSessionId`, `SuperAdminId`, `TenantId`, `AccessMode` (`ElevatedRead`, `ImpersonationReadOnly`, `ImpersonationFull`), `ImpersonatedUserId` (nullable), `Reason`, `StartedAt`, `ExpiresAt`, `EndedAt`, `EndReason`, `ActionsCount`, `WriteActionsCount`, `NotificationEmailSentAt`, `NotificationEndEmailSentAt`.

35. **ImpersonationActionLog** — detalle por acción
    - `Id`, `ImpersonationAuditId`, `Action`, `EntityType`, `EntityId`, `IsWrite`, `Timestamp`, `Details` (JSON).

36. **SupportAccessLog** — visible al tenant
    - `Id`, `TenantId`, `SuperAdminName` (snapshot), `AccessMode`, `ReasonSummary` (versión sanitizada), `StartedAt`, `EndedAt`, `ActionsPerformed`, `WriteActionsPerformed`, `EmailNotificationSentAt`, `TenantAcknowledgedAt`.

37. **AlertRules** + **AlertEvents** — configuración de alertas observabilidad.

38. **IsolationAnomalies** — detecciones del anomaly detector.

---

## Formato de Respuesta Esperado

### 1. Assumptions a validar
2-3 cosas sobre el modelo que cuestionarías antes de ejecutar.

### 2. Convenciones de DDL del proyecto
- Naming convention (PascalCase para tablas, etc.).
- Tipos: `UNIQUEIDENTIFIER DEFAULT NEWSEQUENTIALID()` para PKs, `DATETIME2(3)` para timestamps, `NVARCHAR` para texto, `DECIMAL(18,4)` para cantidades monetarias.
- Collation: `Modern_Spanish_CI_AS` o similar para soporte es-MX.
- Naming de índices, FKs, constraints.

### 3. Script completo de creación de schema

DDL ejecutable en orden correcto de dependencias. Para cada tabla:
- `CREATE TABLE` con columnas, tipos, nullability.
- PKs, FKs con `ON DELETE` / `ON UPDATE` policies apropiadas.
- CHECK constraints mínimos (ej: `Status IN (...)`, cantidades > 0, fechas coherentes).
- Unique constraints donde aplique.
- `ROWVERSION` en entidades de concurrencia.
- Comentarios `--` explicando decisiones no obvias.

Agrupa en secciones:
- 3a. Schemas y setup inicial.
- 3b. Tablas nivel plataforma.
- 3c. Tablas nivel tenant.
- 3d. Tablas nivel branch.
- 3e. Tablas de usage tracking.
- 3f. Tablas de super-admin y operaciones.
- 3g. Tablas de observabilidad.

### 4. Script completo de índices

Todos los índices con comentario de por qué existen. Incluye mínimo:
- Índices compuestos `(TenantId, <campo>)` para entidades tenant-scoped.
- Índices compuestos `(TenantId, BranchId, <campo>)` para entidades branch-scoped.
- Índice en `Email` de Users (autenticación).
- Índice en `Plate` y `VIN` de Vehicles (búsqueda rápida).
- Índice en `SKU`, `OEMCode` de Parts.
- Índice filtrado para `DeletedAt IS NULL` en entidades con soft delete.
- Índice en `ReportStatus IN ('Pending','Failed')` de MeterEvents (para job de reporter).
- Índice en `Timestamp DESC` de AuditLog, ImpersonationAudits, etc.
- Índices en `Status` donde aplique (ServiceOrders, CFDIs, Payments).

### 5. Row-Level Security policies

Script SQL con:
- Función de filtro `dbo.fn_tenantAccessPredicate(@TenantId UNIQUEIDENTIFIER)`.
- Security policies para al menos: `ServiceOrders`, `Customers`, `Vehicles`, `Parts`, `CFDIs`, `Payments`, `InventoryMovements`.
- Ejemplo de cómo el middleware setea `SESSION_CONTEXT(N'TenantId', @tenantId)` al inicio de cada request.
- Documentación de cómo bypass para jobs del sistema (SESSION_CONTEXT con role especial).

### 6. Seed data inicial

Script `INSERT` para:
- `Plans` (plan único "TallerPro Base").
- `PlanMeters` (los 7 meters con pool incluido y overage price).
- `CatalogTypes` (los 8 tipos de catálogo genérico).
- `Roles` base del sistema (Owner, Admin, Advisor, Mechanic, Warehouse, Accountant).
- `SuperAdmins` seed del founder (sin MFA secret — se genera en primer login).

### 7. EF Core Migration Strategy

Documento breve sobre:
- Cómo versionar migrations con EF Core 9.
- Convención de naming `YYYYMMDDHHMM_Description`.
- Rollback strategy (`Down()` methods mantenidos).
- Cómo manejar migrations en producción multi-tenant (downtime vs blue-green).
- Tooling: `dotnet ef migrations add`, scripts idempotentes, sqlpackage para deploy.

### 8. Script de verificación post-deploy

Query SQL que valida:
- Todas las tablas tenant-scoped tienen índice compuesto con `TenantId` primero.
- Todas las tablas tienen RLS policy activa (cuando aplique).
- Todos los `RowVersion` están presentes donde se espera.
- No hay FKs rotos.

---

## Restricciones de la Respuesta

- **DDL ejecutable, no pseudocódigo**. Debe correr contra SQL Server 2022+ sin errores.
- Comentarios SQL donde la decisión no sea obvia.
- NO incluyas stored procedures ni triggers (lógica en aplicación).
- NO incluyas código C# (otros prompts).
- Prioriza tablas con scripts completos sobre prosa.
- Longitud target: ~10,000-12,000 tokens.

---

## Al final de tu respuesta

Genera un bloque **"ADR Update — Database Schema"** con decisiones específicas que este prompt cementó:
- Tipo exacto de PKs (GUID vs bigint).
- Estrategia de `RowVersion`.
- Estrategia de soft delete.
- Convenciones de índices.
- RLS aplicada.
