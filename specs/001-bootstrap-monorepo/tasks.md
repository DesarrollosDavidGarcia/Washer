# Tasks: Bootstrap del monorepo TallerPro

- **Spec**: `specs/001-bootstrap-monorepo/spec.md`
- **Plan**: `specs/001-bootstrap-monorepo/plan.md`

> Tareas atómicas (1-4h). Criterio de hecho verificable. Marcar `[x]` al cerrar.
>
> **Nota TDD**: esta feature es puramente infraestructural (archivos de configuración MSBuild, stub de analyzer, CI). No hay lógica de dominio que testear con xUnit/bUnit. La verificación es estructural: `dotnet build` (0 warnings), `dotnet test` (5 suites, 0 tests, exit 0), `dotnet format --verify-no-changes`. La prueba de CA-03 (TP0005) se realiza con un archivo probe temporal en T-10.

---

## T-01 — `git init` + `global.json`

- **Descripción**: Inicializar el repositorio git con rama `main` y crear `global.json` con el pin de SDK.
- **Archivos**:
  - crea: `.git/` (git init)
  - crea: `global.json`
- **Criterio**:
  - [ ] `git status` muestra repositorio git válido en la raíz.
  - [ ] Rama activa es `main` (`git branch --show-current` → `main`).
  - [ ] `global.json` existe con contenido byte-exact:
    ```json
    {
      "sdk": {
        "version": "9.0.203",
        "rollForward": "latestFeature"
      }
    }
    ```
  - [ ] `dotnet --version` desde la raíz respeta el pin (`9.0.203` o superior en `9.0.*`).
- **Depende de**: —
- **Agente**: orquestador (comandos de shell simples)
- **Estado**: [ ]

---

## T-02 — `Directory.Build.props`

- **Descripción**: Crear el archivo de props centrales que aplica a todos los proyectos del monorepo: propiedades de compilación comunes, NuGet audit y supply-chain, inyección del Roslyn Analyzer propio.
- **Archivos**:
  - crea: `Directory.Build.props`
- **Criterio**:
  - [ ] Archivo existe con las propiedades exactas del plan §Contenido canónico:
    - `LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`
    - `TreatWarningsAsErrors=true`
    - `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild=true`
    - `NuGetAudit=true`, `NuGetAuditMode=all`, `NuGetAuditLevel=moderate`
    - `RestorePackagesWithLockFile=true`
  - [ ] `<ItemGroup>` con `<ProjectReference>` al analyzer contiene exactamente `Condition="'$(MSBuildProjectName)' != 'TallerPro.Analyzers'"`, `OutputItemType="Analyzer"`, `ReferenceOutputAssembly="false"`, `Private="false"`.
  - [ ] NO contiene `<TargetFramework>` (D-01 del plan).
  - [ ] EOL LF, sin BOM.
- **Depende de**: T-01
- **Agente**: `dotnet-dev`
- **Estado**: [ ]

---

## T-03 — `Directory.Packages.props`

- **Descripción**: Crear el archivo de Central Package Management con las versiones semilla de todos los paquetes NuGet confirmados en `stack.md`. Verificar que todas las versiones son estables y compatibles con .NET 9 en el momento de implementación.
- **Archivos**:
  - crea: `Directory.Packages.props`
- **Criterio**:
  - [ ] `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` presente.
  - [ ] Grupos de paquetes del plan §Contenido canónico cubiertos: Build tools, Test, Domain/Application, Persistencia, Observabilidad, UI, Pagos/Integraciones, Analyzer (compile-time).
  - [ ] Ninguna versión usa wildcard `*` — todas son versiones exactas (ej. `7.14.1` no `7.*`). Las versiones del plan son orientativas; el implementador fija la última estable verificada.
  - [ ] EOL LF, sin BOM.
  - [ ] `dotnet restore` (con `-p:RestorePackagesWithLockFile=false`) sobre cualquier `.csproj` individual no produce `NU1100` (paquete sin versión en CPM) para ninguno de los paquetes listados.
- **Depende de**: T-01
- **Agente**: `dotnet-dev`
- **Estado**: [ ]

---

## T-04 — `.editorconfig`

