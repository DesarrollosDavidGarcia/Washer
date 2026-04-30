# Prompt 4 de 7 — Auth + Impersonation + Super-Admin Portal

## Contexto del Proyecto

**TallerPro**: SaaS multitenant + multitaller para talleres mecánicos en México.

**Stack**: .NET Core 9 + Blazor Hybrid (MAUI Windows + Android) + MudBlazor + Mediator (Othamar) + EF Core + SQL Server + SignalR + Cloudflare Access.

**Decisiones fundacionales ya tomadas**:
- Auth: ASP.NET Core Identity + Duende IdentityServer (o Microsoft Entra External ID).
- Multi-taller con `UserBranchAccess` (M:N con rol por sucursal).
- Super-admin: founder tiene acceso total; equipo futuro solo read-only.
- Impersonation con email al tenant en CADA acceso (transparencia total).
- Portal super-admin separado: `admin.tallerpro.mx` detrás de Cloudflare Access (Zero Trust).
- Portal admin = ASP.NET Core MVC + MudBlazor (Blazor Server components).
- Schema DDL ya existe (de P3): `SuperAdmins`, `SuperAdminSessions`, `ImpersonationAudits`, `ImpersonationActionLog`, `SupportAccessLog`, `UserBranchAccess`.
- Aislamiento tenant es invariante del sistema (P2 ya entregó código de `ITenantContext`, middleware, Roslyn analyzer).
- Cero hard deletes; `ImpersonationAudits` y `SupportAccessLog` son inmutables.

**Reglas de impersonation**:
- Razón obligatoria (> 20 chars).
- TTL 60 min por sesión, extensible con nueva razón.
- Banner rojo permanente durante sesión.
- MFA re-verify al iniciar.
- Acciones destructivas deshabilitadas incluso para founder (cancelar suscripción, borrar tenant, modificar CFDI timbrados).
- Double-confirm en write actions durante impersonation Full.
- Tenant puede terminar sesión en tiempo real desde su portal.
- Email inmediato al tenant al iniciar Y al finalizar sesión.

---

## Tu Rol

Actúa como **Arquitecto de Software Senior** especializado en:
- Auth/AuthZ patterns en .NET 9 multitenant.
- Impersonation patterns seguros en SaaS B2B (Stripe, Linear, Intercom style).
- Blazor Server + MudBlazor para portales admin.
- Cloudflare Zero Trust + MFA + SSO.
- Cumplimiento LFPDPPP con audit trails inmutables.

Responde con código C# ejecutable, componentes MudBlazor funcionales, y flujos claros.

---

## Alcance de ESTE prompt (P4)

Entregar el sistema completo de autenticación, autorización multi-taller, impersonation governance y portal de super-admin.

**SÍ incluir**:
- Auth setup completo (login, refresh, MFA, Cloudflare Access).
- Modelo de autorización multi-taller (ITenantContext + IBranchContext + IImpersonationContext).
- Matriz de permisos roles × recursos × modo (normal/impersonation).
- Flujo completo de impersonation (backend + banner MudBlazor + emails).
- Estructura y vistas principales del portal `TallerPro.Admin`.
- Componente MudBlazor de "Support Access Log" para el tenant (en Hybrid).
- Middleware de autorización durante impersonation.
- Email templates (Brevo) de notificación al tenant.

**NO incluir**:
- DDL de tablas (P3 ya lo entregó).
- Código Roslyn analyzer (P2).
- Lógica de pagos (P5).
- Lógica de IA (P6).
- Marketing site o signup flow (P7).

---

## Formato de Respuesta Esperado

### 1. Assumptions a validar
2-3 cosas sobre el modelo auth que cuestionarías.

### 2. Decisión de Identity Provider

Elige definitivo entre:
- ASP.NET Core Identity + Duende IdentityServer (self-hosted, flexible, paga para Duende en producción).
- Microsoft Entra External ID (antes Azure AD B2C — managed, MFA fácil, costo por MAU).
- Auth0 (managed, excelente DX, caro a escala).

