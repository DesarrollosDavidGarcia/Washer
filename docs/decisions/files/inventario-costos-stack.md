# Inventario de Costos — SaaS Talleres Mecánicos
## Stack .NET + Blazor Hybrid + DeepSeek/Novita + Stripe
## Cifras en USD salvo donde se indique MXN

Documento vivo. Todas las cifras son referencia abril 2026 y deben validarse con cotización directa del proveedor.

---

## 1. Resumen ejecutivo por fase

| Fase | Tenants | Infra fija mensual | Variable estimado (10 talleres × 3 suc × 50 órdenes/día) | Total mensual estimado |
|------|---------|---------------------|-----------------------------------------------------------|------------------------|
| **Pre-launch / dev** | 0 | $30–80 | $0 | **$30–80** |
| **Alpha** (pilotos) | 1–5 | $150–250 | $50–120 | **$200–370** |
| **Beta** | 20–50 | $400–700 | $200–500 | **$600–1,200** |
| **Early GA** | 100–200 | $900–1,500 | $800–2,000 | **$1,700–3,500** |
| **Scale GA** | 500+ | $2,500+ | $3,000+ | **$5,500+** |

> Los números de equipo humano **no** están incluidos arriba. Se desglosan al final.

---

## 2. Licencias de software (librerías y frameworks)

### 2.1 Stack confirmado — gratis siempre

| Componente | Licencia | Costo | Notas |
|---|---|---|---|
| .NET 9 / .NET 10 | MIT | $0 | Microsoft, LTS garantizada |
| ASP.NET Core (MVC, API, SignalR) | MIT | $0 | Incluido en .NET |
| EF Core | MIT | $0 | |
| Blazor Hybrid / .NET MAUI | MIT | $0 | |
| **MudBlazor** | MIT | $0 | ✅ Safe, no commercial risk |
| **Mapster** | MIT | $0 | ✅ Source generators |
| **FluentValidation** | Apache 2.0 | $0 | |
| **Polly** | BSD 3-Clause | $0 | |
| **Serilog** | Apache 2.0 | $0 | |
| **OpenAI NuGet** (para Novita) | MIT | $0 | |
| **Stripe.net** | Apache 2.0 | $0 | |
| **Markdig** (blog marketing) | BSD 2-Clause | $0 | |
| SixLabors.ImageSharp.Web | Apache 2.0 | $0 | Image optimization marketing |
| WebOptimizer | Apache 2.0 | $0 | Bundling + minification |

### 2.2 ⚠️ Decisiones con riesgo de licenciamiento

| Componente | Situación actual | Recomendación |
|---|---|---|
| **MediatR** (v13+) | **Dual license** desde jul 2025. Community gratis para empresas con revenue < $5M USD/año, con re-registro anual y warnings en logs. Standard/Pro/Enterprise pagos. | **Reemplazar por `Mediator` de Martin Othamar (MIT, source generators, API casi idéntica)**. O usar `IMediator` custom en ~50 líneas. |
| **AutoMapper** (v16+) | Igual que MediatR | Ya descartado — usamos **Mapster** ✅ |
| **FluentAssertions** (v8+) | Commercial desde 2024 | Para tests: usar **Shouldly** (MIT, activo) o **xUnit assertions** nativas |
| **MassTransit** | Commercial desde 2025 | No lo necesitamos en MVP, pero si llega la hora de bus: evaluar **Wolverine** (MIT) o **NATS** + cliente nativo |
| **Moq** | Cambió a paga y luego revirtió con controversia | Usar **NSubstitute** (BSD) como default para mocks |

### 2.3 Alternativas OSS sugeridas (reemplazo directo)

```csharp
// En lugar de MediatR.Send(new CreateOrderCommand())
// Mediator (Othamar) tiene API casi idéntica:
using Mediator;

public record CreateOrderCommand(...) : IRequest<OrderDto>;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public ValueTask<OrderDto> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // ...
    }
}

// Registro en Program.cs
builder.Services.AddMediator(); // genera en build, no reflexión
```

**Beneficios adicionales de `Mediator` vs `MediatR`**:
- Generación en compile time → zero reflexión en runtime.
- Detecta handlers faltantes o duplicados en build, no en producción.
- AOT ready (importante si .NET 10+ pushea AOT default).
- Sin warnings en logs, sin re-registro anual.

