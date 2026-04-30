# Analyze: Scaffold de los 13 proyectos en `src/` — iteración 7

- **Fecha**: 2026-04-29 (séptima ejecución del flujo)
- **Feature**: `specs/002-scaffold-src-projects`
- **Iteraciones previas**: 2026-04-22 (r1: H-02/H-03/H-04), 2026-04-23 r2-r6 (N-04/N-05, M-01/M-02, R-01..R-06, Q-01..Q-04/C-01/C-03/Sec-Bajo, Q-05/O-01/S-01/S-02)
- **Veredicto**: **READY_TO_IMPLEMENT** — primer cierre exitoso tras 6 rondas previas. `security-reviewer` approve; `code-reviewer` approve; `qa-reviewer` approve condicionado (2 importantes documentales no bloqueantes + 3 sugerencias).

## Cambios respecto a la iteración 6

- **Q-05/S-03 cerrados** en T-02b: Case A-full ahora tiene **2 expected literales completos embebidos** (variante `HAS_MAUI=1` con `[PASS] C9 — Hybrid restore ok` + resumen `12 passed, 0 failed, 0 skipped, 0 warnings`; variante `HAS_MAUI=0` con `[WARN] C9 — Hybrid restore skipped: MAUI workload missing` + resumen `11 passed, 0 failed, 0 skipped, 1 warnings`). `build_expected A-full` retorna `"$EXPECTED_A_FULL"` directamente sin interpolación. Simétrico con Cases A/B (cierre de C-03).
- **O-01 cerrado** en T-02 C4: validación por archivo, no por contador. Mapa proyecto → SDK explícito iterando archivos presentes con regex anclado por banda. Los totales 9/1/3 son estado final de régimen A; régimen B itera el subset presente sin comparar contra umbrales numéricos; régimen C emite `[SKIP]` global.
- **S-01 cerrado** en T-02b: `git init` sin subshell, usa `pushd "$TMP_ROOT" > /dev/null` / `popd > /dev/null` con comentario inline didáctico (`# S-01 — sin subshell: 'exit 2' termina el script padre directamente, sin depender de la propagación del exit code del subshell vía 'set -e'. pushd/popd contienen el cambio de CWD sin crear proceso hijo.`).
- **S-02 cerrado** en T-02b: `readonly HAS_MAUI` post-H4 con nota `# S-02: prevenir mutación accidental entre cases`.

## Trazabilidad

Sin cambios estructurales. Todos los CA con mitigación dura.

| Eje | Cubierto en | Estado |
|---|---|---|
| RF-01..RF-13 → 13 `.csproj` | T-04 (7), T-05 (Components), T-06 (3 Web), T-07 (Hybrid), T-08 (Analyzers) | OK |
| RF-14 (Nullable + ImplicitUsings) | Bloques canónicos plan §Componentes; T-02 C2 verifica | OK |
| RF-15 (sin PackageRef/ProjectRef) | CA-03; T-02 C3 verifica | OK |
| RF-16 (CLAUDE.md preservado) | T-01 snapshot; T-02 C7 hash compare | OK |
| RF-17 (sin sln/props/global.json) | T-02 C10 verifica | OK |
| RF-18 (manual byte-exact) | CA-10; T-04..T-08 byte-exact + T-10 review cruzada | OK (ver N-01) |
| CA-01..CA-10 | T-02 + T-09 ejecución | OK |
| Constitución §1-2 (.NET 9, Blazor Hybrid) | Plan §Arquitectura | OK |
| ADRs nuevos | D-01..D-05 sin requerir ADR (decisiones de proceso, derivadas) | OK |
| Grafo dependencias | T-01 → T-02 → T-02b → T-04..T-08 (‖) → T-09 → T-10 → T-11. Acíclico. | OK |

## Resúmenes de subagentes

### `qa-reviewer` (Sonnet)

