# Prompt 7 de 7 — Marketing + Onboarding + Integraciones + Deployment

## Contexto del Proyecto

**TallerPro**: SaaS multitenant + multitaller para talleres mecánicos en México.

**Stack producto (ya resuelto en P1-P6)**: .NET Core 9 + Blazor Hybrid (MAUI) + MudBlazor + Mediator (Othamar) + Mapster + EF Core + SQL Server + SignalR + Stripe Billing/Meters + DeepSeek vía Novita + Serilog/Seq.

**Stack marketing site (decisión fundacional)**:
- **ASP.NET Core MVC .NET 9** (no Next.js — se aceptó el trade-off de unificar stack).
- Bootstrap 5 + SCSS custom + tokens compartidos con producto.
- Output Caching (.NET 9 nuevo) + WebOptimizer para bundling.
- SCSS compilado en build (`DartSassBuilder`).
- `SixLabors.ImageSharp.Web` para imágenes AVIF/WebP.
- Cloudflare delante (CDN + WAF + Always Online + Brotli edge).
- Dominios: `tallerpro.mx` (marketing), `app.tallerpro.mx` (API), `admin.tallerpro.mx` (super-admin).
- Blog con MDX ya no aplica — usaremos Markdown con Markdig.
- Analytics: **Plausible self-hosted** (no GA4, evita banner de cookies LFPDPPP).

**Modelo comercial**: Plan único $899 MXN + sucursal adicional $449 MXN + overage metered (ver P5 para detalle).

**Integraciones externas** (decisiones ya tomadas):
- **Email transaccional + marketing**: Brevo (API v3, NuGet `sib_api_v3_sdk`).
- **CFDI 4.0**: SW Sapien (cliente custom HTTP + Polly).
- **WhatsApp Business**: Meta Cloud API directo (evaluar 360dialog / Gupshup como alternativas).
- **Telegram**: Bot API oficial (gratis, trivial).
- **SMS México**: Labsmobile primario, Twilio como fallback.
- **Pagos**: Stripe Billing (MVP), Stripe Connect / Mercado Pago (post-MVP).
- **IA**: Novita (ya resuelto en P6).

**Sync offline**:
- Cliente Hybrid con SQLite local + outbox pattern.
- Resolución de conflictos: LWW con RowVersion (entidades simples), CRDT/monotónico (stock), merge manual UI (CFDI, pagos).
- Datos cifrados localmente (tokens, claims, folios no timbrados).
- Política de purga: órdenes cerradas > 90 días fuera de caché local.

**Deployment**:
- **Alpha** (≤ 20 tenants): Docker Compose en VPS (Hetzner CX31 / Contabo).
- **Beta** (≤ 200): Azure Container Apps consumption.
- **GA** (≤ 2,000): ACA dedicated o AKS.
- **Cliente Hybrid**: MSIX con auto-update servidor propio (Windows), APK Play Console canal cerrado (Android).
- CI/CD: GitHub Actions.

---

## Tu Rol

Actúa como **Arquitecto Full-Stack Senior + DevOps** con experiencia en:
- ASP.NET Core MVC para sitios de marketing SaaS B2B con SEO técnico serio.
- Integración de pasarelas de pago con flujos de signup.
- Integraciones con PACs mexicanos (SW Sapien), Meta WhatsApp Cloud API, Telegram Bot, SMS providers.
- Sync offline bidireccional en Blazor Hybrid con SQLite.
- CI/CD en GitHub Actions para .NET.
- Deployment en Azure Container Apps, Hetzner, AKS.
- MSIX + auto-update + code signing.

Responde con código ASP.NET Core MVC ejecutable, configs de deployment listas, y scripts operables.

---

## Alcance de ESTE prompt (P7)

Entregar los **últimos componentes del sistema**: marketing site, flujo signup → onboarding, integraciones externas clave, sync offline patterns, deployment + CI/CD.

**SÍ incluir**:
1. Estructura y código clave del `TallerPro.Web` (marketing MVC).
2. Pricing page con calculadora interactiva.
3. Flujo signup → Stripe Checkout → webhook → provisión tenant (end-to-end).
4. Clientes .NET para integraciones (SW Sapien, WhatsApp Meta, Telegram Bot, SMS Labsmobile, Brevo).
5. Patrón completo de sync offline con conflictos.
6. Configuración Docker Compose (alpha) + Azure Container Apps (beta).
7. Pipeline CI/CD GitHub Actions.
8. Distribución del cliente Hybrid (MSIX + APK).
9. SEO técnico (sitemap, structured data, Open Graph).
10. Aviso de Privacidad LFPDPPP con sección de acceso de soporte.