---

## 3. Herramientas de desarrollo

| Tool | Costo mensual/anual | Notas |
|---|---|---|
| **Visual Studio Community** | $0 | ✅ Gratis para equipos < 5 devs y empresas con revenue < $1M USD. Tu caso aplica. |
| Visual Studio Professional | ~$45 USD/mes/dev | Solo si superan criterios de Community |
| **JetBrains Rider** | $14–25 USD/mes/dev (50% descuento startup primer año) | Alternativa popular en equipos .NET modernos |
| **VS Code** + C# Dev Kit | $0 | Excelente opción liviana, compatible con Blazor/MAUI |
| **GitHub** (repos privados) | Free plan: ✅ suficiente | Team $4/user/mes si necesitas auditoría o SAML |
| **GitHub Copilot Business** | $19 USD/mes/dev | Opcional, acelera productividad ~20-40% |
| **Claude Code / Cursor** | $20 USD/mes/dev | Alternativa Copilot |
| **Azure DevOps** | Free para ≤5 users | Alternativa a GitHub Actions |
| **Postman / Bruno / Insomnia** | $0 (tier free suficiente) | |
| **DBeaver** | $0 | Cliente SQL gratuito |

**Recomendación MVP**: VS Community + GitHub Free + Copilot ($19/dev/mes opcional). Para 3 devs = **$0–57/mes** total en tooling.

---

## 4. Infraestructura cloud (fijos mensuales)

### 4.1 Compute — Backend API + SignalR + jobs

| Opción | Costo | Uso recomendado |
|---|---|---|
| **Hetzner Cloud CX31** (4 vCPU, 8 GB) | $12 | Pre-launch y alpha inicial |
| **Contabo Cloud VPS M** (6 vCPU, 16 GB) | $10 | Barato, bueno para alpha |
| **DigitalOcean Droplet** (4 vCPU, 8 GB) | $48 | Más caro pero UX mejor |
| **Azure Container Apps** (consumption) | $20–150 según tráfico | Serverless, escala a 0 en idle |
| **Azure Container Apps** (dedicated) | $150–400 | Beta/GA con autoscaling |
| **Azure App Service** B2 | $55 | Simple, managed |
| **AKS** (Azure Kubernetes) | $150+ nodes + $75 control plane | Solo cuando justifique (GA escala) |

**Recomendación por fase**:
- Alpha: **Hetzner CX31** ($12) o Contabo VPS ($10). Docker Compose. Total compute < $15/mes.
- Beta: **Azure Container Apps** consumption ($50–150).
- GA: **ACA dedicated** ($200–400) o migración a AKS según escala.

### 4.2 Base de datos — SQL Server

| Opción | Costo | Notas |
|---|---|---|
| **SQL Server Express** en VPS | $0 licencia | ⚠️ Límites: 10 GB/DB, 1 GB RAM, 1 socket. OK pre-launch, se queda corto en alpha. |
| **SQL Server Developer** | $0 | ⚠️ Licencia prohíbe producción. No usar. |
| **Azure SQL Database Basic** (DTU) | $5/mes | 2 GB max, muy limitado |
| **Azure SQL Database Standard S0** | $15/mes | 250 GB, suficiente alpha |
| **Azure SQL Database Standard S2** | $75/mes | Beta |
| **Azure SQL Database vCore GP 2 vCore** | $370/mes | Beta/Early GA |
| **Azure SQL Managed Instance GP 4 vCore** | $740/mes | GA real, cross-DB queries, VNet |
| **SQL Server Standard en VM** | $600–900/mes licencia + VM | Alternativa self-managed |
| **PostgreSQL en Azure Flex** (¿cambio de plan?) | $15–100/mes comparable | Solo si reconsideran SQL Server |

**Recomendación**:
- Pre-launch/Alpha: **Azure SQL DB Standard S0 o S1** ($15–30/mes).
- Beta: **Azure SQL DB S2 o GP 2 vCore** ($75–370).
- GA: **Azure SQL MI GP 4 vCore** ($740) solo cuando el volumen justifique.

**Palanca de costo clave**: diferir MI cuesta $400–700/mes; cada mes de retraso es dinero que sigues quemando. Pero migrar a MI después de entrar en GA también es feasible con downtime mínimo.