- **Veredicto**: **`approve`** condicionado (2 importantes documentales + 3 sugerencias).
- Confirma cierre de Q-05 (Case A-full con 2 bloques `$'...'` literales completos), O-01 (T-02 C4 validación por archivo), S-01 (`pushd/popd` documentado), S-02 (`readonly HAS_MAUI`).
- Matriz CA→tests completa (CA-01..CA-10 cubiertos por C1..C12 + criterio T-04..T-08).
- **Hallazgos nuevos** — todos detectables por el harness, ninguno bloqueante:
  - **N-02 [IMPORTANTE]**: C9 tiene dos mensajes `[SKIP]` semánticamente distintos según sub-régimen — `"no .csproj to inspect (count=0)"` en régimen C (vía `should_run_check`) y `"Hybrid not yet created"` en régimen B con Hybrid ausente (interno a `check_C9()`). El criterio de hecho de T-02 no documenta la dualidad explícitamente. Riesgo: el implementador podría unificar mensajes; Case B del harness lo detectaría. **Acción para implementador**: respetar literalmente los dos mensajes en `check_C9()`.
  - **N-04 [IMPORTANTE]**: La descripción de `build_fixture` (línea 453) dice "ejecuta H1–H4 (solo la primera vez)", contradiciendo el criterio de hecho (línea 469: "`HAS_MAUI` se computa una sola vez al inicio"). H4 vive en el preludio del harness (líneas 335–343), fuera de cualquier función. Riesgo: si el implementador mueve H4 dentro del helper o la declara `local`, `build_expected A-full` falla con "unbound variable" bajo `-u`. **Acción para implementador**: H4 va en el preludio top-level, NO dentro de `build_fixture`.
  - **N-01 [SUGERENCIA]**: CA-10 ("sin `dotnet new`") sin verificación automatizada granular. Los checks C4/C5/C2 verifican el XML final pero no detectan props extra sutiles que `dotnet new` añada y que el implementador borre parcialmente. **Backlog**: T-10 ya hace review cruzada code+qa con diff directo vs canónico — basta enlazar explícitamente a CA-10.
  - **N-03 [SUGERENCIA]**: `build_fixture` no especifica estrategia de limpieza de `$TMP_ROOT` entre cases (nuevo `mktemp` por case vs. reutilización con limpieza). **Backlog**: trap EXIT cubre el último valor; recomendación: única `TMP_ROOT` recreando contenido entre cases.
  - **N-05 [SUGERENCIA]**: `quickstart.md` §Paso 8 usa `git diff --stat HEAD~1 HEAD` que falla en repos con un solo commit. **Backlog**: añadir entrada en §Troubleshooting con alternativa `sha256sum -c .../claude-md-hashes.txt`.

### `code-reviewer` (Opus)

- **Veredicto**: **`approve`** (0 bloqueantes, 0 importantes nuevos, 2 sugerencias backlog).
- Confirma O-01 (semántica completa de C4 reescrita, no solo nota), S-01 (comentario didáctico explicando porqué, no solo cambio mecánico), S-02 (nota inline sobre intención defensiva). Q-05 también confirmado.
- Convenciones .NET de T-04..T-08 conformes a constitución §1-2 y `stack.md`. Naming `TallerPro.*` consistente. Deuda técnica explícita y aceptable (ventana sin `Directory.Build.props` documentada en plan §Riesgos).
- **Hallazgos nuevos**:
  - **D-01 [SUGERENCIA backlog]**: `<AssemblyName>` se infiere del filename. `verify.sh` no exige verificación explícita; cualquier `Directory.Build.props` futuro que lo redefina debe alinearse o `verify.sh` necesita un nuevo check. **Backlog**: nota en `plan.md §Riesgos` o anexo en spec 001.
  - **D-03 [SUGERENCIA backlog]**: el historial cronológico de revisiones acumulado en T-02 y T-02b (rondas 1-7) es trazabilidad útil ahora pero lectura futura será confusa. **Backlog post-implement**: consolidar en `analyze.md`, dejar en `tasks.md` solo el contrato vigente.

### `security-reviewer` (Opus)

- **Veredicto**: **`approve`** (confirma approve de ronda 6).
- Superficie de seguridad inalterada: trap EXIT con prefix-guard `/tmp/*` intacto; `pushd/popd` no abre nuevas rutas de escritura ni race conditions; `readonly HAS_MAUI` solo previene mutación accidental, no toca validación de input externo; expected literales son data, no código.
- Sin nuevos secretos, PII, inputs no validados, ni primitivas crypto. Validación de `dotnet workload list` con regex anclada `'^[[:space:]]*maui\b'` preservada.
- I-01 (`mktemp -d -t washer-harness.XXXXXX` identificable) e I-02 (portabilidad macOS `$TMPDIR`) reservados para spec 001 (matriz CI multi-OS) — no aplicables aquí.

## Hallazgos consolidados

### Bloqueantes

_Ninguno._

### Importantes (no bloqueantes — atención al implementador)