**NO incluir**:
- Código de IA (P6), pagos detallado (P5), auth (P4), schema DB (P3), aislamiento (P2), decisiones fundacionales (P1).
- Todo lo anterior ya está resuelto — puedes referenciar.

---

## Formato de Respuesta Esperado

### 1. Assumptions a validar
2-3 sobre marketing, deployment o integraciones.

### 2. Estructura del proyecto `TallerPro.Web`

Árbol completo del proyecto MVC con todos los archivos clave:
- Areas (`Marketing`, `Legal`, `Auth`).
- Controllers, Views, ViewModels.
- Services (`StripeCheckoutService`, `BackendApiClient`, `MarkdownBlogService`).
- Styles SCSS con tokens compartidos.
- Content/Blog con Markdown files.
- wwwroot con assets.

### 3. Pricing Page con Calculadora Interactiva

**View Razor completa** `/Areas/Marketing/Views/Precios/Index.cshtml`:
- Hero con propuesta de valor del pricing.
- Plan base card destacado.
- Sección "Qué pagas si creces" con tarifas overage.
- **Calculadora interactiva** (JavaScript vanilla, no React/Alpine):
  - Sliders: # sucursales (1-20), órdenes/mes/sucursal (50-2000), % facturadas (0-100%), WhatsApp por orden (0-5), % IA avanzada.
  - Cálculo real-time del costo estimado: base + sucursales + overage proyectado.
  - Muestra breakdown: "Plan base: $X | Sucursales: $Y | Overage estimado: $Z | Total: $W".
  - Call-to-action "Empezar trial 14 días" con values pre-llenados.
- FAQ de pricing (qué pasa si excedo el pool, cómo se factura, cancelación).

Código JavaScript vanilla de la calculadora (self-contained, ~100 líneas).

### 4. Flujo Signup → Stripe Checkout → Webhook → Provisión

**Controller `RegistroController`** en `Areas/Auth`:
- `GET /registro?plan=base` → view con form.
- `POST /registro/crear` → validación + crea Stripe Checkout Session → redirect 303.

**Código del `StripeCheckoutService`**:
- Crea Customer en Stripe si no existe.
- Crea Checkout Session con:
  - `mode: 'subscription'`.
  - `line_items`: plan base + (opcional) N-1 sucursales adicionales si ya lo sabe.
  - `subscription_data.trial_period_days: 14`.
  - `payment_method_collection: 'always'` (captura método aún en trial).
  - `success_url` y `cancel_url`.
  - Metadata con UTMs capturados.

**Webhook handler** (ya existe en P5, aquí solo el endpoint y dispatch):
- `POST /api/webhooks/stripe` en `TallerPro.Web`.
- Valida firma.
- Dispatch a Mediator handler `CheckoutSessionCompletedCommand` → llama a backend interno `POST app.tallerpro.mx/internal/tenants/provision` con shared secret HMAC.

**Provisión del tenant** en `TallerPro.Api`:
- Crea `Tenant` con `StripeCustomerId`, `StripeSubscriptionId`, `Status=Trial`, UTMs.
- Crea primer `Branch` con datos del form.
- Crea primer `User` (Admin) con email + password temporal.
- Seed de catálogos iniciales para el branch.
- Genera link de descarga firmado (expira en 7 días) del MSIX.
- Envía email via Brevo con credenciales + link descarga.
- Slack `#signups` informativo.

**Email template de bienvenida** completo (HTML).

Diagrama de secuencia ASCII del flujo end-to-end.

### 5. Landing Page + Otras Vistas de Marketing

Views Razor para:
- **Home** (`/`): hero + propuesta de valor + features con íconos + social proof + CTA duplicado.
- **Características** (`/caracteristicas`): detalle por módulo (Recepción IA, Cotización, Inventario, Custodia, Panel Mecánico, CFDI, Analítica).
- **Por qué TallerPro** (`/por-que`): posicionamiento vs Excel/cuadernos/ERPs genéricos.
- **Contacto** (`/contacto`): form simple con envío a `#contact-leads` Slack via webhook.
- **Demo** (`/demo`): form con email + día preferido → Google Calendar integration (opcional post-MVP, MVP solo guarda lead).
- **Blog** (`/blog`): índice con posts MDX + detalle `/blog/{slug}`.