### 4.3 Redis (SignalR backplane + cache)

| Opción | Costo | Notas |
|---|---|---|
| **Redis autohospedado** en VPS | $0 extra | Si el VPS tiene RAM. Alpha con 1 instancia OK. |
| **Upstash Redis** free tier | $0 | 10k commands/día, suficiente alpha/beta |
| **Azure Cache for Redis Basic C0** | $17/mes | 250 MB, OK alpha |
| **Azure Cache for Redis Standard C1** | $72/mes | Beta, con HA |
| **Azure Cache Premium P1** | $415/mes | GA con sharding |

**Recomendación MVP**: autohospedado o Upstash free. Costo: **$0** hasta beta.

### 4.4 Storage (Blob: fotos custodia, XMLs CFDI, backups)

| Tipo | Costo |
|---|---|
| Azure Blob Hot | $0.018/GB/mes + egress |
| Azure Blob Cool | $0.010/GB/mes + mayor acceso cost |
| Cloudflare R2 | $0.015/GB/mes **sin egress** ⭐ |
| Backblaze B2 | $0.006/GB/mes + egress |

**Estimación alpha** (10 tenants × 3 suc × 50 órdenes/día × 10 fotos × 500 KB + XMLs):
- ~225 GB/mes de nuevos archivos
- ~$4–10/mes con R2 o Blob Cool

**Recomendación**: **Cloudflare R2** por cero egress (fotos de custodia se ven mucho). **$5–15/mes en alpha**.

### 4.5 CDN + WAF

| Opción | Costo | Notas |
|---|---|---|
| **Cloudflare Free** | $0 | ✅ Incluye CDN, WAF básico, DDoS, SSL, Always Online |
| Cloudflare Pro | $20/mes/dominio | Image optimization, WAF avanzado, reglas custom |
| Azure Front Door Standard | $35+ | Integración nativa con Azure |

**Recomendación**: **Cloudflare Free** para alpha/beta. Upgrade a Pro ($20) en GA si necesitas image resize.

### 4.6 Observabilidad

| Componente | Opción gratis | Opción paga |
|---|---|---|
| Logs estructurados | **Seq self-hosted** ($0 hasta 40 GB/día) | Seq Team Edition $660/año |
| Metrics + traces | **Grafana Cloud Free** (10k series, 50 GB logs, 50 GB traces) | Grafana Cloud Pro $49+/mes |
| Errors / exceptions | **Sentry Developer** (5k events/mes gratis) | Sentry Team $26+/mes |
| APM | Azure Application Insights free tier (5 GB/mes) | Excede ~$2.76/GB |
| Uptime monitoring | **UptimeRobot free** (50 monitors, 5 min) | UptimeRobot Pro $7/mes |
| **Datadog** | $0 trial 14 días | **$31+ USD/host/mes** ⚠️ caro |

**Recomendación stack alpha**: **Seq self-hosted + Grafana Cloud Free + Sentry Dev + UptimeRobot free**. Costo: **$0–15/mes**.

### 4.7 Dominios

| Item | Costo anual |
|---|---|
| Dominio .mx | ~$30–40 USD/año (~$500–700 MXN) |
| Dominio .com | ~$15 USD/año |
| Dominios adicionales (app.* etc.) | $0 (subdominios del principal) |

**Total**: ~$45 USD/año ≈ **$4/mes** amortizado.

### 4.8 Code signing certificate (MSIX Windows)

| Opción | Costo |
|---|---|
| **Sin signing** | $0 pero Windows Defender marca SmartScreen Warning por meses hasta ganar reputación. **No aceptable para B2B**. |
| OV Certificate (Sectigo, ssls.com) | $70–150/año |
| EV Certificate (Sectigo) | $300–700/año. Confianza inmediata, sin delay de reputación |
| Azure Trusted Signing | $10/mes (preview/GA reciente) — managed, integrado con Azure DevOps |

**Recomendación**: **Azure Trusted Signing** ($10/mes) o OV Certificate ($70–150/año).

### 4.9 Play Console Android

- One-time **$25 USD**. Registro único por cuenta de desarrollador.

---

## 5. SaaS de terceros (integraciones)

