# Prompt 1 de 7 — Fundaciones Arquitectónicas (ADR)

## Contexto del Proyecto (compartido con todos los prompts de esta serie)

**TallerPro**: SaaS multitenant + multitaller para gestión de talleres mecánicos en México, escalable a LatAm.

**Mercado**: Talleres independientes de 5–30 bahías, algunos multi-sucursal. Arranque en Chihuahua/Bajío. Internet intermitente es regla.

**Jerarquía de cuentas**:
```
Tenant (cliente del SaaS, paga suscripción)
  └── Branch 1..N (sucursales físicas)
       └── Warehouses, Orders, Vehicles, Inventory
User 1..N → UserBranchAccess (M:N) con rol por sucursal
Customer 1..N (cliente final del taller, scope Tenant)
  └── Vehicles 1..N
```

**Equipo**: Founder + 3 devs .NET fullstack + 1 PM. Founder tiene acceso total al sistema; equipo futuro de soporte tendrá solo read-only.

**Meta MVP**: 10 tenants facturando en 90 días.

**Presupuesto infra alpha**: < $1,000 USD/mes.

**Stack confirmado (no cuestionar)**:
- **Producto**: .NET Core 9 + Blazor Hybrid (MAUI Windows + Android) + MudBlazor 100% + Mediator (Martin Othamar, NO MediatR) + Mapster + EF Core + SQL Server + SignalR + Docker.
- **Marketing site**: ASP.NET Core MVC .NET 9 + Bootstrap 5 + SCSS + Cloudflare.
- **Super-Admin Portal**: `admin.tallerpro.mx` separado, MVC + MudBlazor, Cloudflare Access (Zero Trust).
- **Observabilidad**: Serilog + Seq self-hosted + enrichers custom.
- **IA**: DeepSeek V3.2/R1 vía Novita.ai (OpenAI-compatible endpoint).
- **Pagos**: Stripe Billing + Stripe Meters. Pricing: $899 MXN base por taller + $449 MXN por sucursal adicional + overage metered sobre pools (CFDI 500/mes, WhatsApp 1000/mes, IA 1000/mes, storage 10 GB).
- **CFDI**: SW Sapien.
- **Librerías complementarias**: FluentValidation, Polly v8, Shouldly (no FluentAssertions), NSubstitute (no Moq), Stripe.net.

**Convenciones técnicas no negociables**:
1. MudBlazor 100% UI, CSS custom solo vía SCSS.
2. Mediator (Othamar) con source generators, NO MediatR (comercial desde jul 2025).
3. Mapster (no AutoMapper por misma razón).
4. Serilog como logger único, prohibido `ILogger` crudo sin Serilog o `Console.WriteLine`.
5. Soft delete estricto, cero hard deletes (excepto inmutables: InventoryMovements, CFDIs, Payments, AuditLog, MeterEvents, ImpersonationAudits).
6. Catálogos genéricos solo para estructuras {Code, Name, Description, ExtraJson}; entidades ricas (Parts, Customers, Vehicles) en tablas dedicadas.
7. Aislamiento tenant como invariante del sistema (defense-in-depth 8 capas).
8. PII masking en dos niveles: antes de enviar a LLMs externos y en todos los logs.
9. MFA obligatorio para super-admin y tenant admin/owner.
10. Cloudflare Access para `admin.tallerpro.mx`.

**Tolerancia a cross-tenant leak: CERO**. Un solo incidente es escenario existencial.

---

## Tu Rol

Actúa como **CTO + Arquitecto de Software Senior** con experiencia comprobada en:
- SaaS B2B multitenant sobre .NET + SQL Server en producción real.
- Blazor Hybrid con MudBlazor + MAUI (Windows + Android).
- Clean Architecture / Vertical Slice en .NET.
- Unit economics de SaaS B2B pre-break-even.
- Compliance LFPDPPP México + SAT CFDI.

Responde como arquitecto hablando con founder técnico: sin relleno, tradeoffs explícitos, cuestionando premisas cuando apliquen.

---

## Alcance de ESTE prompt (P1)

Entregar el **Architecture Decision Record (ADR)** fundacional del proyecto. Este documento será la "constitución" que todos los siguientes prompts respetarán.

**NO incluir en esta respuesta**:
- DDL de base de datos (eso es P3).
- Código de componentes específicos (eso es P4-P7).
- Implementación de aislamiento tenant (eso es P2).
- Pricing detallado con unit economics (eso es P5).

**SÍ incluir**:
- Decisiones arquitectónicas de alto nivel con justificación.
- Arquitectura C4 (niveles 1 y 2).
- Estructura completa de la solución .sln.
- Decisión de estrategia multitenant (matriz + elección).
- Decisión de patrón arquitectónico (Clean vs Vertical Slice vs Híbrido).
- Roadmap de 8 semanas de alto nivel.
- Top 10 riesgos técnicos.
- Scope cuts explícitos fuera de MVP.

---

## Formato de Respuesta Esperado

### 1. Assumptions a validar
3–7 cosas que cuestionarías al founder antes de mover código. Incluye al menos: (a) asunción sobre tamaño de equipo, (b) asunción sobre internet en talleres, (c) asunción sobre SLA requerido, (d) asunción sobre geografía de deployment.

### 2. Arquitectura C4 Nivel 1 (Contexto)
Diagrama textual tipo ASCII mostrando:
- Sistema central (TallerPro).
- Actores: Dueño/Admin, Asesor, Mecánico, Almacenista, Contador, Cliente Final, Super-Admin del SaaS.
- Sistemas externos: Stripe, Novita AI, SW Sapien, Meta WhatsApp, Brevo, Telegram, SMS provider, Cloudflare.

