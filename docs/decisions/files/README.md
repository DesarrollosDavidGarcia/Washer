# TallerPro SaaS — Prompts Divididos por Feature

Paquete de 7 prompts encadenados para generar la arquitectura completa del SaaS
de gestión de talleres mecánicos (.NET + Blazor Hybrid + MudBlazor + Stripe + IA).

Cada prompt es autocontenido (incluye el contexto mínimo necesario) y produce
un entregable aislado que cabe en una sola sesión de Claude/GPT/DeepSeek.

---

## Orden de ejecución

### Secuencia mínima (11 sesiones)

```
P1 (Fundaciones)
 │
 ├── P2 (Aislamiento Tenant)      ← puede correr en paralelo con P3
 ├── P3 (Base de Datos)            ← puede correr en paralelo con P2
 │
 ├── P4 (Auth + Super-Admin)       ← requiere P1, P2, P3
 ├── P5 (Pagos + Usage Tracking)   ← requiere P1, P3
 ├── P6 (IA + Catálogos + Obs.)   ← requiere P1, P3
 ├── P7 (Marketing + Signup)       ← requiere P1, P5
 ├── P9 (Docker Multi-Ambiente)    ← puede correr en paralelo con P4-P7
 ├── P10 (Testing Strategy)        ← después de P2-P9
 ├── P11 (5 Planes + Feature Flags) ← requiere P3, P5 — afecta todo
 └── P12 (Enterprise VPS Dedicada) ← requiere P11, P9 — el más complejo
```

### Modalidad "una a una" (más seguro, más lento)

Ejecuta P1 → P2 → P3 → P4 → P5 → P6 → P7 secuencialmente. Cada sesión nueva
con respuesta anterior pegada como contexto si el modelo lo requiere.

### Modalidad "paralelo rápido" (más eficiente)

1. Día 1: ejecuta P1. Revisa/aprueba.
2. Día 2: abre 2 sesiones paralelas, ejecuta P2 y P3.
3. Día 3: abre 5 sesiones paralelas, ejecuta P4, P5, P6, P7 y P9.
4. Día 4: ejecuta P10 (testing) y P11 (planes) en paralelo.
5. Día 5: ejecuta P12 (Enterprise VPS) con contexto de P11 y P9.

Total: 5 días de trabajo del arquitecto LLM en lugar de 11.

---

## Contenido de cada prompt

| # | Nombre | Entregables principales | Tokens estimados respuesta |
|---|--------|-------------------------|----------------------------|
| P1 | Fundaciones | C4, estructura .sln, convenciones, multitenancy decision, roadmap 8s, riesgos, scope MVP | ~8-10k |
| P2 | Aislamiento Tenant | 8 capas código, Roslyn Analyzer, Test Suite, Runbook, Threat Model | ~15k |
| P3 | Base de Datos | DDL SQL Server completo (plataforma + tenant + branch + usage + admin), RLS policies, índices, seed | ~10-12k |
| P4 | Auth + Impersonation + Super-Admin | ITenantContext middleware, impersonation flow, banner MudBlazor, admin portal, Cloudflare Access, emails | ~12-14k |
| P5 | Pagos Stripe + Usage Tracking | UsageTracker + Stripe Meters + dashboards + pricing validado + unit economics | ~13-15k |
| P6 | IA + Catálogos + Observabilidad | Catálogos genéricos, Novita integration, Serilog + Seq + alerts, PII masking | ~12-14k |
| P7 | Marketing + Onboarding + Integraciones | Marketing MVC, pricing page + calculadora, signup → webhook, SW Sapien/WhatsApp/SMS, sync offline, CI/CD básico | ~10-12k |
| P9 | Docker Multi-Ambiente (dev/staging/prod) | Dockerfiles optimizados, docker-compose por ambiente, secrets con sops+age, scripts ops, CI/CD completo, Caddy, mocks, runbook | ~12-14k |
| P10 | Testing Strategy Unificada | Pirámide, TestFixtures, unit/integration/E2E/load/chaos/contract/mutation tests, quality gates, CI pipelines, runbook | ~13-15k |
| P11 | **5 Planes + Feature Flags** | Schema expandido, IPlanFeatureService, FeatureGate MudBlazor, seed de planes, Stripe provisioning, upgrade/downgrade handlers, UI tenant + admin | ~13-15k |
| P12 | **Enterprise VPS Dedicada** | Terraform/Hetzner provisioning, Ansible playbook, Caddy con dominio custom, log shipping centralizado, admin dashboard fleet, backup DR, runbook ops | ~13-15k |

**Total**: 11 sesiones × ~13k tokens promedio = ~143k tokens de arquitectura
accionable. Imposible en una sola sesión pero manejable en 11.

---

## Cómo ejecutar cada prompt

1. Abre una sesión nueva en Claude (claude.ai) o tu herramienta preferida.
2. Copia el contenido completo del archivo `PN-*.md`.
3. Pégalo como primer mensaje.
4. Espera la respuesta (probablemente 5-15 minutos para completarse).
5. Guarda la respuesta en `responses/PN-respuesta.md`.
6. Revisa contra los criterios de aceptación del prompt.
7. Si hay inconsistencias o faltantes, haz follow-ups en la misma sesión.

**Modelo recomendado**: Claude Opus 4.7 o GPT-5 (mejor razonamiento arquitectónico).
**Modelo económico**: DeepSeek V4 vía Novita (consistente con tu stack de IA propio — dogfooding).

---

## Después de ejecutar los 7 prompts

Tendrás:
- Arquitectura C4 completa.
- DDL ejecutable para crear la DB.
- Código C# de los componentes críticos (middleware, handlers, integrations).
- Runbooks operacionales (incident response, super-admin ops).
- Unit economics calculados.
- Roadmap accionable de 8 semanas.

## P8 — Consolidador (opcional, 9ª sesión)

Después de tener las 8 respuestas guardadas (P1-P7 + P9), ejecuta **`P8-consolidador-repo.md`**
en una sesión aparte pegando las respuestas (o sus "ADR Updates" resumidos)
como contexto. Genera:

- `ARCHITECTURE.md` unificado con TOC navegable.
- Estructura completa del repo inicial (gitignore, editorconfig, Directory.Build.props, etc.).
- 15 ADRs numerados en formato Nygard (incluyendo ADR Docker multi-ambiente).
- `README.md`, `CONTRIBUTING.md`, `SECURITY.md` raíz del repo.
- 15 tickets Sprint 1 listos para GitHub Issues con criterios de aceptación.
- Roadmap alto nivel Sprint 2-8.
- Checklist de onboarding de dev nuevo.
- Matriz de cross-references entre documentos.

**Con P8 ejecutado, el repo está listo para commit inicial y kick-off con el equipo el lunes.**

---

## Mantenimiento de coherencia entre prompts

Cada prompt referencia un "Base Context" compartido (sección al inicio que incluye
decisiones ya tomadas). Esto mantiene consistencia sin requerir que pegues
respuestas anteriores en sesiones nuevas.

Si detectas una inconsistencia entre outputs de prompts distintos, el orden de
autoridad es:
1. P1 (decisiones fundacionales — nada las contradice).
2. P2, P3 (seguridad y schema — la capa más crítica).
3. P4-P7 (features — pueden ajustarse si hay conflicto con P1-P3).