### 5.1 IA — Novita (DeepSeek)

Precios verificados abril 2026 (serverless):
- **DeepSeek V3.1**: $0.27/MT input, $1.00/MT output (cache hit $0.135/MT)
- **DeepSeek V3.2 Exp**: $0.27/MT input, $0.41/MT output
- **DeepSeek V3 0324**: $0.27/MT input, $1.12/MT output
- **DeepSeek-OCR**: $0.03/MT input/output (para cuando agregues lectura de manuales escaneados)

**Estimación de costo por orden de servicio procesada E2E**:

| Tarea | Modelo | Tokens aprox | Costo |
|---|---|---|---|
| Clasificación síntoma | V3.2 non-thinking | 500 in + 200 out | $0.00022 |
| Pre-orden con tool calling | V3.2 + JSON | 1,500 in + 500 out | $0.00061 |
| Diagnóstico con RAG (opcional) | V3.2 Speciale o R1 | 4,000 in + 800 out | $0.003 |
| Feedback/summary (opcional) | V3.2 | 1,000 in + 300 out | $0.00039 |
| **Total por orden (típico)** | | | **~$0.003–0.005** |
| **Con diagnóstico RAG pesado** | | | **~$0.02–0.05** |

**Estimación mensual alpha** (10 tenants × 3 suc × 50 órdenes/día = 45,000 órdenes/mes):
- Bajo uso IA (solo clasificación + pre-orden): 45,000 × $0.0005 = **$22/mes**
- Uso medio (incluye diagnóstico RAG): 45,000 × $0.003 = **$135/mes**
- Uso alto (RAG pesado + summaries): 45,000 × $0.015 = **$675/mes**

**Palancas de optimización de costo IA**:
1. Cache de respuestas similares (embedding matching local) — puede reducir 30–50% el tráfico.
2. Usar `deepseek-v3.2` non-thinking por default, escalar a Speciale solo si confidence < umbral.
3. Prompt caching de Novita (cache read 50% más barato) — útil si tienes system prompts largos constantes.
4. Batch inference de Novita ("50% cheaper") — útil para jobs nocturnos (resúmenes semanales).
5. Cost ceiling por tenant: hard cap $X/mes en plan Starter; si excede, rate-limit.
6. Modelo más pequeño como router (clasifica si la consulta amerita modelo grande).

### 5.2 Stripe (pagos)

**Suscripción SaaS en MX (Nivel 1)**:
- Tarjeta nacional: **3.6% + $3 MXN** por transacción exitosa
- Tarjeta internacional: 4.5% + $3 MXN
- OXXO Pay: **3.6% + $10 MXN** por voucher pagado
- SPEI (Customer Balance): **1.4% + $5 MXN**
- Refunds: comisiones de procesamiento **no se devuelven**

**Estimación MVP**:
- 10 tenants × $500 MXN/mes promedio = $5,000 MXN MRR
- Comisión Stripe: ~$230 MXN/mes (~$12 USD)

**Stripe Billing Starter**: $0 (no hay cuota fija básica). Solo % transaccional.

**Nivel 2 (Stripe Connect)**: post-MVP, comisiones adicionales por transferencia.

### 5.3 CFDI — SW Sapien

Pricing típico en MX (validar con proveedor):
- **Timbres en paquete**: $1–3 MXN por timbre según volumen
- 1,000 timbres: ~$1,500–2,500 MXN ($75–125 USD)
- 10,000 timbres: ~$10,000 MXN ($500 USD)
- 100,000 timbres: ~$70,000 MXN ($3,500 USD)

**Estimación alpha** (45,000 órdenes/mes × ~0.7 ratio de facturación = 31,500 CFDIs/mes):
- ~$47,000–95,000 MXN/mes ($2,350–4,750 USD)

⚠️ **Este es probablemente tu costo variable más grande después del equipo humano**. El pricing realmente empieza a doler en beta.

**Palancas de optimización**:
1. **Cobrar el CFDI al tenant** como línea separada en el plan (ej: Starter incluye 500 timbres, luego $1.50/timbre). La mayoría de SaaS del sector así cobra.
2. Negociar pricing por volumen con SW Sapien (descuento a partir de 50k+ timbres/mes).
3. Comparar con otros PACs: **Facturama**, **Finkok**, **The Factory HKA**. A veces 20–40% más baratos a volumen.
4. Batch de timbrado para reducir overhead API (algunos PACs descuentan por batch).