| ID | Origen | Descripción | Detección | Acción para `dotnet-dev` |
|---|---|---|---|---|
| N-02 | qa | C9 dos `[SKIP]` distintos por sub-régimen | Case B harness | Respetar literalmente los 2 mensajes en `check_C9()` |
| N-04 | qa | `build_fixture` descripción ambigua sobre H1–H4 | Test execution | H4 en preludio top-level, NO dentro del helper |

### Sugerencias / backlog

| ID | Origen | Cuándo aplicar |
|---|---|---|
| D-01 | code | Anexo en plan 002 §Riesgos o spec 001 cuando aborde `Directory.Build.props` |
| D-03 | code | Tarea de limpieza post-T-11: consolidar historial cronológico |
| N-01 | qa | Linkear T-10 a CA-10 con diff directo vs canónico (ajuste menor en T-10) |
| N-03 | qa | Aclaración de estrategia TMP en T-02b (ajuste documental opcional) |
| N-05 | qa | Entrada en `quickstart.md` §Troubleshooting |

### Reservados (no aplicables a 002)

- **I-01, I-02 [security]**: portabilidad macOS / nombre identificable de fixture — diferidos a spec 001.
- **H-01, H-06, H-08**: dependencias de `ci.yml` y review humano de spec 001.

## Acciones

**Recomendación principal**: proceder a `/speckit.implement specs/002-scaffold-src-projects`. Los importantes N-02/N-04 son ambigüedades documentales detectables por el harness T-02b; el `dotnet-dev` recibe instrucciones explícitas en este analyze.md.

**Alternativa conservadora** (a discreción del orquestador, ~5–10 min): ronda 8 acotada para cerrar N-02 + N-04 con micro-ediciones documentales:

1. Añadir nota en T-02 §Checks bajo C9: dualidad de mensajes `[SKIP]` con texto literal exacto de cada path.
2. Reescribir descripción de `build_fixture` en T-02b eliminando "(solo la primera vez)" y aclarando que H1–H4 son inicialización global del script, no parte del helper.

Coste marginal bajo, pero no exigido por las reglas del skill (no son `Bloqueantes`).

## Constitución

Sin violaciones. Restricción técnica §1 (.NET 9), §2 (Blazor Hybrid + MudBlazor + Razor SDK), §10 (Analyzer netstandard2.0) y §"Convenciones de calidad" (Nullable enable + ImplicitUsings enable) respetadas. Ventana temporal sin `TreatWarningsAsErrors` documentada en plan §Riesgos con secuencia 002→001.

## Veredicto final

**READY_TO_IMPLEMENT — ronda 7**. Los 3 reviewers convergen en `approve`. Los 2 importantes (N-02, N-04) son riesgos documentales auto-corregibles por el harness T-02b, anotados como atención explícita al implementador en este analyze.md. Las 5 sugerencias entran al backlog post-implement.

**Observación meta**: 7 rondas. La ronda 6 cerró el último bloqueante legítimo (Q-05 + 3 importantes O-01/S-01/S-02). La ronda 7 ya no presenta bloqueantes ni reviewers en `changes-requested` — solo riesgos documentales acotados que la maquinaria de pruebas (T-02b) detecta. **Convergencia alcanzada**.

**Siguiente comando**: `/speckit.implement specs/002-scaffold-src-projects`.

**Plan de implementación** (referencia para el orquestador):

1. **Fan-out paralelo inicial**: T-01 (snapshot hashes CLAUDE.md, `dotnet-dev`) ‖ T-03 (verificar `.gitattributes`, `dotnet-dev`).
2. **T-02** (`verify.sh`, `dotnet-dev`; `code-reviewer` + `security-reviewer` paralelos al cierre).
3. **T-02b** (`test_verify.sh` harness, `dotnet-dev`; `qa-reviewer` al cierre). **Atender N-02 y N-04 al implementar.**
4. **Fan-out csproj**: T-04 (7 genéricos) ‖ T-05 (Components) ‖ T-06 (3 Web) ‖ T-07 (Hybrid) ‖ T-08 (Analyzers), `dotnet-dev` + `frontend-dev`.
5. **T-09** (`verify.sh` ejecución real tras T-02b + T-04..T-08, `qa-reviewer`).
6. **T-10** (review cruzada `code-reviewer` + `qa-reviewer`; aprovechar para N-01 → diff vs canónico explícito).
7. **T-11** (marcar spec `implemented`).
