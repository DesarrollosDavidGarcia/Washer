# Clarify — 001 bootstrap-monorepo

- **Fecha**: 2026-04-30
- **Ronda**: 1 (7 preguntas, 7 resueltas)
- **Ejes cubiertos**: configuración SDK, distribución analyzer, estructura sln, CI matrix, política de warnings, CPM, inicialización git.
- **No invocados**: `security-reviewer` (sin PII/auth/pagos en esta spec), `ui-ux-designer` (sin UI).

## Q&A

### PA-01 — Versión exacta del SDK en `global.json`

**Pregunta**: ¿`9.0.100` (primer release) o la versión feature-latest disponible en el entorno de desarrollo?

**Respuesta**: `9.0.203` + `rollForward=latestFeature`.

**Fuente**: `stack.md` prescribe "SDK `9.0.x`, roll-forward `latestFeature`"; `dotnet --version` en la máquina de desarrollo devuelve `9.0.203`. Pinear la versión exacta del entorno garantiza reproducibilidad local; `latestFeature` permite actualizaciones de parches en CIs con SDK superior sin romper clones.

**Impacto en spec**: RF-01 actualizado con versión `9.0.203`.

---

### PA-02 — Distribución del Roslyn analyzer

**Pregunta**: ¿`ProjectReference` directo o NuGet local con `packages.lock.json`?

**Respuesta**: `ProjectReference` directo desde `Directory.Build.props` (Opción A).

**Fuente**: `stack.md` dice "Roslyn Analyzer propio (`TallerPro.Analyzers` TP0001-TP0005) inyectado vía `Directory.Build.props`". Spec RF-03 ya prescribe `OutputItemType=Analyzer; ReferenceOutputAssembly=false`. Documentación `P8-consolidador-repo.md` confirma `ProjectReference`. La simplicidad supera el versionado independiente para un analyzer interno.

**Impacto en spec**: RF-03 sin cambio (ya estaba correcto).

---

### PA-03 — `TallerPro.Hybrid` en el `.sln`

**Pregunta**: ¿Incluir en `TallerPro.sln` desde el día 1 o sln separado?

**Respuesta**: Incluir en `TallerPro.sln` desde el inicio. En CI, el step de compilación de Hybrid es condicional a `runner.os == 'Windows'`.

**Fuente**: `constitution.md` §Restricciones técnicas: "Blazor Hybrid (MAUI Windows + Android)" es core, no opcional. El monorepo es único (`P8`). La fricción en Linux se mitiga con el condicional de CI, no excluyendo el proyecto del sln (lo que rompería la navegación de IDE para todos los devs).

**Impacto en spec**: RF-08 actualizado con el condicional de CI.

---

### PA-04 — Matrix de CI desde día 1

**Pregunta**: ¿Solo `ubuntu-latest` inicialmente o ambos runners desde el primer commit?

**Respuesta**: `ubuntu-latest` + `windows-latest` desde el primer commit (Opción B).

**Fuente**: RF-08 ya prescribía "Matrix sobre ubuntu-latest (obligatorio) y windows-latest (para Hybrid Windows target)". La decisión era implícita; la resolución de PA-03 (Hybrid en sln) la hace obligatoria. El coste de minutos de GitHub Actions es aceptable dado el presupuesto del proyecto.

**Impacto en spec**: RF-08 ya lo cubría; se clarifica el condicional de Hybrid.

---

### PA-05 — `TreatWarningsAsErrors=true` global

**Pregunta**: ¿Desde el bootstrap o diferido?

**Respuesta**: Desde el bootstrap, en `Directory.Build.props` (Opción A).

**Fuente**: `constitution.md` §Convenciones de calidad: "`Nullable enable` + `TreatWarningsAsErrors` en todos los proyectos" — listado como principio de calidad no negociable. `stack.md`: "`TreatWarningsAsErrors=true`" en la lista de propiedades de compilación. No hay margen de diferirlo.

**Mitigación de warnings transitivos**: los paquetes NuGet de `Directory.Packages.props` se fijarán a versiones estables que no emitan warnings en .NET 9. Si algún paquete los produce, se suprime con `<NoWarn>` explícito por proyecto (nunca globalmente).

**Impacto en spec**: RF-03 sin cambio (ya lo tenía).

---

### PA-06 — `GlobalPackageReference` para herramientas externas

**Pregunta**: ¿Solo CPM clásico o también StyleCop/SonarAnalyzer como `GlobalPackageReference`?

**Respuesta**: Solo CPM clásico + analyzer propio vía `ProjectReference` (Opción A).

**Fuente**: `stack.md` no menciona StyleCop ni SonarAnalyzer. El analyzer propio (`TallerPro.Analyzers` con TP0001-TP0005) cubre las reglas de calidad específicas del proyecto. Añadir herramientas externas en el bootstrap introduce ruido de warnings ajenos antes de tener código real. Se pueden agregar en una spec posterior si se detecta necesidad.

**Impacto en spec**: RF-04 sin cambio.

---

### PA-07 — `git init` como parte de la spec

**Pregunta**: ¿`git init` en esta spec o manual por el usuario?

**Respuesta**: `git init` como parte de esta spec, rama `main`, primer commit (Opción A). RF-11 añadido.

**Fuente**: El repo está actualmente en estado "Is a git repository: false". GitHub Actions CI (RF-08) requiere repositorio git con historial. El flujo `/speckit.implement` debe dejar el repo en estado funcional end-to-end, incluyendo que el primer `git push` al remote sea posible.

**Impacto en spec**: RF-11 añadido. El primer commit incluirá todos los archivos creados por RF-01..RF-10.

---

## Cambios aplicados a spec.md en esta ronda

- RF-01: versión `9.0.203` especificada.
- RF-06: marcado `[x]` como cubierto por spec 002 (implementada 2026-04-30).
- RF-08: matrix biplatforma clarificada con condicional de Hybrid.
- RF-11: añadido (git init + rama main + primer commit).
- Sección "Preguntas abiertas": todos los PA marcados `[x]`.
- Sección "Historial de clarificaciones": añadida.
