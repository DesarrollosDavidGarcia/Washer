# Glosario — TallerPro

> Términos de dominio. Mantener alfabético. Una línea por término.

**ADR** — Architecture Decision Record. Documento Nygard-style en `docs/decisions/` que registra una decisión arquitectónica, alternativas descartadas y consecuencias.
**Aislamiento tenant** — invariante del sistema: ningún tenant puede leer/escribir datos de otro. Enforced en 8 capas (defense-in-depth).
**Asesor** — rol de usuario que recibe clientes y abre órdenes de servicio en una Branch.
**Branch** — sucursal física de un Tenant. Scope de Warehouses, Orders, Vehicles físicos e Inventory.
**Catálogo genérico** — tabla `{Code, Name, Description, ExtraJson}` para estructuras simples. Entidades ricas NO usan catálogo genérico.
**CFDI** — Comprobante Fiscal Digital por Internet 4.0 (SAT México). Timbrado vía SW Sapien. Inmutable.
**Cloudflare Access** — gateway Zero Trust que protege `admin.tallerpro.mx` (MFA + SSO + device posture).
**Custodia** — registro fotográfico del estado del vehículo al ingreso/salida del taller.
**Customer** — cliente final del taller (no del SaaS). Scope Tenant.
**Dueño/Admin** — rol de tenant con acceso total a su propio tenant.
**FeatureGate** — componente MudBlazor que muestra/oculta UI según el plan del tenant y flags.
**Hard delete** — eliminación física. Prohibido salvo en tablas inmutables (CFDIs, Payments, AuditLog, MeterEvents, ImpersonationAudits, InventoryMovements).
**Impersonation** — mecanismo de super-admin para actuar como un usuario tenant. Auditado en `ImpersonationAudits` (inmutable) con banner MudBlazor visible.
**LFPDPPP** — Ley Federal de Protección de Datos Personales en Posesión de Particulares (México).
**Mecánico** — rol operativo. Ejecuta órdenes. Acceso limitado en móvil.
**Mediator (Othamar)** — librería CQRS con source generators usada en TallerPro. NO confundir con MediatR (prohibido).
**Meter** — contador de uso facturable en Stripe (CFDIs emitidos, WhatsApp enviados, IA tokens, storage GB).
**MeterEvent** — evento de uso enviado a Stripe Meters. Inmutable. Reconciliado con logs locales.
**Outbox** — patrón de persistencia de mensajes salientes en cliente Hybrid para sync offline.
**PAC** — Proveedor Autorizado de Certificación (CFDI). TallerPro usa SW Sapien; alternativas: Facturama, Finkok.
**PII masking** — enmascarado de datos personales en dos capas: (1) antes de enviar a LLMs externos, (2) en Serilog antes de escribir a Seq.
**Plan** — nivel de suscripción del tenant (5 planes P11). Controla feature flags, pools incluidos y caps metered.
**Platform-level** — scope de entidades compartidas por todos los tenants (planes, catálogos del sistema, admin audit).
**Pool** — cuota incluida en un plan (ej. 500 CFDIs/mes, 1000 WhatsApp/mes). Overage se factura metered.
**RLS** — Row-Level Security de SQL Server. Última capa de defensa de aislamiento tenant.
**ServiceOrder** — orden de servicio del taller: cliente + vehículo + síntoma + diagnóstico + refacciones + pagos.
**SignalR** — realtime backplane de la app Hybrid (notificaciones de cambios de estado).
**Soft delete** — marcado lógico (`IsDeleted`, `DeletedAt`, `DeletedBy`). Default en TallerPro.
**Super-Admin** — rol de plataforma (founder + soporte futuro). Acceso cross-tenant vía `admin.tallerpro.mx` con impersonation auditada.
**Sync offline** — reconciliación entre SQLite local del Hybrid y SQL Server central cuando vuelve conectividad.
**TenantContext** — servicio scoped que resuelve `TenantId` desde JWT/cookie en cada request. Invariante del sistema.
**TP0001-TP0005** — reglas Roslyn del analyzer `TallerPro.Analyzers` que enforce el aislamiento tenant en build time.
**Warehouse** — almacén físico dentro de una Branch.