Cada view con metadata SEO (Open Graph + Twitter Cards + structured data).

### 6. SEO Técnico

Código de:
- **`SitemapController`** que genera `/sitemap.xml` dinámico recorriendo rutas MVC + blog posts.
- **`/robots.txt`** en wwwroot.
- **JSON-LD middleware** que inyecta `<script type="application/ld+json">` en cada view según tipo: `SoftwareApplication` en home, `Organization` global, `Product` con `offers` en precios, `BreadcrumbList` en todas, `FAQPage` en pricing FAQ, `Article` en blog posts.
- **Canonical URLs** via Tag Helper.
- **`hreflang` preparado** para futuro multi-idioma.
- Configuración de **Output Caching** con tags por ruta + invalidación cuando se publique blog post.

Meta target: **Lighthouse Mobile ≥ 85, Desktop ≥ 90, LCP < 2s en 4G**.

### 7. SCSS Pipeline + Tokens Compartidos

**`shared-tokens.scss`** (consumido por `TallerPro.Web` y por `Taller.Components` para generar MudTheme):
```scss
$color-primary: #E30613;      // rojo automotriz (validar con founder)
$color-secondary: #1C1C1C;    // negro técnico
$color-accent: #FFC107;       // ámbar (warnings, attention)
$color-success: #2E7D32;
$color-error: #C62828;
$color-surface: #FFFFFF;
$color-background: #F5F5F5;

$font-family-base: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
$font-size-base: 16px;
$line-height-base: 1.5;

$spacing-unit: 8px;
$radius-sm: 4px;
$radius-md: 8px;
$radius-lg: 16px;

// ... sombra, breakpoints, etc.
```

**Pipeline de build**:
- `DartSassBuilder` compila SCSS → CSS en `wwwroot/dist/` durante `dotnet build`.
- `WebOptimizer` bundlea + minifica CSS y JS.
- `MudThemeBuilder` (source generator C# en `Taller.Components`) lee `shared-tokens.scss` vía regex simple y genera `MudTheme` con los tokens.

Código del source generator del `MudThemeBuilder`.

### 8. Aviso de Privacidad LFPDPPP

View completa `/Areas/Legal/Views/AvisoDePrivacidad/Index.cshtml` con texto legal validado:
- Identidad del responsable.
- Datos recabados (operativos, de pago, de uso, auto-generados).
- Finalidades primarias y secundarias (opt-in obligatorio con checkbox).
- **Transferencias declaradas**: Stripe (USA/Irlanda), Novita AI (global), Brevo (Francia), SW Sapien (MX), Meta WhatsApp (global), Cloudflare (global).
- **Sección "Acceso del equipo de soporte de TallerPro"** con condiciones: notificación por email, log visible al tenant, duración máxima 60 min, motivo justificado, tenant puede terminar sesión.
- Derechos ARCO + procedimiento.
- Datos de contacto del responsable.
- Fecha de última actualización.

Checkbox en form de registro "Acepto el aviso" con link al texto.

### 9. Clientes .NET para Integraciones

Para cada integración, entrega código C# completo del cliente + política Polly + logging Serilog:

**a) `SwSapienClient`** (CFDI 4.0):
- Interfaz: `TimbrarAsync(xml, ct)`, `CancelarAsync(uuid, motivo, ct)`, `ConsultarEstatusAsync(uuid, ct)`.
- Cliente HTTP custom con base URL + API key.
- Polly: retry 3× con backoff + circuit breaker + timeout 15s.
- Validación XSD local antes de enviar (evita rebotes del PAC).
- Logs estructurados + Error handling con códigos SAT conocidos.

**b) `WhatsAppMetaClient`** (Meta Cloud API):
- Interfaz: `SendUtilityMessageAsync(to, template, params, ct)`, `SendMarketingMessageAsync(...)`, handler para incoming messages via webhook.
- Manejo de errores específicos de Meta (rate limits, template no aprobado, 24h window).
- Integración con `UsageTracker.RecordWhatsAppMessageAsync` automática.