### 5.4 Email — Brevo (antes Sendinblue)

- **Free**: 300 emails/día — ✅ suficiente pre-launch
- **Starter**: $9/mes por 20,000 emails
- **Business**: $18/mes por 20,000 emails + marketing automation + A/B
- **Enterprise**: custom

**Estimación alpha**: 10 tenants × 200 emails transaccionales/mes + marketing nurture = ~5,000 emails/mes → **Free tier suficiente** ($0).

**Beta**: ~30,000 emails/mes → **Business $18/mes**.

### 5.5 WhatsApp Business (Meta Cloud API)

Precios desde julio 2025 (México):
- **Utility** (confirmaciones, actualizaciones): ~$0.015–0.025 USD/mensaje
- **Authentication** (OTPs): ~$0.015 USD/mensaje
- **Marketing**: ~$0.06–0.09 USD/mensaje
- **Service** (cliente inicia, respuestas en 24 h): **gratis primeras 1,000 conversaciones/mes, luego ~$0.015**

**Estimación alpha** (45,000 órdenes × 3 mensajes utility promedio):
- 135,000 mensajes utility: ~$2,700 USD/mes ⚠️ muy alto

⚠️ **Este costo puede explotar si no se controla**. Palancas:
1. **Limitar mensajes utility automáticos**: enviar solo los más críticos (listo para recoger, cotización aprobada).
2. **Usar Telegram Bot gratuito** para clientes que lo acepten (cero costo).
3. **SMS más barato** para fallback: $0.03 vs WhatsApp utility $0.02 (similar).
4. **Cobrar WhatsApp al tenant** en el plan (Starter sin WhatsApp, Pro incluye hasta X, Enterprise unlimited).
5. **Agregar capa de 360dialog o Gupshup**: a veces mejoran pricing en paquetes grandes y dan mejor soporte regional.

**Alternativas de proveedor WhatsApp**:
- Meta Cloud API directo: más barato pero menos soporte
- 360dialog: reseller europeo, bueno soporte, pricing similar + markup
- Gupshup: global, markup moderado, buena DX
- Twilio: markup alto pero UX excelente

### 5.6 SMS México

| Proveedor | Costo/SMS |
|---|---|
| **Labsmobile** | $0.025–0.04 USD |
| Twilio | $0.048 USD |
| Brevo SMS | $0.039 USD |
| **Infobip** | $0.035 USD |

**Estimación alpha**: 45,000 órdenes × 10% con SMS fallback × 2 mensajes = 9,000 SMS/mes → **$225–360 USD/mes**.

**Palanca**: SMS solo como fallback cuando WhatsApp falla, no como canal primario.

### 5.7 Telegram Bot

- **Gratis siempre**. Sin límites razonables para este caso.
- Incluir en MVP como canal opcional del cliente es pure upside.

### 5.8 Analytics marketing

| Opción | Costo |
|---|---|
| **Plausible Cloud** | $9–19 USD/mes (10k–100k pageviews) |
| **Plausible self-hosted** | $0 (Docker en VPS existente) |
| **Umami self-hosted** | $0 |
| **Vercel Analytics** | $0 si usaras Next.js (no aplica, MVC) |
| **Google Analytics 4** | $0 pero requiere cookie banner LFPDPPP |

**Recomendación**: **Plausible self-hosted** o **Umami** en el mismo VPS del backend. **$0/mes**.

### 5.9 Product analytics (dentro de la app, post-MVP)

| Opción | Costo |
|---|---|
| **PostHog Cloud free** | $0 hasta 1M events/mes |
| PostHog Cloud paid | $0.00005/event después |
| **PostHog self-hosted** | $0 + costo infra |

**Estimación alpha**: ~500k events/mes → **$0** con PostHog Cloud free.

### 5.10 Slack/Discord/comunicación

- **Slack Free**: 90 días de histórico, OK para startup inicial. $0.
- **Discord**: gratis.
- **Google Workspace**: $6 USD/user/mes (necesario para emails corporativos).

---

## 6. Legales y contables (costos one-time y recurrentes)