- **Descripción**: Crear el `.editorconfig` raíz con reglas de naming C#, indentación, EOL y severidades de los diagnósticos TP0001-TP0005.
- **Archivos**:
  - crea: `.editorconfig`
- **Criterio**:
  - [ ] Sección `[*]`: `end_of_line=lf`, `charset=utf-8`, `trim_trailing_whitespace=true`, `insert_final_newline=true`.
  - [ ] Sección `[*.cs]`: `indent_size=4`, reglas de naming (interfaces `I`-prefix, async `*Async`, tipos PascalCase, campos privados `_camelCase`).
  - [ ] Sección `[*.{csproj,props,targets,sln,slnf,json,yml,yaml}]`: `indent_size=2`.
  - [ ] Diagnósticos del analyzer:
    ```ini
    dotnet_diagnostic.TP0001.severity = error
    dotnet_diagnostic.TP0002.severity = error
    dotnet_diagnostic.TP0003.severity = error
    dotnet_diagnostic.TP0004.severity = error
    dotnet_diagnostic.TP0005.severity = warning
    ```
  - [ ] EOL LF, sin BOM.
- **Depende de**: T-01
- **Agente**: `dotnet-dev`
- **Estado**: [ ]

---

## T-05 — Stub de `TallerPro.Analyzers` (C# + release tracking)

- **Descripción**: Añadir los archivos C# al proyecto `TallerPro.Analyzers` ya existente (creado en spec 002): `DiagnosticDescriptors.cs`, `TallerProAnalyzer.cs` (stub con detección básica de TP0005), `AnalyzerReleases.Unshipped.md`. Modificar el `.csproj` del analyzer para añadir las referencias a `Microsoft.CodeAnalysis.CSharp` y `Microsoft.CodeAnalysis.Analyzers` (excepción documentada a CPM).
- **Archivos**:
  - crea: `src/TallerPro.Analyzers/DiagnosticDescriptors.cs`
  - crea: `src/TallerPro.Analyzers/TallerProAnalyzer.cs`
  - crea: `src/TallerPro.Analyzers/AnalyzerReleases.Unshipped.md`
  - modifica: `src/TallerPro.Analyzers/TallerPro.Analyzers.csproj` (añade `<PackageReference>` con `<VersionOverride>` — única excepción a CA-06; usar `VersionOverride` no `Version` para evitar `NU1008` bajo CPM)
- **Criterio**:
  - [ ] `DiagnosticDescriptors.cs`: clase `static internal`, 5 constantes `DiagnosticDescriptor` (TP0001-TP0005) con `id`, `title`, `messageFormat`, `category`, `defaultSeverity`, `isEnabledByDefault` per el plan §Contenido canónico.
  - [ ] `TallerProAnalyzer.cs`: clase `[DiagnosticAnalyzer(LanguageNames.CSharp)]`, `SupportedDiagnostics` retorna los 5 descriptores, `Initialize` registra `AnalyzeConsoleWriteLine` para `SyntaxKind.InvocationExpression`, la lógica detecta `Console.WriteLine` y reporta TP0005.
  - [ ] `AnalyzerReleases.Unshipped.md`: encabezado `; Unshipped analyzer release` + tabla de 5 rules **sin** bloque `Release X.Y.Z` (ese bloque solo va en `Shipped.md`; incluirlo aquí con `EnforceExtendedAnalyzerRules=true` rompe el build con RS2000/RS2001).
  - [ ] `.csproj` del analyzer compila sin errores: `dotnet build src/TallerPro.Analyzers/TallerPro.Analyzers.csproj` → exit 0.
  - [ ] EOL LF en todos los archivos nuevos/modificados.
- **Depende de**: T-01, T-02 (Directory.Build.props inyecta el analyzer, necesario verificar que no hay auto-referencia)
- **Agente**: `dotnet-dev`
- **Estado**: [ ]

---

## T-06 — 5 proyectos `tests/` (`.csproj` scaffold)

