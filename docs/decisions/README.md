# docs/decisions/ — Architecture Decision Records

Registro de decisiones arquitectónicas siguiendo formato **Nygard**. Plantilla en `.specify/templates/adr-template.md`.

## Estructura

```
docs/decisions/
├── README.md                      ← este archivo (índice + reglas)
├── ADR-XXXX-*.md                  ← ADRs numerados (4 dígitos zero-pad)
└── files/                         ← inputs crudos P1-P12 (referencia, NO editar)
    ├── README.md
    ├── P1-fundaciones.md
    ├── P2-aislamiento-tenant.md
    ├── P3-base-de-datos.md
    ├── P4-auth-impersonation.md
    ├── P5-pagos-usage-tracking.md
    ├── P6-ia-catalogos-observabilidad.md
    ├── P7-marketing-deployment.md
    ├── P8-consolidador-repo.md
    ├── P9-docker-multi-ambiente.md
    ├── P10-testing-strategy.md
    ├── P11-planes-y-feature-flags.md
    ├── P12-enterprise-deployments.md
    ├── planes-comerciales-utilidades.md
    ├── inventario-costos-stack.md
    └── prompt-saas-talleres-v10-final.md
```

## Cuándo abrir un ADR

- Elección de librería/framework que sustituye a otra (p. ej. Mediator vs MediatR).
- Cambio de un patrón arquitectónico establecido (Clean Architecture, CQRS, multitenancy).
- Adopción de un servicio externo nuevo (Cloudflare, Novita, SW Sapien).
- Modificación a la constitución o al stack.
- Decisión con trade-off relevante donde el equipo quiera recordar el "por qué".

## Cuándo NO abrir un ADR

- Refactor local sin impacto en arquitectura global.
- Elección de nombres, convenciones menores (esas van a `.specify/memory/` o al template de spec).
- Fixes de bugs.

## Estados

`Proposed → Accepted → Deprecated → Superseded by ADR-XXXX`

## Numeración

Formato `ADR-NNNN-slug-corto.md`. `NNNN` correlativo sin saltos. `slug` kebab-case ≤5 palabras en español o inglés consistente por proyecto (aquí: español).

## Plan inicial de ADRs derivados de P1-P12

> Se crearán conforme se arranquen las features correspondientes. NO se crean todos upfront.

| # | Slug | Origen |
|---|---|---|
| 0001 | constitucion-inicial | P1 |
| 0002 | stack-dotnet-9-blazor-hybrid | P1 |
| 0003 | mudblazor-100-ui | P1 |
| 0004 | mediator-othamar-over-mediatr | P1 + inventario-costos-stack |
| 0005 | mapster-over-automapper | P1 + inventario-costos-stack |
| 0006 | multitenancy-single-db-rls | P2 |
| 0007 | roslyn-tenant-isolation-analyzer | P2 |
| 0008 | serilog-seq-observability | P6 |
| 0009 | stripe-billing-meters | P5 |
| 0010 | deepseek-novita-ai | P6 |
| 0011 | mvc-marketing-site | P7 |
| 0012 | cloudflare-access-admin | P4 |
| 0013 | impersonation-governance | P4 |
| 0014 | offline-first-sqlite-outbox | P1 + cliente Hybrid |
| 0015 | docker-multi-ambiente | P9 |
| 0016 | cinco-planes-feature-flags | P11 |
| 0017 | enterprise-vps-dedicada | P12 |

## Reglas

- Los archivos de `files/` son **inputs crudos** (prompts originales). NO se editan; se citan.
- Cada ADR referencia las secciones relevantes de `files/P*.md`.
- ADR `Deprecated` o `Superseded` se mantiene en el árbol (historia inmutable).