| Item | Costo | Frecuencia |
|---|---|---|
| Aviso de privacidad LFPDPPP redactado por abogado | $500–1,500 USD | One-time |
| Términos y condiciones SaaS | $500–2,000 USD | One-time |
| Contrato de encargado del tratamiento (con Novita, Stripe, Brevo) | $300–800 USD | One-time + revisiones anuales |
| Constitución de sociedad (SAPI de CV o similar) | $800–2,500 USD | One-time |
| Contabilidad mensual (contador) | $150–800 USD/mes | Recurrente |
| Auditoría LFPDPPP cuando escalen | $1,000–3,000 USD | Anual después de GA |

**Total one-time legal/constitución**: **~$2,000–7,000 USD** antes de soft launch.

---

## 7. Equipo humano (el costo dominante)

### 7.1 Salarios México (referencia abril 2026, plaza Chihuahua/Bajío)

| Rol | Nivel | Mensual MXN | Mensual USD aprox |
|---|---|---|---|
| Dev .NET fullstack | Jr (0–2 años) | $20,000–35,000 | $1,100–1,900 |
| Dev .NET fullstack | Mid (2–5 años) | $40,000–60,000 | $2,200–3,300 |
| Dev .NET fullstack | Sr (5+ años) | $65,000–95,000 | $3,600–5,200 |
| Dev .NET fullstack | Lead / Arquitecto | $95,000–140,000 | $5,200–7,700 |
| Tech lead / CTO fracción | | $120,000–200,000 | $6,600–11,000 |
| PM | Mid | $40,000–70,000 | $2,200–3,900 |
| PM | Sr | $70,000–110,000 | $3,900–6,100 |
| Diseñador UI/UX | Mid | $30,000–55,000 | $1,600–3,000 |
| Customer Success / Soporte | Jr–Mid | $20,000–45,000 | $1,100–2,500 |
| Marketer / SEO | Jr–Mid | $25,000–50,000 | $1,400–2,800 |

### 7.2 Equipo MVP asumido

| Rol | Cantidad | Costo mensual USD |
|---|---|---|
| Dev .NET Sr | 1 | $3,600–5,200 |
| Dev .NET Mid | 2 | $4,400–6,600 |
| PM (tú u otro) | 1 | $3,900–6,100 |
| **Total equipo** | **4** | **$11,900–17,900/mes** |

**Esto es 10–30× el costo de toda la infra y SaaS combinados**. La palanca de costo dominante es **no contratar más de lo necesario** y **productividad del equipo actual**.

Añadir un 5º rol (UX, marketer, CS) suma $1,500–3,000/mes más.

### 7.3 Post-launch (post-MVP)

| Rol | Cantidad | Cuándo |
|---|---|---|
| Customer Success / Soporte | 1 | Al llegar a ~20 tenants |
| Marketer / SEO | 1 | En paralelo con soft launch |
| Contador externo | 0.5 (outsourced) | Pre-launch |
| Legal fracción | on-demand | Antes de soft launch |

---

## 8. Tabla consolidada: costo mensual por fase (sin equipo)

### 8.1 Pre-launch / dev (3 personas trabajando en producto, cero clientes)

| Categoría | Costo |
|---|---|
| Compute (Hetzner CX31) | $12 |
| DB (Azure SQL Basic) | $5 |
| Storage (R2 mínimo) | $2 |
| CDN (Cloudflare Free) | $0 |
| Observabilidad (Seq self-hosted + Sentry free) | $0 |
| Dominios amortizados | $4 |
| Email (Brevo Free) | $0 |
| Analytics (Plausible self-hosted) | $0 |
| SMS + WhatsApp (ninguno aún) | $0 |
| CFDI (timbres de prueba) | ~$20 |
| IA (Novita pruebas dev) | $10–30 |
| Code signing (Azure Trusted Signing) | $10 |
| Play Console amortizado (one-time $25) | $2 |
| **TOTAL** | **~$65–85/mes** |

### 8.2 Alpha (5 tenants piloto, pagando trial o early adopter)