- **Descripción**: Crear los 5 directorios de test con sus `.csproj` compilables y descubribles por `dotnet test`. Sin tests aún — suites vacías. Las referencias a paquetes usan CPM (sin `<Version>`).
- **Archivos**:
  - crea: `tests/TallerPro.Domain.Tests/TallerPro.Domain.Tests.csproj`
  - crea: `tests/TallerPro.Application.Tests/TallerPro.Application.Tests.csproj`
  - crea: `tests/TallerPro.Integration.Tests/TallerPro.Integration.Tests.csproj`
  - crea: `tests/TallerPro.Isolation.Tests/TallerPro.Isolation.Tests.csproj`
  - crea: `tests/TallerPro.E2E.Tests/TallerPro.E2E.Tests.csproj`
- **Criterio**:
  - [ ] Todos tienen `<TargetFramework>net9.0</TargetFramework>`, `<IsPackable>false</IsPackable>`, `<IsTestProject>true</IsTestProject>`.
  - [ ] Paquetes base (todos): `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `coverlet.collector`, `Shouldly` — sin `<Version>`.
  - [ ] `Application.Tests` e `Integration.Tests` incluyen `NSubstitute`.
  - [ ] `Integration.Tests` e `Isolation.Tests` incluyen `Testcontainers.MsSql`, `Respawn`.
  - [ ] `ProjectReference` correcto por suite (ver plan §Variaciones por suite); rutas relativas válidas.
  - [ ] `E2E.Tests`: stub mínimo sin referencias a proyectos ni paquetes especiales (se rellenará en spec posterior).
  - [ ] Cada `.csproj` de test incluye `<NoWarn>CS8021</NoWarn>` en `<PropertyGroup>` para suprimir el warning de "no files to compile" con proyectos vacíos bajo `TreatWarningsAsErrors=true`. Sin esto, `dotnet build` falla y CA-02 es inalcanzable.
  - [ ] EOL LF, sin BOM, sin `<Version>` en ningún `<PackageReference>`.
- **Depende de**: T-01, T-03 (CPM debe existir para que el restore resuelva versiones)
- **Agente**: `dotnet-dev`
- **Estado**: [ ]

---

## T-07 — `TallerPro.sln`

- **Descripción**: Generar la solución con los 18 proyectos (13 `src/` + 5 `tests/`) organizados en solution folders `src` y `tests`. Usar `dotnet new sln` + `dotnet sln add`.
- **Archivos**:
  - crea: `TallerPro.sln`
- **Criterio**:
  - [ ] `TallerPro.sln` existe en la raíz.
  - [ ] `dotnet sln list` muestra exactamente 18 proyectos.
  - [ ] Los 13 proyectos `src/` están en la solution folder `src`.
  - [ ] Los 5 proyectos `tests/` están en la solution folder `tests`.
  - [ ] `TallerPro.Hybrid` está incluido en la solución.
  - [ ] EOL LF, sin BOM.
- **Depende de**: T-06 (los 5 test .csproj deben existir; los 13 src .csproj ya existen de spec 002)
- **Agente**: `dotnet-dev`
- **Estado**: [ ]

---

## T-08 — `dotnet restore` + `packages.lock.json`

- **Descripción**: Ejecutar `dotnet restore` sobre la solución para generar `packages.lock.json`. Verificar que CPM resuelve todos los paquetes correctamente y que el audit no reporta vulnerabilidades. Commitear el lock file.
- **Archivos**:
  - genera: `packages.lock.json` (en cada proyecto con `RestorePackagesWithLockFile=true`)
  - lee: `Directory.Build.props`, `Directory.Packages.props`, `TallerPro.sln`
- **Criterio**:
  - [ ] `dotnet restore TallerPro.Linux.slnf` → exit 0, sin errores `NU*`.
  - [ ] No hay warnings de `NuGetAudit` (ningún paquete con CVE moderada o superior).
  - [ ] `packages.lock.json` generado en cada proyecto que lo necesita (o en la raíz si se usa lock file centralizado).
  - [ ] `dotnet restore TallerPro.Linux.slnf --locked-mode` → exit 0 (el lock file es consistente con el grafo de dependencias).
  - [ ] CA-06: `grep -r '<PackageReference.*Version=' tests/ src/` solo devuelve la excepción documentada en `TallerPro.Analyzers.csproj`.
- **Depende de**: T-02, T-03, T-05, T-07
- **Agente**: `dotnet-dev`
- **Estado**: [ ]

---

## T-09 — `TallerPro.Linux.slnf`

- **Descripción**: Crear el solution filter que excluye `TallerPro.Hybrid` para el CI ubuntu y para devs sin workload MAUI.
- **Archivos**:
  - crea: `TallerPro.Linux.slnf`
- **Criterio**:
  - [ ] JSON válido con `"solution": { "path": "TallerPro.sln", "projects": [...] }`.
  - [ ] Contiene exactamente 17 proyectos (18 - 1 Hybrid = 17): 12 src + 5 tests.
  - [ ] `TallerPro.Hybrid` NO aparece en la lista.
  - [ ] `dotnet build TallerPro.Linux.slnf --no-restore -c Release` → exit 0 (usando el restore de T-08).
  - [ ] EOL LF.
- **Depende de**: T-07 (sln debe existir), T-08 (restore hecho para poder verificar)
- **Agente**: `dotnet-dev`
- **Estado**: [ ]

---

## T-10 — Verificación integral + prueba CA-03

- **Descripción**: Ejecutar la batería de verificación completa sobre el estado actual del repo: build limpio, test discovery, format check. Probar CA-03 con un archivo probe temporal que contiene `Console.WriteLine` — debe aparecer TP0005 como warning, luego eliminar el probe.
- **Archivos**:
  - lee: todos los archivos del repo
  - crea-y-elimina (temporal): `src/TallerPro.Api/Probes/TP0005Probe.cs` (solo para CA-03, se elimina antes del commit)
- **Criterio**:
  - [ ] **CA-01 (ubuntu)**: `dotnet build TallerPro.Linux.slnf -c Release --no-restore` → exit 0, **cero warnings**. Si el entorno es Windows, también ejecutar `dotnet build TallerPro.sln -c Release --no-restore` → exit 0 (CA-01 windows queda cubierto por CA-05 si el entorno es Linux/Mac).
  - [ ] **CA-02**: `dotnet test TallerPro.Linux.slnf --no-build -c Release` → 5 suites descubiertas, 0 tests ejecutados, exit 0.
  - [ ] **CA-07**: `dotnet format TallerPro.Linux.slnf --verify-no-changes` → exit 0.
  - [ ] **CA-03**: Crear `src/TallerPro.Api/Probes/TP0005Probe.cs` con `Console.WriteLine("probe");`. `dotnet build TallerPro.Linux.slnf -c Release` muestra `warning TP0005` en ese archivo. Eliminar el archivo; build vuelve a exit 0 sin warnings. Añadir `src/TallerPro.Api/Probes/` a `.gitignore` o verificar que queda excluido del staging antes de T-14.
  - [ ] **CA-08**: `git status --porcelain -- 'src/TallerPro.*/CLAUDE.md' 'tests/TallerPro.*/CLAUDE.md'` → vacío. (B-03 fix: `git diff HEAD` falla antes del primer commit; `git status --porcelain` funciona sin commits previos.)
- **Depende de**: T-04 (editorconfig para format), T-05 (analyzer para CA-03), T-08 (restore), T-09 (slnf)
- **Agente**: `dotnet-dev` (ejecución) + `qa-reviewer` (validación de criterios)
- **Estado**: [ ]

---

## T-11 — `.github/workflows/ci.yml`

- **Descripción**: Crear el workflow CI biplatforma con SHA fijos para las Actions, `permissions: contents: read`, build condicional de Hybrid, test con TRX output. También crea `.github/CODEOWNERS` con protección para `TallerPro.Analyzers` (ADR D-7).
- **Archivos**:
  - crea: `.github/workflows/ci.yml`
  - crea: `.github/CODEOWNERS`
- **Criterio**:
  - [ ] `on:` incluye `push: branches: [main]` y `pull_request: branches: [main]`. Sin `pull_request_target` (Sec-04).
  - [ ] `permissions: contents: read` a nivel de workflow.
  - [ ] Matrix: `[ubuntu-latest, windows-latest]`.
  - [ ] Actions con SHA fijo (no tags flotantes): verificar SHA actual de `actions/checkout@v4` y `actions/setup-dotnet@v4` en el momento de implementación (Sec-03).
  - [ ] `setup-dotnet` con `dotnet-version: '9.0.203'`, `dotnet-quality: 'ga'`.
  - [ ] Restore, build y test usan `TallerPro.Linux.slnf` en ubuntu y `TallerPro.sln` en windows.
  - [ ] Step de upload de artefacto TRX con `if: always()` y SHA fijo para `upload-artifact` (Sec-Medio: mismo criterio que checkout/setup-dotnet).
  - [ ] Step `dotnet format --verify-no-changes` en ubuntu presente en el YAML (Sec-Bajo fix: explícito en el canónico del plan).
  - [ ] `.github/CODEOWNERS` creado con regla para `src/TallerPro.Analyzers/`, `.github/CODEOWNERS` y `.github/workflows/` apuntando a handles reales del founder y dev senior (Sec-Bajo / ADR D-7).
  - [ ] EOL LF, YAML válido.
- **Depende de**: T-09 (slnf debe existir y estar probado), T-10 (verificación local previa)
- **Agente**: `dotnet-dev`
- **Estado**: [ ]

---

## T-12 — `README.md`

- **Descripción**: Crear el README raíz con elevator pitch, badge de CI, quickstart, links a documentación interna.
- **Archivos**:
  - crea: `README.md`
- **Criterio**:
  - [ ] Badge de CI con URL del workflow: `[![CI](https://github.com/<owner>/Washer/actions/workflows/ci.yml/badge.svg)](...)`.
  - [ ] Sección **Quickstart**: `git clone`, `dotnet restore`, `dotnet build`, `dotnet test`.
  - [ ] Links a: `specs/001-bootstrap-monorepo/spec.md`, `.specify/memory/constitution.md`, `docs/decisions/`, `CLAUDE.md`.
  - [ ] Mención de prerequisitos: .NET 9 SDK, workload MAUI opcional para `TallerPro.Hybrid`.
  - [ ] Sin URLs internas, sin secretos, sin nombres de tenants reales (Sec-05: scope público/privado del repo aún no definido).
  - [ ] EOL LF.
- **Depende de**: T-11 (workflow existe para el badge URL)
- **Agente**: orquestador (documentación)
- **Estado**: [ ]

---

## T-13 — ADR-0001 → `Accepted`

- **Descripción**: Actualizar el estado del ADR-0001 (ya redactado en el plan) de `Proposed` a `Accepted` ahora que la implementación está verificada. Confirmar que las 7 decisiones están reflejadas en los archivos creados.
- **Archivos**:
  - modifica: `docs/decisions/ADR-0001-bootstrap-monorepo-y-cpm.md` (línea `Estado`)
- **Criterio**:
  - [ ] `Estado: Accepted` en la cabecera del ADR.
  - [ ] Cada decisión D-01..D-05 es verificable en los archivos implementados (cross-check rápido). (A-02 fix: el plan define D-01..D-05; las decisiones de seguridad D-6/D-7 del ADR son adicionales al plan.)
- **Depende de**: T-10 (verificación completa superada)
- **Agente**: orquestador
- **Estado**: [ ]

---

## T-14 — Primer commit git + push

- **Descripción**: Hacer `git add -A`, revisar el staging, y crear el primer commit con mensaje Conventional Commits. Verificar que no hay secretos ni archivos sensibles incluidos.
- **Archivos**:
  - lee: todos los archivos del repo (revisión del staging)
- **Criterio**:
  - [ ] `git status` muestra 0 archivos modificados después del commit.
  - [ ] Mensaje de commit: `chore: bootstrap monorepo (spec 001)`.
  - [ ] `packages.lock.json` está incluido en el commit.
  - [ ] `src/TallerPro.Api/Probes/` NO aparece en el commit (eliminado en T-10).
  - [ ] No hay archivos `.env`, `appsettings.*.json` con valores reales, ni credenciales en el staging.
  - [ ] `git log --oneline` muestra exactamente 1 commit.
- **Depende de**: T-12, T-13
- **Agente**: orquestador
- **Estado**: [ ]

---

## T-15 — Revisión: `code-reviewer` + `security-reviewer`

- **Descripción**: Revisión cruzada de los archivos de infraestructura creados: `Directory.Build.props`, `Directory.Packages.props`, `ci.yml`, analyzer stub. Verificar que no hay regresiones de seguridad ni deuda técnica introducida.
- **Archivos**:
  - lee: `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `.github/workflows/ci.yml`, `src/TallerPro.Analyzers/*.cs`
- **Criterio**:
  - [ ] `code-reviewer` entrega veredicto `approve` o `changes-requested`.
  - [ ] `security-reviewer` verifica: `permissions: contents: read` en ci.yml, SHAs fijos en Actions, `RestorePackagesWithLockFile=true`, sin `pull_request_target`, sin secretos en staging.
  - [ ] Hallazgos `Crítico`/`Alto` → tarea correctiva antes de marcar T-15 como `[x]`.
- **Depende de**: T-14
- **Agente**: `code-reviewer` + `security-reviewer` (en paralelo)
- **Estado**: [ ]

---

## T-16 — Marcar spec como `implemented`

- **Descripción**: Cambiar `Estado: draft` → `Estado: implemented` en `spec.md`. Anotar el commit SHA y resultado del build en `clarify.md §Cierre`. Marcar todas las tareas T-01..T-15 como `[x]`.
- **Archivos**:
  - modifica: `specs/001-bootstrap-monorepo/spec.md`
  - modifica: `specs/001-bootstrap-monorepo/clarify.md`
  - modifica: `specs/001-bootstrap-monorepo/tasks.md` (marcar tareas)
- **Criterio**:
  - [ ] `Estado: implemented` en `spec.md`.
  - [ ] `clarify.md §Cierre` documenta: commit SHA, fecha, `dotnet build` exit 0, `dotnet test` exit 0.
  - [ ] **CA-04 verificado manualmente**: abrir `TallerPro.sln` en VS2022 17.12+ (o Rider/VS Code con extensión C#), confirmar que 18 proyectos cargan sin errores, Error List muestra TP0001-TP0005 reconocidos. Registrar resultado en `clarify.md §Cierre` (I-02 fix: CA-04 requiere evidencia explícita antes de cerrar).
  - [ ] Todas las tareas T-01..T-15 marcadas `[x]`.
- **Depende de**: T-15
- **Agente**: orquestador
- **Estado**: [ ]

---

## Secuencia recomendada (orden topológico)

```
T-01 (git init + global.json)
  │
  ├── T-02 (Directory.Build.props) ─────────────┐
  ├── T-03 (Directory.Packages.props) ───────────┤
  ├── T-04 (.editorconfig) ─────────────────────┤
  ├── T-05 (Analyzer C# stub) ─────────────────┤
  └── T-06 (5 test .csproj) ──────────────────┐ │
                                               │ │
                              T-07 (sln) ◄─────┘ │
                                │                 │
                     T-08 (restore + lock) ◄───────┘
                                │
                    ┌───────────┤
                    │           │
               T-09 (slnf)   (espera T-04 para format)
                    │           │
                    └─── T-10 (verificación + CA-03) ──┐
                                │                       │
                           T-11 (ci.yml)           T-13 (ADR accepted)
                                │                       │
                           T-12 (README)                │
                                │                       │
                           T-14 (primer commit) ◄───────┘
                                │
                           T-15 (code-reviewer + security-reviewer)
                                │
                           T-16 (spec → implemented)
```

**Paralelos fase 1** (todos dependen de T-01, son independientes entre sí): T-02, T-03, T-04, T-05, T-06.

**Secuencia crítica**: T-01 → T-07 → T-08 → T-09 → T-10 → T-11 → T-12 → T-14 → T-15 → T-16.

## Cierre

- [ ] Todas las tareas T-01..T-16 marcadas `[x]`.
- [ ] `dotnet build TallerPro.Linux.slnf -c Release` exit 0, 0 warnings.
- [ ] `dotnet test TallerPro.Linux.slnf --no-build` exit 0, 5 suites, 0 tests.
- [ ] `packages.lock.json` commiteado.
- [ ] `code-reviewer` + `security-reviewer` → `approve`.
- [ ] ADR-0001 → `Accepted`.
- [ ] `spec.md` → `Estado: implemented`.
