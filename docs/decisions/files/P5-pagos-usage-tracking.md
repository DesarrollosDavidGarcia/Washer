# Prompt 5 de 7 — Pagos Stripe Billing + Usage Tracking + Unit Economics

## Contexto del Proyecto

**TallerPro**: SaaS multitenant + multitaller para talleres mecánicos en México.

**Stack**: .NET Core 9 + Blazor Hybrid (MAUI) + MudBlazor + Mediator (Othamar) + Mapster + EF Core + SQL Server + Stripe.net + SignalR.

**Modelo comercial (decisión fundacional ya tomada)**:
- Plan único con pricing híbrido:
  - **Base**: $899 MXN/mes por taller (incluye 1 sucursal, usuarios ilimitados, todas las features core, pools iniciales).
  - **Sucursal adicional**: +$449 MXN/mes cada una (con pool proporcional).
  - **Overage metered** sobre pools (pools se multiplican por # sucursales activas).
- **Pools incluidos por sucursal**:
  - CFDI: 500 timbres
  - WhatsApp utility: 1,000 mensajes
  - AI Basic: 1,000 órdenes
  - AI Advanced (RAG): 100 consultas
  - Storage: 10 GB
  - WhatsApp Marketing: 0 (siempre overage)
  - SMS: 0 (siempre overage)
- **Overage pricing** (MXN):
  - CFDI: $2/timbre
  - WhatsApp utility: $0.40/mensaje
  - WhatsApp marketing: $1.50/mensaje
  - SMS: $0.70/SMS
  - AI Basic: $0.30/orden
  - AI Advanced: $2.00/consulta
  - Storage: $4.00/GB
- **Usuarios ilimitados** (no se cobra per-seat).
- **Trial 14 días** (método de pago capturado pero no cobrado).

**Otros decisiones ya tomadas**:
- Stripe Billing con **Stripe Meters** para metered pricing (feature 2024+).
- Gestión dinámica de sucursales con **proration automática** (Stripe gestiona).
- Reset mensual de contadores **alineado con ciclo de facturación Stripe** (no calendario).
- Idempotencia crítica en meter reporting: `IdempotencyKey = {MeterType}:{ResourceId}`.
- Webhooks Stripe con validación de firma + tabla de `StripeEventsProcessed` para dedup.
- Dunning: 3 reintentos automáticos + estado `PastDue` → suspensión suave día 15.
- Schema DDL ya existe (P3): `UsageCounters`, `MeterEvents`, `UsageAlerts`, `BranchQuantityHistory`, `Plans`, `PlanMeters`.
- Nivel 2 (pagos taller ↔ cliente final vía Stripe Connect o Mercado Pago) es **post-MVP** pero arquitectura preparada (campos nullable en `Branches`, enum `GatewayProvider` en `Payments`).
- Aislamiento tenant es invariante (código de P2 ya existe).

---

## Tu Rol

Actúa como **Arquitecto de Software + FinOps Senior** con experiencia comprobada en:
- Stripe Billing + Stripe Meters en producción (feature 2024+).
- Usage-based pricing hybrid models (base + seat + usage).
- Idempotencia en sistemas distribuidos para metered billing.
- Unit economics de SaaS B2B con múltiples variables (CFDI, IA, comms).
- CFDI 4.0 MX con retenciones y complemento de pagos.
- Polly para resilience en integraciones financieras.

Responde con código C# ejecutable, cálculos financieros con números concretos, y decisiones de pricing defendibles.

---

## Alcance de ESTE prompt (P5)

Entregar el **sistema completo de pagos, usage tracking y dashboards financieros** del SaaS.

**SÍ incluir**:
1. Arquitectura completa de `UsageTracker` + `UsageEventHandler` + `StripeMeterReporter`.
2. Código C# de integración con Stripe Billing + Meters.
3. Handlers Mediator para sincronización tenant ↔ Stripe subscription.
4. Webhook handler completo con validación de firma + idempotencia.
5. `SubscriptionService` para add/remove branches con proration.
6. Dashboard de consumo del tenant (MudBlazor con MudChart).
7. Portal de subscription del tenant (ver plan, cambiar sucursales).
8. Super-admin dashboard financiero (MRR, overage, top tenants).
9. Pricing validado con números + unit economics 3 escenarios.
10. KPIs financieros desde día 1.

**NO incluir**:
- DDL de tablas (P3 ya lo entregó).
- Integración IA con Novita (P6 — aunque aquí se refiere al `AIUseCase` meter).
- Marketing pricing page (P7).
- Auth / impersonation (P4).

---

## Formato de Respuesta Esperado

### 1. Assumptions a validar
2-3 cosas sobre el modelo de pricing que cuestionarías al founder.

### 2. Mapeo Modelo de Pricing → Entidades Stripe

Tabla exacta de cómo se representa el modelo en Stripe:

| Concepto | Entidad Stripe | Configuración |
|---|---|---|
| Plan base $899 MXN | Price `recurring`, monthly | ... |
| Sucursal adicional $449 MXN | Price `recurring` `graduated` (tier 1: 0-1 @ $0; tier 2+: $449) | ... |
| Pool CFDI | Meter `cfdi_stamps` + Price graduated (0-500 @ $0; 501+ @ $2) | ... |
| Pool WhatsApp utility | Meter `whatsapp_utility` + Price graduated | ... |
| ... (todos los 7 meters) | ... | ... |

**Decisión crítica**: ¿el pool se multiplica automáticamente por cantidad de sucursales en Stripe o en tu lógica local?
- Opción A: Stripe tiene solo 500 free tier, tu backend calcula y reporta 1500 real cuando hay 3 sucursales.
- Opción B: Stripe tiene 500 × quantity = funcionalidad nativa de Stripe (verifica si existe en 2026).
- Justifica decisión.

### 3. Código del `UsageTracker` Service

Interfaz + implementación completa:

```csharp
public interface IUsageTracker
{
    Task RecordCfdiStampAsync(Guid cfdiId, CancellationToken ct = default);
    Task RecordWhatsAppMessageAsync(Guid messageId, WhatsAppMessageType type, CancellationToken ct = default);
    Task RecordSmsAsync(Guid smsId, CancellationToken ct = default);
    Task RecordAIRequestAsync(Guid aiEventId, AIUseCase useCase, long tokensIn, long tokensOut, CancellationToken ct = default);
    Task RecordStorageDeltaAsync(Guid branchId, decimal gbDelta, CancellationToken ct = default);
}
```

Implementación que:
- Resuelve `TenantId` y `BranchId` del contexto actual.
- Crea entrada en `MeterEvents` con `Status=Pending` + `IdempotencyKey`.
- Actualiza `UsageCounters` (atomic increment via SQL).
- Evalúa thresholds 80%, 100%, 120% y crea `UsageAlert` + push SignalR.
- Publica evento `UsageEventRecorded` via Mediator para procesamiento async.
- Logs estructurados Serilog.

### 4. Código del `UsageEventHandler`

Notification handler de Mediator que:
- Recibe `UsageEventRecorded`.
- Push SignalR `UsageHub.SendAsync("UsageUpdated", ...)` al tenant activo.
- Si cruzó threshold, envía email via Brevo al admin del tenant.
- Error handling + retry con Polly.

### 5. Código del `StripeMeterReporter` Background Service

`IHostedService` que:
- Corre cada 60 segundos (configurable).
- Lee batch de 100 `MeterEvents` con `Status IN ('Pending', 'Failed')` ordenados por `CreatedAt`.
- Por cada uno: llama `MeterEventService.CreateAsync(...)` con `IdempotencyKey`.
- Actualiza a `Status='Reported'` con `StripeMeterEventId`.
- Retry con backoff exponencial: 1m, 5m, 15m, 1h, 6h, 24h. Max 10 attempts.
- Después de max attempts → `Status='Failed'` + alerta crítica Slack #alerts.
- Métricas: throughput, latencia, error rate.

Código C# completo con uso de `Stripe.Billing.MeterEventService`.

### 6. Webhook Handler Completo de Stripe

Controller `WebhooksController` (en `TallerPro.Web`, marketing) con:
- Endpoint `POST /api/webhooks/stripe`.
- Validación de firma con `EventUtility.ConstructEvent(...)` usando secret de Stripe.
- Idempotencia: tabla `StripeEventsProcessed` con `EventId` PK; si existe, retorna 200 OK sin procesar.
- Dispatch a handlers Mediator según `event.type`:
  - `customer.subscription.created/updated/deleted` → `StripeSubscriptionSyncCommand`.
  - `invoice.created` → `StripeInvoiceSnapshotCommand`.
  - `invoice.payment_succeeded` → `StripePaymentConfirmedCommand`.
  - `invoice.payment_failed` → `StripeDunningCommand`.
  - `billing_meter_event_summary.created` → `StripeMeterReconciliationCommand`.
- Error handling: si handler lanza, retornar 500 para que Stripe reintente.
- Logs estructurados con `EventId`, `Type`, `TenantId` (cuando se puede resolver).
- Tests: simular eventos de Stripe y verificar dispatch correcto.

### 7. `SubscriptionService` para Gestión de Sucursales

Código C# completo:

```csharp
public class SubscriptionService
{
    public async Task<BranchChangePreview> PreviewBranchAddAsync(Guid tenantId, CancellationToken ct);
    public async Task<BranchChangeResult> AddBranchAsync(Guid tenantId, Guid newBranchId, CancellationToken ct);
    public async Task<BranchChangePreview> PreviewBranchRemoveAsync(Guid tenantId, Guid branchId, CancellationToken ct);
    public async Task<BranchChangeResult> RemoveBranchAsync(Guid tenantId, Guid branchId, string reason, CancellationToken ct);
}
```

Implementación que:
- Llama `SubscriptionService.UpdateAsync(...)` con nueva `quantity` del Branch item + `proration_behavior: create_prorations`.
- Registra en `BranchQuantityHistory` antes de la llamada a Stripe (transaccional).
- Si Stripe falla, rollback local.
- `PreviewBranchAddAsync` calcula el cargo prorated para mostrar al usuario antes de confirmar.
- Idempotency via idempotency key a Stripe.

### 8. Trial 14 Días

Código de:
- Creación de subscription con `trial_period_days: 14`.
- Cambio de estado `Tenant.Status` en webhooks: `trial` → `active` al cobrar exitoso.
- UI warning durante trial mostrando "Te quedan X días de prueba".
- Handling de dunning si al día 14 la tarjeta falla (retención antes de suspender).

### 9. Reset Mensual Alineado con Ciclo Stripe

Background job `BillingCycleSyncJob`:
- Corre diariamente a las 3 AM MX.
- Detecta tenants cuyo `current_period_start` cambió vs último snapshot.
- Crea nuevos `UsageCounters` para el nuevo período con:
  - `IncludedQuota = PlanMeter.IncludedQuota × ActiveBranchesCount`
  - `PeriodStart, PeriodEnd` de Stripe subscription.
  - `UsedQuantity = 0`, `OverageQuantity = 0`.
- Marca los `UsageCounters` del período anterior como histórico (inmutable, consultable).
- Push SignalR a tenants afectados para refresh de dashboard.

Código completo.

### 10. Dashboard de Consumo del Tenant (MudBlazor)

Componente Razor completo `UsageDashboard.razor`:
- Cards por meter con pool usado/restante (ej: "CFDI 342/500 (68%) • 18 días restantes"):
  - Progreso `MudProgressLinear` con color dinámico (verde < 80%, amarillo < 100%, naranja < 120%, rojo > 120%).
  - Proyección a fin de mes basada en pace (simple extrapolation).
- Gráfica semanal/diaria de consumo por meter con `MudChart`.
- Alert banners cuando hay meters en overage.
- Link a "Ver detalle" por meter → tabla con últimos `MeterEvents`.
- Real-time vía `UsageHub` (SignalR).
- Responsive para desktop y tablet.

Include C# code-behind con `[Inject]` de servicios, subscripción SignalR, state management.

### 11. Portal de Subscription del Tenant (MudBlazor)

Componente `SubscriptionPortal.razor`:
- Vista general: plan actual, sucursales activas, próximo cargo estimado, método de pago.
- "Próximo cargo estimado" = plan base + N sucursales + overage del período actual.
- Botón "Agregar sucursal" → modal con preview de proration + confirmación.
- Botón "Desactivar sucursal" → modal con credit prorated + confirmación.
- Tabla de historial de invoices con link al PDF de Stripe.
- Botón "Actualizar método de pago" → redirige a Stripe Billing Portal embebido (customer portal session).
- Botón "Cancelar suscripción" → flujo de retención + confirmación doble.

### 12. Super-Admin Dashboard Financiero

Componente `FinancialDashboard.razor` en `TallerPro.Admin`:

Vista top-level:
- MRR actual + gráfica histórica 12 meses.
- ARR projection.
- Overage revenue del mes (crecimiento vs mes anterior).
- # tenants activos por estado (Trial/Active/PastDue/Canceled).
- Churn mensual %.
- ARPA (Average Revenue Per Account).
- Revenue mix: % base / % sucursales / % overage.

Tabla "Top 10 tenants por revenue":
- Nombre, plan, # sucursales, MRR base, overage mes, total mes, costo variable estimado, margen bruto.
- Sort + filter.

Tabla "Tenants con issues":
- PastDue, failed payments, overage > 200% pool.
- Action buttons rápidos: enviar recordatorio, revisar invoice, llamar a tenant.

Link a unit economics individual por tenant (drill-down).

### 13. Pricing Validado con Números

Ejecuta los cálculos y valida que el modelo sostiene margen bruto ≥ 60%:

**Tarifas Stripe por país**:
- MX tarjeta nacional: 3.6% + $3 MXN por transacción.
- OXXO Pay: 3.6% + $10 MXN.
- SPEI: 1.4% + $5 MXN.

**Costo variable por pool consumido (nuestro costo, no el del tenant)**:
- CFDI: SW Sapien ~$1.50 MXN/timbre a volumen.
- WhatsApp utility: Meta Cloud API ~$0.02 USD ≈ $0.35 MXN/mensaje.
- AI Basic (DeepSeek V3.2 via Novita): ~$0.02 MXN/orden típica.
- AI Advanced (DeepSeek + RAG): ~$0.20 MXN/consulta.
- SMS: Labsmobile ~$0.35 MXN/SMS.
- Storage R2: ~$0.30 MXN/GB/mes.

Calcula margen bruto del overage:
| Meter | Precio cobrado | Costo variable | Margen bruto |
|---|---|---|---|
| CFDI | $2.00 | $1.50 | 25% ⚠️ |
| WhatsApp utility | $0.40 | $0.35 | 12.5% ⚠️ |
| AI Basic | $0.30 | $0.02 | 93% ✅ |
| ... | | | |

**Cuestiona si el pricing necesita ajuste**: si margen de CFDI es 25% y de WhatsApp 12.5%, ¿estos overage realmente cubren los costos + Stripe fees + overhead? Propón ajuste si es necesario.

### 14. Unit Economics — 3 Escenarios

Calcula para cada escenario: revenue, costo variable, Stripe fees, margen bruto, contribución a overhead.

**Escenario A: Taller pequeño típico**
- 1 sucursal, 200 órdenes/mes, 70% facturadas, 2 WhatsApp por orden, IA en 50% de órdenes.
- Verifica que NO entra en overage.
- Revenue: $899. Costo variable: $X. Margen: Y%.

**Escenario B: Cadena media**
- 3 sucursales, 800 órdenes/mes total (267/sucursal), 70% facturadas, IA en 80% (20% RAG), 2 WhatsApp/orden.
- Calcula pools vs consumo real.
- Revenue: $899 + 2×$449 + overage. Margen: Y%.

**Escenario C: Cadena grande con overage significativo**
- 5 sucursales, 3,000 órdenes/mes, 80% facturadas, IA intensiva, WhatsApp marketing activo, SMS activo.
- Excede pools en múltiples meters.
- Revenue total con overage: $X. Costo variable: $Y. Margen: Z%.

**Conclusión**: ¿el pricing sostiene margen bruto ≥ 60% en promedio? Si no, propón ajustes:
- Subir precio base.
- Reducir pools incluidos.
- Subir tarifas overage.
- Incluir "plan Enterprise" con descuento base pero overage fijo alto.

### 15. KPIs Financieros desde el Día 1

Tabla de 12 KPIs con:
- Nombre, fórmula técnica, fuente de datos (tabla), frecuencia de cálculo, alert threshold, responsable.

Incluye:
- MRR, ARR, overage revenue mensual, ARPA, % tenants con overage, revenue mix, margen bruto por tenant, burn rate, CAC/LTV (post-beta), churn mensual, meter reconciliation error, threshold alerts fired %.

Código SQL de algunos KPIs clave para que el dashboard sea ejecutable desde día 1.

### 16. Tests Críticos

Tests en `TallerPro.Integration.Tests`:
- `UsageTracker_RecordCfdi_IncrementsCounterAndCreatesMeterEvent`.
- `UsageTracker_DuplicateEventWithSameIdempotencyKey_OnlyRecordedOnce`.
- `StripeMeterReporter_BatchReport_MarksAllReported`.
- `StripeMeterReporter_FailedReport_RetriesWithBackoff`.
- `SubscriptionService_AddBranch_CreatesProrationCharge`.
- `Webhook_DuplicateEventId_ReturnsOkWithoutProcessing`.
- `BillingCycleSync_NewPeriod_CreatesFreshUsageCounters`.
- `UsageCounter_OverPool_TriggersAlertAt80_100_120_Percent`.

Código completo de cada test con Testcontainers (SQL Server + Redis) + WireMock (Stripe API mock).

---

## Restricciones de la Respuesta

- **Código C# ejecutable con referencias NuGet correctas**.
- Usa .NET 9 idioms.
- Convenciones: Mediator (Othamar), Mapster, Serilog, FluentValidation, Stripe.net.
- Prioriza código + cálculos numéricos sobre prosa.
- Los cálculos financieros deben usar números concretos, no rangos.
- Longitud target: ~13,000-15,000 tokens de respuesta.

---

## Al final de tu respuesta

Genera **"ADR Update — Billing & Unit Economics"** con decisiones cementadas (incluyendo si el pricing se mantiene o se propone ajuste concreto).