| Categoría | Costo |
|---|---|
| Compute (Hetzner + backup) | $30 |
| DB (Azure SQL S1) | $30 |
| Storage (R2) | $8 |
| CDN (Cloudflare Free) | $0 |
| Redis (Upstash free o autohosted) | $0 |
| Observabilidad | $0–15 |
| Dominios | $4 |
| Email (Brevo Starter) | $9 |
| Stripe (3.6% de $200 MXN × 5 tenants) | ~$2 |
| CFDI (SW Sapien, 5k timbres) | ~$400 |
| IA (Novita) | $30–100 |
| WhatsApp (Meta Cloud utility) | $50–200 |
| SMS fallback | $20–50 |
| Code signing | $10 |
| **TOTAL** | **~$600–850/mes** |

### 8.3 Beta (30 tenants activos)

| Categoría | Costo |
|---|---|
| Compute (ACA consumption) | $100 |
| DB (Azure SQL GP 2 vCore) | $370 |
| Storage (R2) | $25 |
| CDN | $0 |
| Redis (Azure Basic) | $17 |
| Observabilidad (Grafana Cloud + Sentry Team) | $26 |
| Email (Brevo Business) | $18 |
| Stripe | $15 |
| CFDI (SW Sapien 30k timbres) | ~$1,500 |
| IA | $400–1,200 |
| WhatsApp | $300–800 |
| SMS | $100–300 |
| **TOTAL** | **~$2,900–4,600/mes** |

### 8.4 Early GA (150 tenants)

| Categoría | Costo |
|---|---|
| Compute (ACA dedicated o AKS) | $400 |
| DB (Azure SQL MI GP 4 vCore) | $740 |
| Storage | $80 |
| CDN (Cloudflare Pro) | $20 |
| Redis Standard | $72 |
| Observabilidad | $50 |
| Email | $40 |
| Stripe | $100 |
| **CFDI (150k timbres/mes)** | **~$7,500** ⚠️ |
| IA | $1,500–4,000 |
| WhatsApp | $1,500–4,000 |
| SMS | $500 |
| **TOTAL** | **~$12,500–17,500/mes** |

---

## 9. Palancas estratégicas de optimización

### 9.1 Top 10 palancas para **reducir** costo

1. **Cobrar CFDI e integraciones al tenant en el plan** (pricing) — recupera 80% del costo variable.
2. **Usar modelos IA económicos por default** (V3.2 non-thinking), escalar solo si es necesario.
3. **Prompt caching de Novita** (50% descuento en cache hits) para system prompts repetidos.
4. **Self-hosting** para herramientas no-core (Plausible, Umami, Seq, Grafana) en lugar de pagar cloud.
5. **Cloudflare delante de todo** — reduce bandwidth, compute y WAF costs a casi $0.
6. **Hetzner/Contabo en lugar de Azure/AWS** para alpha (5–10× más barato).
7. **Telegram + Email como canales default**, WhatsApp/SMS solo cuando el tenant lo active y lo pague.
8. **Negociar pricing por volumen** con SW Sapien (o migrar a Facturama/Finkok si es mejor).
9. **Diferir migraciones managed** (Azure SQL MI, Azure SignalR Service, AKS) hasta que el tráfico justifique el 3–5× de precio.
10. **Batch jobs nocturnos** (reports, forecasting, summaries) con Novita batch inference (50% descuento).

### 9.2 Top 5 palancas para **invertir más** (cuando crezca el negocio)

1. **Observabilidad seria** (Datadog o equivalente) — $500–1,500/mes pero ahorra días de debugging.
2. **Azure SQL MI o SQL HA** — aislamiento, backups automáticos, PITR robusto.
3. **Code signing EV** — confianza inmediata, cero fricción para clientes enterprise.
4. **GitHub Copilot Business o Claude Code** — 20–40% productividad del equipo dev.
5. **Customer Success dedicado** — cada CS bien capacitado paga por sí mismo con retención y upsells.

### 9.3 Costos que NO debes recortar

