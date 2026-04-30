# Spec: Bootstrap del monorepo TallerPro

- **ID**: 001
- **Estado**: implemented
- **Fecha**: 2026-04-22

## Resumen

Crear la solución `.sln` de TallerPro con 13 proyectos en `src/` y 5 suites en `tests/`, configuración central de compilación (`Directory.Build.props`, `Directory.Packages.props`, `global.json`), `.editorconfig` con reglas del analyzer propio (`TP0001`-`TP0005`) y `README.md` raíz. El objetivo es dejar un repo clonable que compila verde en Windows/Linux/Mac y tiene CI mínimo corriendo — pre-requisito de cualquier feature de producto.

## Problema

- **Dolor**: `src/` y `tests/` están vacíos; no hay `.sln`, ni `Directory.Build.props`, ni `.editorconfig` con reglas del analyzer, ni `global.json`. No se puede ejecutar `dotnet build`, ni montar CI, ni abrir otra spec de producto (la constitución prohíbe tocar `src/` sin bootstrap previo).
- **Afectados**: el equipo completo (3 devs + PM + founder) bloqueados para arrancar Sprint 1. Todo subagente `dotnet-dev`/`frontend-dev`/`tester` no tiene dónde escribir código.
- **Coste de no hacerlo**: cero progreso en el roadmap P1 de 8 semanas. Sin bootstrap no se puede verificar que el Roslyn analyzer (defensa clave de aislamiento tenant) se aplica en build, ni que central package management funciona, ni que CI falla PRs con warnings.

## Casos de uso

- **Actor**: dev del equipo (founder, 3 devs) + agentes LLM (`dotnet-dev`, `tester`, `frontend-dev`).
- **Escenarios**:
  1. **Dado** un dev clona el repo en Windows/Linux/Mac con .NET 9 SDK instalado, **cuando** ejecuta `dotnet restore && dotnet build`, **entonces** la solución completa compila sin errores ni warnings en < 60s.
  2. **Dado** un dev abre el `.sln` en Visual Studio 2022 / Rider / VS Code, **cuando** carga los 13+5 proyectos, **entonces** el IDE reconoce referencias, analyzer inyectado, y reglas `.editorconfig` activas.
  3. **Dado** un PR con código que rompe una regla del analyzer (p. ej. `Console.WriteLine` fuera de `TallerPro.Observability`), **cuando** CI corre `dotnet build`, **entonces** el pipeline falla con diagnóstico `TP0005`.
  4. **Dado** un dev añade una dependencia NuGet, **cuando** edita solo `Directory.Packages.props` (sin `<Version>` en el `.csproj`), **entonces** la versión se aplica a todos los proyectos que la referencian.
  5. **Dado** un dev ejecuta `dotnet test` tras bootstrap, **cuando** las 5 suites están vacías, **entonces** corre 0 tests con exit code 0 (suites descubribles pero sin asserts).

## Requisitos funcionales

