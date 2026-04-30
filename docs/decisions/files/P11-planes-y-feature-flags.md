# Prompt 11 — Sistema de 5 Planes Comerciales + Feature Flags

## Contexto del Proyecto

**TallerPro**: SaaS multitenant + multitaller para talleres mecánicos en México.

**Stack**: .NET Core 9 + Blazor Hybrid + MudBlazor + Mediator (Othamar) + Mapster + EF Core + SQL Server + Stripe Billing/Meters.

**Decisiones fundacionales ya tomadas**:
- Schema DB ya existe de P3 (tablas base).
- Stripe Billing + Meters integrado (de P5).
- ITenantContext y aislamiento de P2.
- **Sin trial, sin devolución**: pay-and-use immediately.
- Upgrade: inmediato con proration. Downgrade: efectivo al siguiente ciclo.
- Este prompt expande P3 y P5 y se integra con P4 (admin portal).

---

## Modelo de 5 Planes Confirmado

### Planes estándar (self-service via Stripe Checkout)

| Plan | Code | Base MXN | Sucursal extra | Sucursales incluidas | Modo |
|------|------|----------|----------------|---------------------|------|
| Starter | `STARTER` | $399 | N/A (no escala) | 1 fija | POOL multi-tenant |
| Básico | `BASICO` | $899 | $299 | 1 | POOL multi-tenant |
| Pro | `PRO` | $1,499 | $449 | 1 | POOL multi-tenant |
| Business | `BUSINESS` | $3,499 | $699 (desde la 4ta) | 3 | POOL multi-tenant |

### Plan Enterprise (sales-led, deployment dedicado)

- **Code**: `ENTERPRISE`
- **Precio mensual**: $24,000 MXN
- **Setup fee one-time**: $49,999 MXN (incluye provisioning + migración + 40h training)
- **Contrato**: 24 meses sin aumento de precio
- **Pago**: Trimestral adelantado (factura Stripe Invoicing o SPEI)
- **Modo**: DEDICATED — VPS exclusiva por cliente (ver P12)
- **Público**: No aparece en pricing page; solo via contacto comercial
- Todo lo de Business + white-label completo + SLA 99.9% contractual + AM senior dedicado

### Matriz de Features por Plan

| Feature Code | STARTER | BASICO | PRO | BUSINESS | ENTERPRISE |
|---|:---:|:---:|:---:|:---:|:---:|
| `MULTIPLE_BRANCHES` | ❌ | ✅ | ✅ | ✅ | ✅ |
| `UNLIMITED_USERS` | ❌ (3 max) | ✅ | ✅ | ✅ | ✅ |
| `UNLIMITED_ORDERS` | ❌ (150/mes) | ✅ | ✅ | ✅ | ✅ |
| `MULTI_WAREHOUSE_BASIC` | ❌ | ✅ | ✅ | ✅ | ✅ |
| `MULTI_WAREHOUSE_ADVANCED` | ❌ | ❌ | ✅ | ✅ | ✅ |
| `CUSTOM_ROLES` | ❌ | ❌ | ✅ | ✅ | ✅ |
| `AI_BASIC` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `AI_ADVANCED_RAG` | ❌ | ✅ | ✅ | ✅ | ✅ |
| `PRIORITY_AI_QUEUE` | ❌ | ❌ | ❌ | ✅ | ✅ |
| `BI_BASIC` | ❌ | ❌ | ✅ | ✅ | ✅ |
| `BI_ADVANCED` | ❌ | ❌ | ❌ | ✅ | ✅ |
| `API_READONLY` | ❌ | ❌ | ✅ | ✅ | ✅ |
| `API_FULL` | ❌ | ❌ | ❌ | ✅ | ✅ |
| `WEBHOOKS` | ❌ | ❌ | ❌ | ✅ | ✅ |
| `MULTI_RFC_PER_BRANCH` | ❌ | ❌ | ❌ | ✅ | ✅ |
| `WHITE_LABEL_LIGHT` | ❌ | ❌ | ❌ | ✅ | ✅ |
| `WHITE_LABEL_FULL` | ❌ | ❌ | ❌ | ❌ | ✅ |
| `DEDICATED_BACKUP` | ❌ | ❌ | ❌ | ✅ | ✅ |
| `DEDICATED_DEPLOYMENT` | ❌ | ❌ | ❌ | ❌ | ✅ |
| `IMPERSONATION_SUPPORT` | ❌ | ❌ | ✅ | ✅ | ✅ |
| `SLA_99_9` | ❌ | ❌ | ❌ | ✅ | ✅ |
| `DEDICATED_AM` | ❌ | ❌ | ❌ | ❌ | ✅ |
| `CUSTOM_DOMAIN` | ❌ | ❌ | ❌ | ❌ | ✅ |