Justifica con matriz de costo a 10, 100, 1000 tenants. Recomendación clara.

### 3. Configuración de Authentication

Código C# completo de `Program.cs` en `TallerPro.Api`:
- `AddAuthentication().AddJwtBearer(...)`.
- Claims obligatorios: `sub`, `tenant_id`, `role`, `iat`, `exp`, `mfa_verified`.
- Claims de impersonation (cuando aplique): `impersonation.tenant_id`, `impersonation.user_id`, `impersonation.mode`, `impersonation.expires_at`, `impersonation.audit_id`.
- Key rotation strategy con Key Vault.
- Access token 15 min + refresh token 30 días con rotación.

### 4. Modelo de Autorización Multi-Taller

Código C# de:

**Interfaces**:
- `ITenantContext` (ya declarada en P2, aquí implementar completa).
- `IBranchContext` con `CurrentBranchId`, `AccessibleBranchIds` para el usuario actual.
- `IImpersonationContext` con `IsImpersonating`, `OriginalSuperAdminId`, `ImpersonatedTenantId`, `ImpersonatedUserId`, `Mode` (enum), `ExpiresAt`, `AuditId`.

**Middleware**:
- `TenantBranchContextMiddleware` que pobla los 3 contextos desde JWT.
- Orden en pipeline: después de auth, antes de MVC/endpoints.

**Authorization Policies**:
- Policy `AuthenticatedWithTenant` — default fallback policy.
- Policy `RequireBranchAccess` — valida que el usuario tenga acceso a la branch del recurso.
- Policy `AdminOnly` — solo SuperAdmins.
- Policy `FounderOnly` — solo el role Founder.
- Policy `NotDuringReadOnlyImpersonation` — deniega si `IImpersonationContext.Mode == ReadOnly`.

**Authorization Handlers** con código:
- `BranchAccessHandler : IAuthorizationHandler` que verifica `UserBranchAccess` tabla.
- `ImpersonationWriteHandler` que bloquea write actions si mode=ReadOnly.

### 5. Matriz de Permisos (Roles × Recursos × Modo)

Tabla completa que cruza:
- **Roles de tenant**: Owner, Admin, Advisor, Mechanic, Warehouse, Accountant.
- **Roles super-admin**: Founder, Support (futuro), Billing (futuro).
- **Recursos**: ServiceOrder, Customer, Vehicle, Part, Inventory, CFDI, Payment, User, Branch, Settings, AuditLog, UsageData, AdminOperations.
- **Modos**: Normal (user), ElevatedRead (admin), ImpersonationReadOnly, ImpersonationFull.
- **Acciones**: Read, Create, Update, SoftDelete, Export.

Matriz con ✅/❌/⚠️(con confirmación) para cada combinación.

### 6. Flujo Completo de Impersonation

**Diagrama de secuencia en ASCII** del flujo completo:
```
Founder → Admin Portal → Click "Impersonar usuario Juan de Tenant ABC"
   ↓
Modal (razón, modo, TTL, MFA re-verify)
   ↓
POST /admin/impersonate/{tenantId}
   ↓
Backend validations + crea SuperAdminSession + ImpersonationAudit + SupportAccessLog
   ↓
Envía email al Dueño/Admin vía Brevo
   ↓
Push SignalR SupportAccessHub al cliente Hybrid (banner in-app)
   ↓
Slack #admin-audit
   ↓
Retorna nuevo JWT con claims impersonation
   ↓
UI redirige a vista del tenant con banner rojo permanente
   ↓
[... interacciones del admin ...]
   ↓
Salir (manual o TTL expira) → actualiza audits → email "sesión terminada" → UI redirige
```

### 7. Código C# del Flujo de Impersonation

**Handlers Mediator**:
- `StartImpersonationCommand` + handler (validación, MFA re-verify, creación de audits, generación de JWT impersonation, envío de email/Slack/SignalR).
- `EndImpersonationCommand` + handler.
- `ExtendImpersonationCommand` + handler.