- [ ] RF-01: Crear `global.json` en la raíz fijando SDK `9.0.203` con `rollForward=latestFeature`. (PA-01: versión instalada en la máquina de desarrollo; `latestFeature` permite actualizaciones de parches sin romper clones con SDK superior.)
- [ ] RF-02: Crear `TallerPro.sln` con 13 proyectos `src/` + 5 proyectos `tests/` organizados en solution folders (`src`, `tests`).
- [ ] RF-03: Crear `Directory.Build.props` en raíz con propiedades comunes (`TargetFramework=net9.0`, `LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild=true`) + `ProjectReference` al analyzer propio con `OutputItemType=Analyzer` y `ReferenceOutputAssembly=false` (excepto en el propio proyecto `TallerPro.Analyzers`).
- [ ] RF-04: Crear `Directory.Packages.props` con `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` y versiones semilla de los paquetes confirmados en `stack.md` (Mediator, Mapster, FluentValidation, Polly, Serilog + sinks, Stripe.net, EF Core, MudBlazor, xUnit, Shouldly, NSubstitute, bUnit, Testcontainers, Respawn).
- [ ] RF-05: Crear `.editorconfig` en raíz con (a) reglas de naming C# estándar, (b) `dotnet_diagnostic.TP0001.severity=error`, (c) `TP0002.severity=error`, (d) `TP0003.severity=error`, (e) `TP0004.severity=error`, (f) `TP0005.severity=warning`, (g) indentación y line endings.
- [x] RF-06: ~~Crear los 13 `.csproj` en `src/`~~ **Cubierto por spec 002** (`specs/002-scaffold-src-projects`, implementada 2026-04-30). Los 13 `.csproj` existen, `verify.sh` exit 0.
- [ ] RF-07: Crear los 5 `.csproj` en `tests/` con SDK `Microsoft.NET.Sdk`, referencias a xUnit runner, Shouldly, NSubstitute, y el proyecto `src/` correspondiente.
- [ ] RF-08: Crear `.github/workflows/ci.yml` mínimo que dispara en `push` y `pull_request` a `main`: checkout → setup-dotnet 9 → `dotnet restore` → `dotnet build --no-restore -c Release` → `dotnet test --no-build -c Release`. Matrix: `ubuntu-latest` (obligatorio, compila todo salvo Hybrid Windows) + `windows-latest` (compila Hybrid; step de Hybrid condicional a `runner.os == 'Windows'`). (PA-03, PA-04: Hybrid en sln único desde día 1; CI biplatforma desde el primer commit.)
- [ ] RF-09: Crear `README.md` raíz con: elevator pitch TallerPro, badges (CI status), quickstart (`git clone && dotnet build && dotnet test`), links a `ARCHITECTURE.md` (pendiente), `.specify/memory/constitution.md`, `docs/decisions/`, y CLAUDE.md principal.
- [ ] RF-10: Stub mínimo de `TallerPro.Analyzers` que declare las cinco reglas `TP0001`-`TP0005` como `DiagnosticDescriptor` registradas (sin lógica de análisis real aún) para que `.editorconfig` reconozca los IDs y CI no falle por IDs desconocidos. La lógica real se implementa en una spec posterior.
- [ ] RF-11: `git init` en raíz, rama `main`, primer commit con todos los archivos de bootstrap. (PA-07: repo actualmente no es git; GitHub Actions requiere repositorio git desde el primer commit.)

## Requisitos no funcionales

- **Rendimiento**: `dotnet restore` < 30s con caché caliente; `dotnet build` solución completa < 60s en hardware dev estándar; CI pipeline total < 5 min.
- **Seguridad**: sin secretos comiteados. `.gitignore` ya existente cubre `appsettings.*.json` locales. CI no imprime variables de entorno sensibles.
- **Observabilidad**: build logs de CI legibles, warnings listados; diagnósticos del analyzer visibles en Problems panel de IDEs.
- **Accesibilidad**: N/A (sin UI en esta spec).
- **Reproducibilidad**: `global.json` + CPM garantizan builds idénticos entre devs y CI.
- **Multi-plataforma**: compila en Windows y Linux. `TallerPro.Hybrid` solo compila en runner Windows para target Windows; target Android compila en cualquier OS con workload instalado.

## Criterios de aceptación

- [ ] CA-01: `git clone <repo> && cd Washer && dotnet restore && dotnet build` termina con exit code 0 y cero warnings en Ubuntu-latest y Windows-latest.
- [ ] CA-02: `dotnet test` descubre las 5 suites, ejecuta 0 tests, exit code 0.
- [ ] CA-03: Un commit que introduce `Console.WriteLine("x")` en `TallerPro.Api/Program.cs` hace que `dotnet build` reporte `TP0005` como warning visible (el stub del analyzer lo detecta aunque sea por un análisis simple).
- [ ] CA-04: Abrir `TallerPro.sln` en Visual Studio 2022 17.12+ carga los 18 proyectos sin errores de carga y el Error List muestra los `DiagnosticDescriptor` de `TP0001`-`TP0005` reconocidos.
- [ ] CA-05: GitHub Actions `ci.yml` corre verde en el PR que introduce el bootstrap.
- [ ] CA-06: Ningún `.csproj` declara `<PackageReference ... Version="..."/>`; todas las versiones están en `Directory.Packages.props`.
- [ ] CA-07: `dotnet format --verify-no-changes` pasa.
- [ ] CA-08: Cada uno de los 13 proyectos `src/*` y 5 `tests/*` conserva su `CLAUDE.md` ya creado (no se sobrescribe).