### Pools de Consumo por Plan

| Meter | STARTER | BASICO | PRO | BUSINESS | ENTERPRISE |
|---|---|---|---|---|---|
| `CFDI_STAMPS` | 50 fijo | 200/sucursal | 500/sucursal | **2,000/plan** | 3,000/plan |
| `WHATSAPP_UTILITY` | 100 fijo | 500/sucursal | 1,500/sucursal | **5,000/plan** | 10,000/plan |
| `WHATSAPP_MARKETING` | 0 | 0 | 0 | 0 | 0 |
| `SMS` | 0 | 0 | 0 | 0 | 0 |
| `AI_BASIC` | 50 fijo | 500/sucursal | 1,500/sucursal | **10,000/plan fair use** | Ilimitado |
| `AI_ADVANCED_RAG` | 0 | 50/sucursal | 200/sucursal | **500/plan** | 1,500/plan |
| `STORAGE_GB` | 2 fijo | 5/sucursal | 15/sucursal | **50/plan** | 200/plan |

**Regla crítica `POOL_SCOPE`**:
- `PER_BRANCH`: pool se multiplica por cantidad de sucursales activas (Básico, Pro).
- `PER_PLAN`: pool compartido entre todas las sucursales (Business, Enterprise).
- `FIXED`: pool único, no escala (Starter).

### Overage Pricing (universal, en MXN)

| Meter | Precio overage |
|---|---|
| `CFDI_STAMPS` | $2.50 |
| `WHATSAPP_UTILITY` | $0.50 |
| `WHATSAPP_MARKETING` | $1.80 |
| `SMS` | $0.85 |
| `AI_BASIC` | $0.30 |
| `AI_ADVANCED_RAG` | $2.00 |
| `STORAGE_GB` | $4.00 |

### Ciclo de vida

- **Sin trial**: signup → Stripe Checkout → cobro inmediato → tenant activo.
- **Sin devolución**: pago firme, sin reembolsos.
- **Upgrade**: inmediato con proration automática de Stripe.
- **Downgrade**: efectivo al siguiente ciclo (previene abuso intraciclo).
- **Enterprise**: contrato 24 meses, pago trimestral adelantado, precio congelado.

---

## Tu Rol

Actúa como **Arquitecto Senior + Product Engineer** con experiencia en:
- Feature flags en SaaS B2B multi-plan.
- Stripe Products/Prices provisioning vía API (no manual en dashboard).
- Pipeline behaviors Mediator para enforcement de policies cross-cutting.
- MudBlazor components con conditional rendering.
- Billing lifecycle: upgrade/downgrade, proration, dunning.

Responde con código C# ejecutable, migrations EF Core, Razor components, scripts de seed.

---

## Alcance de ESTE prompt (P11)

