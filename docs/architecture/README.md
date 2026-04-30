# docs/architecture/

Diagramas transversales del sistema. Formato preferido: **Mermaid** en markdown (renderizado por GitHub + Cloudflare Pages + IDEs modernos).

## Contenido esperado

| Archivo | Contenido | Fuente de verdad |
|---|---|---|
| `c4-context.md` | C4 Nivel 1 — sistema + actores + sistemas externos | P1 Fundaciones |
| `c4-containers.md` | C4 Nivel 2 — Marketing, Hybrid, API, Admin, SQL, Seq, Redis, R2 + relaciones | P1 Fundaciones |
| `c4-components-api.md` | C4 Nivel 3 — componentes internos de `TallerPro.Api` | P1 + P4 |
| `erd.md` | Entity Relationship Diagram — DDL de `TallerPro.Domain` | P3 Base de Datos |
| `signup-flow.md` | Sequence diagram — Marketing → Stripe → webhook → tenant | P7 Marketing |
| `impersonation-flow.md` | Sequence diagram — super-admin → justificación → token → banner → audit | P4 Auth |
| `tenant-isolation-layers.md` | Diagrama de las 8 capas de aislamiento defense-in-depth | P2 Aislamiento |
| `sync-offline-flow.md` | Flow — Hybrid SQLite + Outbox ↔ API central | P1 + cliente Hybrid |
| `ai-pipeline.md` | Flow — PII masking → Novita → respuesta → cache | P6 IA |
| `observability-pipeline.md` | Flow — Serilog → enrichers → Seq → alertas | P6 Observabilidad |
| `payment-and-meters.md` | Flow — uso → UsageTracker → Stripe Meters → invoice | P5 Pagos |
| `deployment-topology.md` | Topología por ambiente (alpha VPS, beta ACA, GA AKS) | P9 Docker + P12 Enterprise |

## Reglas

- **Un diagrama = un archivo**. Título en H1, breve resumen, bloque Mermaid.
- Cada diagrama cita la fuente (ADR o prompt P*) que lo originó.
- Cambios que rompen un diagrama requieren ADR explícito.
- Alt-text para accesibilidad (párrafo resumen sobre el bloque Mermaid).

## Generación

Los diagramas los redacta el subagente `ui-ux-designer` durante `/speckit.plan` de la feature correspondiente, o por ADR puntual. No se generan automáticamente a partir del código.