### 3. Arquitectura C4 Nivel 2 (Contenedores)
Diagrama textual detallando:
- Marketing site (MVC .NET 9).
- Cliente Hybrid (MAUI + BlazorWebView Windows + Android).
- API Backend (ASP.NET Core + SignalR).
- Admin Portal (MVC + MudBlazor).
- SQL Server multitenant.
- Seq (logs).
- Redis (SignalR backplane + cache).
- File storage (Cloudflare R2 o Azure Blob).
- Relaciones y protocolos entre contenedores.

### 4. Estructura de la Solución .sln
Árbol completo con propósito de cada proyecto. Mínimo debe incluir:
- `TallerPro.Domain`
- `TallerPro.Application`
- `TallerPro.Infrastructure`
- `TallerPro.Shared`
- `TallerPro.Components`
- `TallerPro.Hybrid`
- `TallerPro.Api`
- `TallerPro.LocalDb`
- `TallerPro.Web` (marketing)
- `TallerPro.Admin` (super-admin)
- `TallerPro.Observability`
- `TallerPro.Security`
- `TallerPro.Analyzers` (Roslyn analyzers)
- `tests/TallerPro.*.Tests` (Domain, Application, Integration, Isolation, E2E)

### 5. Decisión de Patrón Arquitectónico
Elige entre Clean Architecture, Vertical Slice, Onion, N-tier, o híbrido. Justifica con 3 razones operativas específicas al caso. Menciona al menos una alternativa descartada con razón.

### 6. Decisión de Multitenancy
Matriz comparativa:

| Opción | Aislamiento | Costo/tenant | Migraciones | Ruido vecino | Sharding |
|---|---|---|---|---|---|
| A: Single DB + TenantId discriminator + EF Global Query Filters | | | | | |
| B: Schema-per-tenant | | | | | |
| C: Database-per-tenant | | | | | |
| D: Híbrido (pool + dedicada para enterprise) | | | | | |

Decisión para MVP + roadmap de evolución hacia GA (1000+ tenants).

### 7. Modelo de Auth Recomendado
Elige entre: ASP.NET Core Identity + Duende IdentityServer, Microsoft Entra External ID, Auth0, Keycloak self-hosted. Justifica con costo y capacidad multi-taller.

### 8. Decisión de Framework del Cliente Hybrid
Confirma Blazor Hybrid (MAUI + BlazorWebView) con targets Windows + Android. Estructura de handlers específicos por plataforma. Estrategia de distribución (MSIX + feed propio; APK + Play Console canal cerrado).

### 9. Decisión de Backend API Style
MVC clásico vs Minimal APIs vs híbrido (MVC para admin, Minimal APIs para cliente Hybrid). Justifica.

### 10. Roadmap 8 Semanas MVP (Alto Nivel)
Sprints de 1 semana cada uno:

| Sprint | Objetivo principal | Entregables clave | DoD |
|---|---|---|---|
| 1 | Foundation | Setup .sln, Roslyn analyzer, isolation test suite base, CI/CD | Builds pasan, isolation tests vacíos corren en CI |
| 2 | ... | ... | ... |
| ... | ... | ... | ... |
| 8 | Pre-launch | Pruebas con tenants piloto, dashboard admin, emails | 3 tenants piloto activos |

El aislamiento tenant + observabilidad + admin portal son **foundational** (Sprint 1-3), no se dejan para el final. Equipo: 3 devs + 1 PM.

### 11. Top 10 Riesgos Técnicos
Tabla con Probabilidad × Impacto × Mitigación × Owner.

Incluye al menos:
1. Cross-tenant data leak (CRÍTICO — escenario existencial).
2. Meter reconciliation drift Stripe ↔ local.
3. Sync offline corrupción de datos.
4. SW Sapien outage bloqueando facturación.
5. Novita API cost explosion.
6. Impersonation abuse por super-admin.
7. PII leak en logs.
8. Multi-branch data corruption.
9. Stripe webhook replay attack.
10. Blazor Hybrid offline desync > 24h.

### 12. Scope Explícitamente FUERA de MVP
Lista de features/capacidades que NO se incluyen en las 8 semanas iniciales y por qué. Mínimo debe mencionar:
- Stripe Connect / Mercado Pago Marketplace (Nivel 2 pagos).
- OBD-II dongle integration.
- Integración con Contpaqi/Aspel export.
- App iOS (solo Android en MVP para mecánico).
- Multi-idioma (solo es-MX en MVP).
- White-label / reseller program.
- API pública para integradores externos.
- Forecast de inventario con ML.NET (solo alertas simples en MVP).

### 13. Decisiones Críticas Abiertas
Lista 3–5 decisiones que quedan abiertas y requieren validación del founder antes de empezar Sprint 1. Ejemplo: "¿RFC emisor CFDI por sucursal o por tenant?" o "¿Tenant puede tener usuarios desactivados manteniendo licencia, o se recupera el seat?".

---

## Restricciones de la Respuesta

- Cuestiona ambigüedades antes de asumir.
- Cada decisión: alternativa descartada con razón.
- Prioriza tablas y diagramas ASCII sobre prosa.
- Cada decisión relevante viene con costo mensual estimado cuando aplique.
- Nivel: un dev senior debe poder empezar Sprint 1 leyendo solo esto.
- NO escribas código C# ni DDL en esta respuesta (eso es P2-P7).
- Longitud target: ~8,000–10,000 tokens de respuesta.

---

## Al final de tu respuesta

Genera un bloque resumen titulado **"ADR Key Decisions Summary"** en markdown con máximo 20 bullets que capturen las decisiones arquitectónicas clave. Este bloque será referenciado como contexto en los siguientes 6 prompts de esta serie.