**SÍ incluir**:
1. Schema DB expandido (`Plans`, `PlanFeatures`, `PlanMeters` con `PoolScope`, `SubscriptionChangeHistory`).
2. Constantes y enums de Features.
3. Servicio `IPlanFeatureService` con enforcement.
4. Pipeline behavior `PlanFeatureEnforcementBehavior` de Mediator.
5. Atributos `[RequireFeature]` para controllers/endpoints.
6. Componente MudBlazor `<FeatureGate>` con variantes (hide/disable/upgrade-prompt).
7. Seed data completo de los 5 planes con todos sus features y pool meters.
8. Stripe provisioning script (C# console app) que crea Products/Prices/Meters idempotente.
9. Handlers Mediator para `UpgradePlanCommand`, `DowngradePlanCommand`, `AddBranchCommand`.
10. UI MudBlazor: portal del tenant con "Mi Plan" + comparativa + upgrade/downgrade.
11. Admin portal: vista `/admin/plans` con distribución, MRR, upgrade opportunities.
12. Pricing page (marketing MVC) con las 5 columnas + CTA diferenciado por plan.
13. Tests específicos de enforcement.

**NO incluir**:
- Provisioning de VPS Enterprise (eso es P12).
- Observabilidad centralizada Enterprise (P12).
- Marketing content/copy (ya existe de P7).

---

## Formato de Respuesta Esperado

### 1. Assumptions a validar

2-3 cuestionamientos al founder. Ejemplos:
- "¿Los Add-ons (CFDI Pack+, WhatsApp Marketing Pack, etc.) se gestionan con PlanFeatures o con una tabla AddOns separada?"
- "¿Custom roles en Pro/Business/Enterprise se limitan a N roles máximos o ilimitados?"
- "¿El plan Starter con `MaxOrdersPerMonth=150` qué hace cuando llega? ¿Bloquea o permite con prompt de upgrade?"

### 2. Schema DB Expandido

DDL completo para agregar/modificar:

```sql
-- Tabla Plans expandida
ALTER TABLE Plans ADD
    Code NVARCHAR(20) NOT NULL UNIQUE,
    Tier INT NOT NULL,
    AdditionalBranchPrice DECIMAL(10,2) NULL,
    IncludedBranches INT NOT NULL DEFAULT 1,
    MaxUsers INT NULL,
    MaxOrdersPerMonth INT NULL,
    IsPublic BIT NOT NULL DEFAULT 1,
    IsNegotiable BIT NOT NULL DEFAULT 0,
    RequiresContract BIT NOT NULL DEFAULT 0,
    ContractMinMonths INT NULL,
    SetupFeeMxn DECIMAL(10,2) NULL,
    DeploymentMode NVARCHAR(20) NOT NULL DEFAULT 'POOL' CHECK (DeploymentMode IN ('POOL', 'DEDICATED')),
    AnnualDiscountPercent DECIMAL(5,2) NULL,
    PriceLockedForMonths INT NULL,  -- 24 para Enterprise
    StripeBasePriceId NVARCHAR(100) NULL,
    StripeBranchPriceId NVARCHAR(100) NULL,
    StripeAnnualBasePriceId NVARCHAR(100) NULL,
    StripeSetupPriceId NVARCHAR(100) NULL;

-- Nueva tabla PlanFeatures
CREATE TABLE PlanFeatures (
    PlanId INT NOT NULL,
    FeatureCode NVARCHAR(50) NOT NULL,
    IsEnabled BIT NOT NULL DEFAULT 1,
    ConfigJson NVARCHAR(MAX) NULL,
    PRIMARY KEY (PlanId, FeatureCode),
    FOREIGN KEY (PlanId) REFERENCES Plans(Id)
);

-- PlanMeters expandida
ALTER TABLE PlanMeters ADD
    PoolScope NVARCHAR(20) NOT NULL CHECK (PoolScope IN ('FIXED', 'PER_BRANCH', 'PER_PLAN')),
    IsFairUse BIT NOT NULL DEFAULT 0,
    FairUseSoftCap INT NULL;

-- Historial de cambios de suscripción
CREATE TABLE SubscriptionChangeHistory (
    Id BIGINT IDENTITY PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    ChangeType NVARCHAR(30) NOT NULL,  -- 'Upgrade', 'Downgrade', 'AddBranch', 'RemoveBranch', 'PlanChange'
    FromPlanId INT NULL,
    ToPlanId INT NULL,
    BranchQuantityBefore INT NOT NULL,
    BranchQuantityAfter INT NOT NULL,
    EffectiveAt DATETIME2 NOT NULL,
    ProrationAmountMxn DECIMAL(10,2) NULL,
    StripeInvoiceId NVARCHAR(100) NULL,
    RequestedByUserId UNIQUEIDENTIFIER NOT NULL,
    Reason NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL
);

-- Expansión de Tenants
ALTER TABLE Tenants ADD
    ContractStartAt DATETIME2 NULL,
    ContractEndAt DATETIME2 NULL,
    ContractMonthsCommitted INT NULL,
    PriceLockedUntil DATETIME2 NULL,
    IsEnterpriseDedicated BIT NOT NULL DEFAULT 0,
    DedicatedDeploymentId UNIQUEIDENTIFIER NULL;  -- FK a EnterpriseDeployments (P12)

-- Índices nuevos
CREATE INDEX IX_Plans_Code ON Plans(Code);
CREATE INDEX IX_Plans_IsPublic ON Plans(IsPublic) WHERE IsPublic = 1;
CREATE INDEX IX_PlanFeatures_FeatureCode ON PlanFeatures(FeatureCode);
CREATE INDEX IX_SubChangeHistory_Tenant ON SubscriptionChangeHistory(TenantId, EffectiveAt DESC);
```

### 3. Constantes de Features (C#)

```csharp
namespace TallerPro.Domain.Billing;

public static class PlanCodes
{
    public const string Starter = "STARTER";
    public const string Basico = "BASICO";
    public const string Pro = "PRO";
    public const string Business = "BUSINESS";
    public const string Enterprise = "ENTERPRISE";
}

public static class Features
{
    public const string MultipleBranches = "MULTIPLE_BRANCHES";
    public const string UnlimitedUsers = "UNLIMITED_USERS";
    public const string UnlimitedOrders = "UNLIMITED_ORDERS";
    public const string MultiWarehouseBasic = "MULTI_WAREHOUSE_BASIC";
    public const string MultiWarehouseAdvanced = "MULTI_WAREHOUSE_ADVANCED";
    public const string CustomRoles = "CUSTOM_ROLES";
    public const string AiBasic = "AI_BASIC";
    public const string AiAdvancedRag = "AI_ADVANCED_RAG";
    public const string PriorityAiQueue = "PRIORITY_AI_QUEUE";
    public const string BiBasic = "BI_BASIC";
    public const string BiAdvanced = "BI_ADVANCED";
    public const string ApiReadOnly = "API_READONLY";
    public const string ApiFull = "API_FULL";
    public const string Webhooks = "WEBHOOKS";
    public const string MultiRfcPerBranch = "MULTI_RFC_PER_BRANCH";
    public const string WhiteLabelLight = "WHITE_LABEL_LIGHT";
    public const string WhiteLabelFull = "WHITE_LABEL_FULL";
    public const string DedicatedBackup = "DEDICATED_BACKUP";
    public const string DedicatedDeployment = "DEDICATED_DEPLOYMENT";
    public const string ImpersonationSupport = "IMPERSONATION_SUPPORT";
    public const string Sla999 = "SLA_99_9";
    public const string DedicatedAm = "DEDICATED_AM";
    public const string CustomDomain = "CUSTOM_DOMAIN";
}
```

### 4. Servicio `IPlanFeatureService`

Implementación completa con cache agresivo (plan changes son infrecuentes):

```csharp
public interface IPlanFeatureService
{
    Task<bool> IsEnabledAsync(Guid tenantId, string featureCode, CancellationToken ct = default);
    Task<T?> GetConfigAsync<T>(Guid tenantId, string featureCode, CancellationToken ct = default) where T : class;
    Task EnforceAsync(Guid tenantId, string featureCode, CancellationToken ct = default);
    Task<PlanInfo> GetCurrentPlanAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetEnabledFeaturesAsync(Guid tenantId, CancellationToken ct = default);
    Task InvalidateCacheAsync(Guid tenantId);
}
```

Implementación:
- Usa `ITenantScopedCache` (P2) con key `plan-features:{TenantId}`.
- TTL 1 hora (se invalida explícitamente en upgrade/downgrade).
- `EnforceAsync` lanza `PlanFeatureNotAvailableException` con mensaje accionable: "Esta función requiere plan Pro o superior. [Upgrade]".

### 5. Pipeline Behavior Mediator

```csharp
public sealed class PlanFeatureEnforcementBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IPlanFeatureService _features;
    private readonly ITenantContext _tenant;

    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken ct)
    {
        var attr = typeof(TRequest).GetCustomAttribute<RequireFeatureAttribute>();
        if (attr is not null && _tenant.CurrentTenantId is Guid tid)
        {
            await _features.EnforceAsync(tid, attr.FeatureCode, ct);
        }
        return await next(request, ct);
    }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class RequireFeatureAttribute(string featureCode) : Attribute
{
    public string FeatureCode { get; } = featureCode;
}
```

Uso en command:
```csharp
[RequireFeature(Features.ApiFull)]
public sealed record CreateWebhookCommand(string Url, string[] Events) : IRequest<Result<Webhook>>;
```

### 6. Componente MudBlazor `<FeatureGate>`

```razor
@* TallerPro.Components/Common/FeatureGate.razor *@
@inject IPlanFeatureService Features
@inject ITenantContext Tenant
@inject NavigationManager Nav

@if (_isEnabled)
{
    @ChildContent
}
else
{
    @switch (Mode)
    {
        case FeatureGateMode.Hide:
            // Nada
            break;
        case FeatureGateMode.Disable:
            <div class="feature-gated-disabled" style="opacity:0.5;pointer-events:none">
                @ChildContent
            </div>
            break;
        case FeatureGateMode.UpgradePrompt:
            <MudAlert Severity="Severity.Info" Icon="@Icons.Material.Filled.Lock">
                <MudText>@UpgradeMessage</MudText>
                <MudButton Color="Color.Primary" OnClick="NavigateToUpgrade">
                    Ver planes
                </MudButton>
            </MudAlert>
            break;
    }
}

@code {
    [Parameter, EditorRequired] public string Feature { get; set; } = "";
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public FeatureGateMode Mode { get; set; } = FeatureGateMode.Hide;
    [Parameter] public string UpgradeMessage { get; set; } = "Esta función está disponible en planes superiores.";
    
    private bool _isEnabled;
    
    protected override async Task OnInitializedAsync()
    {
        if (Tenant.CurrentTenantId is Guid tid)
        {
            _isEnabled = await Features.IsEnabledAsync(tid, Feature);
        }
    }
    
    private void NavigateToUpgrade() => Nav.NavigateTo("/settings/subscription");
}

public enum FeatureGateMode { Hide, Disable, UpgradePrompt }
```

Uso:
```razor
<FeatureGate Feature="@Features.BiAdvanced" Mode="FeatureGateMode.UpgradePrompt">
    <AdvancedBiDashboard />
</FeatureGate>

<FeatureGate Feature="@Features.CustomRoles" Mode="FeatureGateMode.Hide">
    <MudMenuItem Href="/settings/roles">Roles personalizados</MudMenuItem>
</FeatureGate>
```

### 7. Seed Data de los 5 Planes

Código C# completo del `PlanSeeder` que corre en startup si la DB está vacía:

```csharp
public sealed class PlanSeeder(ApplicationDbContext db)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await db.Plans.AnyAsync(ct)) return;

        var starter = new Plan
        {
            Code = "STARTER",
            Name = "Starter",
            Tier = 1,
            BasePrice = 399m,
            AdditionalBranchPrice = null,
            IncludedBranches = 1,
            MaxUsers = 3,
            MaxOrdersPerMonth = 150,
            IsPublic = true,
            DeploymentMode = "POOL",
            // ... resto
        };
        
        // ... Basico, Pro, Business, Enterprise
        
        db.Plans.AddRange(starter, basico, pro, business, enterprise);
        await db.SaveChangesAsync(ct);
        
        await SeedFeaturesAsync(ct);
        await SeedMetersAsync(ct);
    }
    
    // SeedFeaturesAsync: matriz completa de PlanFeatures por plan
    // SeedMetersAsync: pools + overage pricing por plan
}
```

### 8. Stripe Provisioning Script (Idempotente)

Console app `TallerPro.Tools.StripeProvisioner` que crea en Stripe:
- 1 Product por plan (excepto Enterprise — manual).
- Prices para base + sucursal extra + cada meter con graduated pricing.
- Meters de Stripe (cfdi_stamps, whatsapp_utility, etc.).
- Stripe Product IDs guardados de vuelta en tabla Plans.

Idempotencia: usa `lookup_key` en Stripe para no duplicar.

### 9. Handlers de Cambio de Plan

**`UpgradePlanCommand`**:
- Valida que el plan destino tenga Tier > actual.
- Llama Stripe `SubscriptionService.UpdateAsync` con `proration_behavior: always_invoice`.
- Registra `SubscriptionChangeHistory`.
- Invalida cache de `PlanFeatureService`.
- Envía email al tenant con summary.
- Push SignalR para actualizar UI en vivo.

**`DowngradePlanCommand`**:
- Valida que el plan destino tenga Tier < actual.
- Valida que no pierda features críticas con data activa (ej: si tiene roles custom y baja a Básico → bloquea o exige cleanup).
- Llama Stripe `SubscriptionService.UpdateAsync` con `proration_behavior: none` + `billing_cycle_anchor: unchanged`.
- Marca cambio como `effective_at = current_period_end`.
- Registra en history con `EffectiveAt` futuro.
- Job diario que aplica los downgrades pendientes al final del ciclo.

**`AddBranchCommand`** / **`RemoveBranchCommand`**:
- Solo para Básico/Pro/Business.
- Llama Stripe update item quantity con proration.
- Registra en `BranchQuantityHistory` (tabla de P5).

### 10. UI del Tenant: "Mi Plan"

Componente `SubscriptionPortal.razor` con:
- Card del plan actual + features incluidos + uso real del período.
- Comparativa visual con otros planes (botón "Comparar planes").
- Botones "Upgrade a Pro/Business" prominentes si aplica.
- Histórico de cambios de plan.
- Próximo cargo estimado con breakdown (base + sucursales + overage proyectado).
- Link a Stripe Billing Portal para método de pago.
- Advertencia clara si intenta downgrade: "Perderás acceso a: [features list]".

### 11. UI Admin Portal: `/admin/plans`

Componente `PlansManagement.razor`:
- **Distribución**: pie chart tenants por plan (MudChart).
- **MRR por plan**: bar chart.
- **Top tenants a upgrade**: tenants con >80% pool utilizado 3+ meses → candidatos upgrade.
- **Tenants a downgrade**: <20% pool utilizado 3+ meses → posible downgrade o churn risk.
- **Enterprise list**: tabla con los Enterprise activos con fecha fin de contrato (alertas 90 días antes).
- **Gestión de Enterprise**: botón "Crear Enterprise" que abre wizard de P12 (provisioning).

### 12. Pricing Page Marketing (MVC)

Vista Razor `/precios` con 5 columnas:
- Starter, Básico, Pro (destacado "Más popular"), Business.
- Enterprise con card especial "Para cadenas y franquicias" con botón "Contactar ventas" (form a `/contacto`).

Calculadora interactiva (JS vanilla) que pregunta:
- ¿Cuántas sucursales?
- ¿Cuántos CFDI al mes?
- ¿Cuántos WhatsApp?
- Recomienda plan óptimo con precio estimado mensual.

### 13. Tests Críticos

En `TallerPro.Application.Tests`:
- `FeatureGate_StarterTenant_CannotAccessRagFeature`.
- `FeatureGate_ProTenant_CanAccessBiBasic_CannotAccessBiAdvanced`.
- `UpgradePlan_WithProration_ChargesCorrectAmount`.
- `DowngradePlan_EffectiveNextCycle_DoesNotChargeImmediately`.
- `DowngradePlan_WouldLoseCustomRoles_ReturnsValidationError`.
- `StarterPlan_ExceedsMaxOrders_Returns403WithUpgradePrompt`.
- `BusinessPlan_PoolPerPlan_AllBranchesShareQuota`.
- `ProPlan_PoolPerBranch_MultipliesWithBranchCount`.

### 14. Migration Path para Tenants Actuales

Si hay tenants en alpha:
- Todos quedan automáticamente en plan "Pro" (el plan default actual).
- Sin cambio de precio en primer ciclo.
- Opción de migrar a Starter/Básico si su uso justifica (notificación proactiva del CS).

---

## Restricciones de la Respuesta

- Código C# ejecutable con referencias NuGet.
- .NET 9 idioms: primary constructors, file-scoped namespaces, `ValueTask<T>`.
- Convenciones: Mediator (Othamar), Mapster, Serilog, FluentValidation, MudBlazor.
- Longitud target: ~13,000-15,000 tokens.

---

## Al final de tu respuesta

Genera **"ADR Update — Plans & Feature Flags"** con decisiones cementadas:
- Los 5 planes con pricing final.
- Estrategia de cache.
- Policy de downgrade.
- Seed strategy.
- Stripe provisioning approach.
