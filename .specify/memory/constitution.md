# Project Constitution

> Principios inmutables de **TallerPro** (workspace `Washer`). La IA y el equipo deben respetarlos en cada spec, plan y tarea. Modificar requiere ADR aprobado en `docs/decisions/`.

## Identidad del proyecto

- **Nombre producto:** TallerPro
- **Workspace repo:** Washer
- **Propósito:** SaaS multitenant + multitaller para gestión de talleres mecánicos en México, escalable a LatAm (arranque Chihuahua/Bajío).
- **Meta MVP:** 10 tenants facturando en 90 días. Presupuesto infra alpha < $1,000 USD/mes.
- **Jerarquía de cuentas:** `Tenant → Branch (1..N) → Warehouses/Orders/Vehicles/Inventory`. `User 1..N → UserBranchAccess (M:N)`. `Customer 1..N` scope Tenant → `Vehicles 1..N`.

## Principios fundamentales

1. **Spec-first**: ninguna funcionalidad se implementa sin una `spec.md` aprobada.
2. **Plan antes que código**: cada spec requiere `plan.md` técnico antes de escribir código.
3. **Tareas atómicas**: el trabajo se descompone en `tasks.md` con pasos verificables.
4. **Tests obligatorios**: toda feature incluye pruebas en `tests/`; features tenant-scoped **deben** incluir test en `TallerPro.Isolation.Tests`.
5. **Decisiones trazables**: trade-offs arquitectónicos se registran como ADR en `docs/decisions/`.
6. **Tolerancia a cross-tenant leak: CERO**. Un incidente es escenario existencial.

## Restricciones técnicas no negociables

1. **.NET 9** (roll-forward a feature latest). AOT-ready cuando aplique.
2. **Blazor Hybrid** (MAUI Windows + Android) + **MudBlazor 100%** UI. CSS custom solo vía **SCSS**. Nada de raw HTML/CSS fuera de SCSS tokens.
3. **Mediator (Martin Othamar)** con source generators para CQRS. **Prohibido MediatR** (licenciamiento comercial desde jul 2025).
4. **Mapster** para mapeos. **Prohibido AutoMapper** (misma razón).
5. **FluentValidation** para validación, **Polly v8** para resiliencia, **Stripe.net** para Stripe.
6. **Serilog** como logger único → Seq self-hosted. **Prohibido** `ILogger` crudo sin Serilog y `Console.WriteLine`.
7. **Tests**: **xUnit** + **bUnit**. **Shouldly** para aserciones (no FluentAssertions). **NSubstitute** para mocks (no Moq).
8. **Soft delete estricto**. Hard delete solo en tablas inmutables: `InventoryMovements`, `CFDIs`, `Payments`, `AuditLog`, `MeterEvents`, `ImpersonationAudits`.
9. **Catálogos genéricos** solo para `{Code, Name, Description, ExtraJson}`. Entidades ricas (Parts, Customers, Vehicles) en tablas dedicadas.
10. **Aislamiento tenant** como invariante del sistema — defense-in-depth 8 capas, enforced por Roslyn Analyzer (`TallerPro.Analyzers` TP0001-TP0005) + `TallerPro.Isolation.Tests` en CI.
11. **PII masking dual**: antes de enviar a LLMs externos + en todos los logs Serilog.
12. **MFA obligatorio** para super-admin y tenant admin/owner.
13. **Cloudflare Access** (Zero Trust) para `admin.tallerpro.mx`.
14. **Integraciones externas**: Stripe Billing + Meters (pagos), DeepSeek V3.2 vía Novita (IA), SW Sapien (CFDI), Meta WhatsApp Cloud API, Brevo (email), Labsmobile (SMS), Telegram.

## Convenciones de calidad

- **Commits**: Conventional Commits (`feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`).
- **PR policy**: ≥1 reviewer; 2 para cambios en `TallerPro.Security/` o `TallerPro.Analyzers/`; founder required para impersonation/pricing.
- **Estilo**: `.editorconfig` enforced, `dotnet format` antes de commit.
- **Migraciones EF**: vía `dotnet ef migrations add`. Prohibido SQL manual en código de producción.
- **Nullable enable** + `TreatWarningsAsErrors` en todos los proyectos.

## Gobernanza

- Cambios a esta constitución → ADR en `docs/decisions/` con estado `Accepted`.
- Decisiones arquitectónicas nuevas (cambio de librería, patrón, framework) → ADR previo.
- Hallazgo de seguridad `Crítico`/`Alto` sin mitigar → bloquea `/speckit.implement`.