## Fuera de alcance

- Implementación real de la lógica del Roslyn analyzer `TP0001`-`TP0005` (solo stubs con `DiagnosticDescriptor` registrados). Ver spec posterior derivada de P2.
- Migraciones EF Core, DDL de SQL Server, seed. Ver spec derivada de P3.
- Program.cs funcional de `TallerPro.Api` con Mediator, auth, SignalR. Ver spec derivada de P1/P4.
- Setup de `TallerPro.Hybrid` con MAUI workloads (splash, iconos, code signing). Diferido a spec específica.
- `Directory.Build.targets` (solo props en esta spec).
- Dockerfiles + docker-compose. Diferido a spec derivada de P9.
- Workflows `deploy-staging.yml`, `deploy-prod.yml`, `mobile-build.yml`, `isolation-tests.yml`. Solo `ci.yml` mínimo aquí.
- Seq + Serilog setup real. Diferido a spec derivada de P6.
- Secrets management con `sops + age`. Diferido a P9.
- CHANGELOG.md, CONTRIBUTING.md, SECURITY.md, CODE_OF_CONDUCT.md, LICENSE. Diferido a siguiente spec.
- ADR-0001 a ADR-0017. Se abrirán bajo demanda conforme se arranquen specs.

## Preguntas abiertas

*Todas resueltas en clarify ronda 1 (2026-04-30). Ver `clarify.md §Q&A`.*

- [x] **PA-01** → `9.0.203` + `rollForward=latestFeature` (stack.md + `dotnet --version`).
- [x] **PA-02** → `ProjectReference` directo desde `Directory.Build.props` (stack.md, RF-03).
- [x] **PA-03** → Hybrid en `TallerPro.sln` desde día 1; CI solo compila Hybrid en `windows-latest` (constitution: Blazor Hybrid es core product).
- [x] **PA-04** → `ubuntu-latest` + `windows-latest` desde el primer commit (RF-08 ya lo prescribía).
- [x] **PA-05** → `TreatWarningsAsErrors=true` global desde el bootstrap (constitution §Convenciones, no negociable).
- [x] **PA-06** → Solo CPM clásico + analyzer propio vía `ProjectReference`. Sin StyleCop/SonarAnalyzer externos (stack.md no los menciona).
- [x] **PA-07** → `git init` como parte de esta spec (RF-11 añadido).

## Conflictos con constitución

Revisada contra `.specify/memory/constitution.md`: **sin conflictos detectados**. Esta spec es puramente habilitante — no introduce código de dominio, no toca aislamiento tenant (solo prepara el analyzer que lo enforce), respeta todas las restricciones no negociables (stack .NET 9, prohibición de `ILogger` crudo, MudBlazor 100%, Mediator/Mapster/Shouldly/NSubstitute, `TreatWarningsAsErrors=true`). Solo hay que registrar en `docs/decisions/` un ADR **ADR-0001-bootstrap-monorepo-y-cpm** al aprobar esta spec, como cobertura de la gobernanza ("Decisión arquitectónica nueva → ADR").

## Historial de clarificaciones

- **2026-04-30 — Ronda 1**: 7 preguntas abiertas (PA-01..PA-07) resueltas automáticamente desde `constitution.md`, `stack.md` y estado del entorno (`dotnet --version = 9.0.203`). RF-06 marcado como cubierto por spec 002. RF-11 añadido (git init). RF-08 precisado con matrix biplatforma. Ver `clarify.md` para Q&A literal.

## Referencias

- `.specify/memory/constitution.md` — identidad y principios.
- `.specify/memory/stack.md` — versiones de paquetes.
- `docs/decisions/files/P1-fundaciones.md` §4 Estructura de la Solución .sln.
- `docs/decisions/files/P8-consolidador-repo.md` §3 Estructura del Repo Inicial, §4 Archivos Clave, §6 Sprint 1 tickets #1, #2, #11, #14.
- `docs/decisions/files/P9-docker-multi-ambiente.md` (diferido; referencia para siguiente spec).
- `src/CLAUDE.md`, `tests/CLAUDE.md` — contratos de cada proyecto.
- `src/TallerPro.Analyzers/CLAUDE.md` — reglas `TP0001`-`TP0005` a registrar como stubs.