**c) `TelegramBotClient`** (Telegram Bot API):
- Cliente simple con NuGet `Telegram.Bot`.
- Setup webhook vs long polling.
- Handler de comandos básicos (ej: `/estatus {orderNumber}`).

**d) `LabsmobileSmsClient`** (SMS México):
- Cliente HTTP custom.
- `SendAsync(phone, text, ct)` → retorna `SmsResult`.
- Integración con `UsageTracker.RecordSmsAsync`.

**e) `BrevoClient`** (Email transaccional + marketing):
- Interfaz para envío transaccional con templates.
- Manejo de bounces + spam complaints via webhook de Brevo.
- Opt-in/opt-out del cliente final.

Todos registrados en DI con `AddHttpClient` + `AddStandardResilienceHandler`.

### 10. Patrón de Sync Offline Completo

**Arquitectura del sync** en Blazor Hybrid:

**Outbox Pattern Local**:
```csharp
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string AggregateType { get; set; } // "ServiceOrder", "Vehicle", etc.
    public Guid AggregateId { get; set; }
    public string Operation { get; set; } // "Create", "Update", "SoftDelete"
    public string Payload { get; set; } // JSON
    public OutboxStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**`SyncService`** en cliente Hybrid:
- Detecta conectividad (ping a backend cada 30s).
- Cuando hay conexión, procesa outbox en batches de 50.
- Por cada mensaje: envía al endpoint correspondiente del backend.
- Maneja respuestas:
  - 200 OK → marca `Synced`.
  - 409 Conflict → invoca resolución de conflictos.
  - 4xx → marca `Failed` permanente (escalate).
  - 5xx → reintento con backoff.

**Resolución de conflictos**:

1. **LWW con RowVersion** (entidades simples: Customer, Vehicle, SettingsCatalogItem):
   - Backend responde 409 con la versión actual.
   - Cliente muestra UI "Ambos editaron este cliente. ¿Conservar mi versión, la del servidor, o fusionar?".

2. **CRDT/Contadores monotónicos** (stock de inventario):
   - Los movimientos de inventario son inmutables.
   - Cliente acumula `InventoryMovements` localmente.
   - Al sync, envía los movimientos y el backend los aplica atómicamente.
   - Stock resultante = suma de movimientos (commutative).

3. **Merge manual** (CFDI, pagos):
   - **Nunca auto-resolver cosas fiscales**.
   - Cliente acumula en outbox como "Pending for manual review".
   - UI specific que muestra al admin del tenant al reconectar.

**Encryption at rest** del SQLite local con DataProtection + keyring del OS:
- Windows: DPAPI.
- Android: Keystore.

Código C# completo del `SyncService` + `ConflictResolver` + UI MudBlazor para resolución manual.

### 11. Distribución del Cliente Hybrid

**Windows (MSIX)**:
- Proceso de packaging con `dotnet publish -f net9.0-windows10.0.22000.0`.
- Firmar con **Azure Trusted Signing** ($10/mes).
- Publicar a feed propio (Azure Blob público o CloudFront).
- Auto-update vía MSIX AppInstaller con check cada startup.
- Archivo `.appinstaller` con URL del feed.
- Código del updater check en app startup.

**Android (APK)**:
- Proceso build con `dotnet publish -f net9.0-android`.
- Firmar con keystore (instrucciones de generación + backup en Bitwarden/1Password).
- Play Console **canal cerrado** (invitaciones por email) — evita review pública en MVP.
- MDM opcional para talleres enterprise (Intune/Hexnode).

### 12. Docker Compose (Alpha) — VPS Self-Hosted

**`docker-compose.prod.yml`** completo:
- `tallerpro-api` (backend).
- `tallerpro-web` (marketing).
- `tallerpro-admin` (super-admin portal).
- `sqlserver` (Azure SQL managed alternativa — solo VPS si presupuesto es crítico).
- `redis` (SignalR backplane + cache).
- `seq` (logs).
- `caddy` o `nginx` (reverse proxy + TLS con Let's Encrypt).
- Volúmenes persistentes.
- Health checks.
- Resource limits.
- Networks.
- Restart policies.

Incluye `.env.example` con todas las variables necesarias.

### 13. Azure Container Apps (Beta)

Config de ACA:
- Resource group structure.
- Azure Container Registry (para imágenes).
- Azure Container Apps environment.
- Apps deploy YAMLs / Bicep template.
- Secrets management con Azure Key Vault.
- Application Gateway o Azure Front Door para WAF.
- Autoscaling rules.
- Azure SQL Managed Instance como DB.

Estimación de costo mensual en ACA beta (~200 tenants): $700-1,500 USD.

### 14. CI/CD con GitHub Actions

**Workflows** completos:

**`.github/workflows/ci.yml`** — PR checks:
- Restore, build, test.
- Run `TallerPro.Isolation.Tests` (critical path).
- Run `TallerPro.Analyzers.Tests` (Roslyn rules).
- Code coverage report.
- SCSS compilation check.
- MudBlazor CSS bundle validation.

**`.github/workflows/deploy-staging.yml`** — on merge to `develop`:
- Build Docker images.
- Push to ACR.
- Deploy to ACA staging environment.
- Run smoke tests.
- Notify Slack.

**`.github/workflows/deploy-prod.yml`** — on tag release `v*.*.*`:
- Build + sign.
- Deploy a prod con approval manual (GitHub Environments).
- EF Core migrations con script idempotente (dry-run first).
- Smoke tests post-deploy.
- Auto-rollback si smoke tests fallan.

**`.github/workflows/mobile-build.yml`** — build de Android APK + Windows MSIX.

### 15. Variables de Entorno y Secrets

Tabla completa de todas las variables que necesita el sistema por ambiente (dev, staging, prod):

| Variable | Descripción | Dev | Staging | Prod | Dónde vive |
|---|---|---|---|---|---|
| `ConnectionStrings__Default` | SQL Server | local | Azure SQL | Azure SQL MI | User secrets / Key Vault |
| `Stripe__SecretKey` | Stripe API key | test | test | live | User secrets / Key Vault |
| `Stripe__WebhookSecret` | Stripe webhook signing | | | | |
| `Novita__ApiKey` | Novita API key | | | | |
| `SwSapien__ApiKey` | SW Sapien | | | | |
| `Meta__WhatsApp__AccessToken` | Meta Cloud API | | | | |
| `Meta__WhatsApp__PhoneNumberId` | | | | | |
| `Telegram__BotToken` | | | | | |
| `Labsmobile__ApiKey` | SMS | | | | |
| `Brevo__ApiKey` | Email | | | | |
| `Seq__ServerUrl` | | | | | |
| `Seq__ApiKey` | | | | | |
| `Cloudflare__ZoneId` | | | | | |
| `Cloudflare__ApiToken` | | | | | |
| `Slack__Webhooks__Alerts` | | | | | |
| `Slack__Webhooks__AdminAudit` | | | | | |
| `JWT__SigningKey` | | | | | |
| `DataProtection__KeyVaultUri` | | | | | |

### 16. Runbook de Deployment

Documento markdown `docs/runbooks/deployment.md` con:
- Pre-deployment checklist.
- Steps exactos para deploy a staging.
- Steps para deploy a prod con approval.
- Rollback procedure (incluyendo DB migrations).
- Post-deployment smoke tests (lista).
- Quién notificar al deployar.

### 17. Tests Críticos

Tests en `TallerPro.Integration.Tests`:
- `Signup_CompleteFlow_CreatesTenantAndSendsEmail`.
- `Webhook_StripeCheckoutSession_ProvisionsTenantCorrectly`.
- `SyncService_ConflictLWW_ResolvesLatestWins`.
- `SyncService_StockConflict_MergesCommutatively`.
- `SwSapien_Timbrado_HandlesXsdErrorGracefully`.
- `WhatsApp_SendUtility_RecordsUsageMeter`.

Código completo.

---

## Restricciones de la Respuesta

- **Código ejecutable** (C#, Razor, SCSS, JS vanilla, YAML).
- Usa .NET 9 idioms.
- Convenciones del proyecto: Mediator (Othamar), Mapster, Serilog, FluentValidation, MudBlazor.
- Prioriza código + YAML + scripts sobre prosa.
- Longitud target: ~10,000-12,000 tokens.

---

## Al final de tu respuesta

Genera **"ADR Final Summary"** con el consolidado de TODAS las decisiones arquitectónicas del proyecto (referenciando ADR Updates de P1-P7). Este será el documento final de referencia para el equipo.