**Services**:
- `IImpersonationService` con métodos públicos.
- `ImpersonationJwtGenerator` que crea tokens con claims especiales.
- `ISupportAccessNotifier` que envía emails via Brevo.

**Controllers** (Minimal APIs):
- `POST /admin/impersonate/{tenantId}`.
- `POST /admin/impersonation/end`.
- `POST /admin/impersonation/extend`.
- `GET /admin/impersonation/active` (listar sesiones activas).
- `POST /admin/impersonation/{id}/revoke` (founder mata sesión de otro).
- `POST /api/support-access-log/{id}/terminate` (tenant termina sesión activa).

### 8. Banner MudBlazor Durante Impersonation

Componente Razor completo `ImpersonationBanner.razor`:
- Detecta si `IImpersonationContext.IsImpersonating == true`.
- Muestra banner rojo fijo en top con:
  - "⚠️ Estás viendo la cuenta de [TENANT NAME] como [USER NAME]"
  - Modo: Read-Only o Full (color codificado).
  - Tiempo restante (cuenta regresiva).
  - Botón [Salir].
  - Botón [Extender +15 min] si TTL < 10 min.
- Incluye MudBlazor styling consistente con el theme.

**Doble confirmación en write actions** (durante Impersonation Full):
- Componente `ImpersonatedActionConfirmDialog.razor` que intercepta botones de acción y pide confirmación adicional con descripción de la acción.

### 9. Estructura y Vistas del Portal `TallerPro.Admin`

Árbol de rutas + Razor Pages + controllers:

```
TallerPro.Admin/
├── Areas/
│   ├── Operations/          (dashboard operativo, alertas, sesiones activas)
│   ├── Tenants/             (lista, drill-down por tenant)
│   ├── Billing/             (MRR, overage, unit economics)
│   ├── Logs/                (explorador Seq embebido o custom)
│   └── Audit/               (explorador ImpersonationAudits + AuditLog)
├── Components/
│   ├── Dashboards/
│   ├── Impersonation/
│   └── Shared/
└── Program.cs
```

Para cada vista principal, entrega:
- **`/` (Operations Dashboard)**: mockup Razor + componentes MudBlazor. Muestra errores últimas 24h con `MudChart`, top 5 tenants por volumen de errores, health de Stripe Meters, sesiones de impersonation activas (tabla), alertas activas.
- **`/tenants/{id}` (Drill-down tenant)**: health, billing, usage, logs filtrados, audit, botones "Ver como admin (Elevated Read)" y "Impersonar usuario…".
- **`/tenants/{id}/impersonate` (Modal de inicio)**: form con razón, modo, TTL, confirmación + MFA re-verify.
- **`/sessions` (Sesiones activas)**: tabla con todas las sesiones, filtros, botón "Terminate all".

### 10. Cloudflare Access (Zero Trust) para admin.tallerpro.mx

Documentación paso a paso de setup:
- Crear Cloudflare Zero Trust team.
- Agregar `admin.tallerpro.mx` como aplicación.
- Configurar Access policy: SSO con Google Workspace + allowlist de emails específicos (no dominio completo).
- Forzar MFA en Google Workspace.
- Session TTL Cloudflare: 8 horas.
- Captura del `CF-Access-Jwt-Assertion` header en backend para correlación con `SuperAdminSessions.CloudflareAccessJwtJti`.
- Código del middleware que valida el Cloudflare Access JWT además del Identity JWT.
- Costo: $0 en free tier hasta 50 usuarios.

### 11. Email Templates — Notificación al Tenant

**Template "Acceso iniciado"** (formato Brevo template):
- Asunto, cuerpo HTML con variables `{{ params.adminName }}`, `{{ params.reason }}`, `{{ params.mode }}`, `{{ params.ttlMinutes }}`, `{{ params.supportLogUrl }}`.
- Texto con contacto de seguridad + referencia al aviso de privacidad.