- Backups + PITR. Si se pierde data, se acaba el negocio.
- Code signing (incluso el barato). Sin firma, B2B serio no instala tu software.
- Aviso de privacidad profesional. Un problema legal cuesta más que 100 avisos.
- SSL/TLS (gratis con Cloudflare/Let's Encrypt, no hay excusa).
- Auth correcto (no hagas DIY auth por ahorrarte $50/mes).
- Stripe (el % duele pero la alternativa es no cobrar).

---

## 10. Resumen de decisiones inmediatas de costo

### ✅ Decisiones "gratis o casi" que debes tomar ya

1. **Reemplazar MediatR → Mediator (Martin Othamar)** — $0 vs $X/año futuro
2. **Cloudflare Free** delante de todo — $0
3. **VS Community** mientras califiques — $0 vs $45/mes/dev
4. **Plausible self-hosted** — $0 vs $19/mes
5. **Telegram Bot** como canal alternativo a WhatsApp — $0 vs $0.02/mensaje
6. **Hetzner o Contabo** para alpha — $12 vs $150+ en Azure
7. **Cloudflare R2** para storage — ahorra 100% egress vs Blob
8. **Plans del SaaS que cobren CFDI/WhatsApp/IA al tenant** — evita que tu margen se vaporice
9. **Seq + Sentry Free + Grafana Cloud Free** — $0 vs $300+/mes en Datadog

### ⚠️ Decisiones que debes validar con cotización directa

1. **SW Sapien**: pedir quote por 10k, 50k, 100k timbres/mes. Comparar con Facturama y Finkok.
2. **Meta WhatsApp Cloud API vs 360dialog vs Gupshup**: pedir pricing paralelo para MX.
3. **Azure SQL MI**: calcular cuándo el ROI vs VM self-managed se cruza (usualmente ~100 tenants).
4. **Novita**: verificar pricing actual y si tienen acuerdos de volumen > 100M tokens/mes.

### 💡 Decisiones de pricing del SaaS (lado ingresos)

El costo variable se neutraliza cobrándolo al tenant:

| Concepto | Cuánto te cuesta | Cuánto cobrar en el plan |
|---|---|---|
| CFDI timbre | $1.50 MXN | Incluir 500/mes en Starter, $2–3 MXN/timbre adicional |
| IA por orden | ~$0.05 USD | Incluir en todos los planes, cap por tier |
| WhatsApp utility | $0.02 USD | Pro tier en adelante, o add-on $30/mes |
| SMS | $0.03 USD | Add-on explícito, no incluido por default |
| Storage fotos | $0.02/GB | Incluir 10 GB en Starter, add-on por GB |

---

## 11. Comparativa: costo total mensual alpha vs ingresos potenciales

### Escenario alpha "saludable"

- 10 tenants × $500 MXN/mes promedio = **$5,000 MXN MRR** (~$270 USD)
- Costo infra + SaaS: **~$700/mes**
- Costo equipo (4 personas): **~$14,000/mes**
- **Burn neto**: ~$14,430/mes
- **Runway con $200k USD inicial**: ~14 meses

### Escenario beta "product-market fit confirmado"

- 50 tenants × $800 MXN/mes promedio = **$40,000 MXN MRR** (~$2,150 USD)
- Costo infra + SaaS: **~$3,800/mes**
- Costo equipo (5 personas): **~$17,000/mes**
- **Burn neto**: ~$18,650/mes
- Necesita funding adicional o crecimiento acelerado

### Escenario GA "negocio viable"

- 200 tenants × $1,200 MXN/mes promedio = **$240,000 MXN MRR** (~$13,000 USD)
- Costo infra + SaaS: **~$15,000/mes**
- Costo equipo (7 personas): **~$25,000/mes**
- **Burn neto**: ~$27,000/mes (aún negativo, pero cercano a breakeven)

**Breakeven aproximado**: **~350–450 tenants** activos pagando plan promedio.

---

## 12. Conclusión

El **costo dominante es el equipo humano**, no la infra ni las SaaS. Esto es típico de SaaS B2B.

La **segunda categoría dominante en runtime es CFDI timbres**, que se neutraliza cobrándolo al tenant.

**IA (Novita/DeepSeek) es sorprendentemente barata** por el pricing de DeepSeek. Con V3.2, incluso con uso intensivo, no debería superar 5–10% del costo total.

**Stripe fees** son relativamente manejables (3.6%) vs alternativas más caras en MX.

**WhatsApp puede explotar** si no se controla; esta es la palanca de variación más importante de vigilar.

**Tool licensing de terceros** (MediatR, AutoMapper, MassTransit) es un riesgo creciente en el ecosistema .NET — diversifica desde el día 1 eligiendo alternativas MIT con momentum.