**Template "Acceso finalizado"** con resumen de la sesión (duración, acciones realizadas, write actions).

**Código C#** del `ISupportAccessNotifier`:
- Integración con Brevo API v3 (paquete NuGet `sib_api_v3_sdk`).
- Queue message via Hangfire para enviar async sin bloquear la creación de la sesión.
- Idempotency: si email falla, reintentar 3 veces con backoff.
- Log estructurado via Serilog de cada notificación.

### 12. Componente Support Access Log en Hybrid (tenant lo ve)

Ubicación: menú del cliente Hybrid → Seguridad → Accesos de soporte.

Componentes MudBlazor:
- `SupportAccessActiveBanner.razor` — muestra banner in-app si hay sesión activa en su tenant. Real-time via SignalR `SupportAccessHub`. Botón "Terminar acceso" funcional.
- `SupportAccessLogTable.razor` — tabla histórica con `MudDataGrid`. Filtros por fecha, admin, modo. Drill-down a detalle.
- `SupportAccessDetailDialog.razor` — muestra detalle de una sesión con lista de acciones realizadas (de `ImpersonationActionLog`).

Solo visible para rol Owner/Admin del tenant (enforcement con policy `RequireTenantAdmin`).

### 13. Endpoints de API para el cliente Hybrid

Nuevos endpoints en `TallerPro.Api`:
- `GET /api/support-access-log` — tenant ve su propio log.
- `GET /api/support-access-log/active` — sesiones activas ahora mismo.
- `POST /api/support-access-log/{id}/terminate` — tenant termina sesión activa.
- `POST /api/support-access-log/{id}/acknowledge` — marca como visto.

### 14. SignalR Hubs relacionados

**`AdminHub`** (solo autenticado SuperAdmin):
- `OnNewAlert(AlertPayload)` — push al dashboard de operations.
- `OnImpersonationStarted/Ended(SessionInfo)` — push a sesiones activas.

**`SupportAccessHub`** (tenant):
- `OnSupportAccessStarted(AccessInfo)` — trigger banner en cliente Hybrid.
- `OnSupportAccessEnded(AccessInfo)` — oculta banner.

Código completo de los hubs + clientes JS/C# para el cliente Hybrid.

### 15. Aviso de Privacidad — cláusula de acceso de soporte

Texto exacto (ya validado con abogado) para la sección del aviso de privacidad que declara:
- Que el equipo de soporte puede acceder.
- Condiciones (notificación por email, log visible al tenant, duración máxima 60 min, motivo justificado, auditoría inmutable).
- Cómo el tenant puede ejercer ARCO.
- Contacto para reportes de accesos sospechosos.

### 16. Test Cases Específicos

Tests en `TallerPro.Isolation.Tests` específicos para impersonation:
- `Impersonation_SuperAdminImpersonatesA_CannotReadB`.
- `Impersonation_ReadOnlyMode_WriteActionReturns403`.
- `Impersonation_FullMode_Founder_CanWrite`.
- `Impersonation_FullMode_SupportRole_CannotWrite`.
- `Impersonation_ExpiredSession_ReturnsTo401`.
- `Impersonation_TenantTerminates_SessionKilledImmediately`.
- `Impersonation_ProhibitedAction_CannotCancelSubscription`.

Código completo de cada test.

---

## Restricciones de la Respuesta

- **Código C# ejecutable y componentes Razor compilables**.
- Usa .NET 9 idioms: primary constructors, file-scoped namespaces, `ValueTask<T>`.
- Convenciones: Mediator (Othamar), Mapster, Serilog, FluentValidation, MudBlazor.
- Prioriza código + diagramas sobre prosa.
- Cada componente MudBlazor debe ser funcional (no pseudocódigo).
- Longitud target: ~12,000-14,000 tokens de respuesta.

---

## Al final de tu respuesta

Genera **"ADR Update — Auth & Super-Admin Operations"** con decisiones específicas cementadas.
