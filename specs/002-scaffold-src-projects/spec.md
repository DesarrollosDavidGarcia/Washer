# Spec: Scaffold de los 13 proyectos en `src/`

- **ID**: 002
- **Estado**: implemented
- **Fecha**: 2026-04-22

## Resumen

Crear únicamente los 13 archivos `.csproj` en `src/` con su `Sdk`, `TargetFramework` y mínimas propiedades necesarias para que cada proyecto sea restaurable y reconocido por el IDE. **Sin** código fuente (`Program.cs`, `.cs`, `.razor`, recursos, `wwwroot/`), **sin** `PackageReference`, **sin** `ProjectReference` entre ellos, **sin** `.sln`, **sin** `Directory.Build.props/Packages.props`, **sin** tocar `tests/`. Un "esqueleto del esqueleto" que otras specs rellenarán.

## Problema

- **Dolor**: `src/` contiene solo `CLAUDE.md` por proyecto. No existe ningún `.csproj`, así que `dotnet new`, `dotnet sln add`, o abrir la carpeta en IDE no muestra proyectos. La spec `001-bootstrap-monorepo` pretende crear todo (csproj + sln + props + analyzer + CI + README) en un solo lote y sigue en `draft` con 7 preguntas abiertas — bloqueando cualquier avance.
- **Afectados**: founder + 3 devs + agentes `dotnet-dev`/`frontend-dev`/`tester`. Sin al menos un árbol de `.csproj` declarado, no se puede razonar sobre dependencias, ni preparar `Directory.Packages.props`, ni decidir cuestiones como "¿Hybrid va en el mismo `.sln`?" (PA-03 de 001), porque no hay proyecto al que mirar.
- **Coste de no hacerlo**: mantiene el deadlock de la spec 001. Crear solo los `.csproj` desbloquea decisiones posteriores: CPM puede listar paquetes que cada proyecto *va a* necesitar; ADR sobre sln único vs. múltiple puede basarse en el grafo real; el analyzer se puede empaquetar como stub una vez exista su propio `.csproj`.

## Casos de uso

- **Actor**: dev del equipo + agentes `dotnet-dev`/`frontend-dev`.
- **Escenarios**:
  1. **Dado** un dev clona el repo tras aplicar esta spec, **cuando** ejecuta `dotnet restore src/TallerPro.Domain/TallerPro.Domain.csproj`, **entonces** restore termina con exit 0 (proyecto vacío, sin paquetes, sin referencias → nada que bajar).
  2. **Dado** un dev abre la carpeta `src/` en Rider / VS / VS Code con C# DevKit, **cuando** el IDE indexa, **entonces** detecta 13 proyectos con su SDK y `TargetFramework` correcto y ninguno muestra error de carga.
  3. **Dado** un dev ejecuta `dotnet build src/TallerPro.Domain/TallerPro.Domain.csproj`, **cuando** no hay fuentes (`.cs`) en la carpeta del proyecto, **entonces** build termina exit 0 generando un assembly vacío (comportamiento default del SDK para proyectos sin código).
  4. **Dado** un dev inspecciona el `.csproj` de `TallerPro.Hybrid`, **cuando** lo abre, **entonces** ve `TargetFrameworks` con `net9.0-android` y `net9.0-windows10.0.19041.0` y `UseMaui=true`, sin referencias a recursos o iconos todavía.
  5. **Dado** un dev inspecciona `TallerPro.Analyzers.csproj`, **cuando** lo abre, **entonces** ve `TargetFramework=netstandard2.0`, `IsRoslynComponent=true` y `EnforceExtendedAnalyzerRules=true`, pero sin `DiagnosticAnalyzer` registrado (cero fuentes).

## Requisitos funcionales

- [ ] RF-01: Crear `src/TallerPro.Domain/TallerPro.Domain.csproj` con `Sdk=Microsoft.NET.Sdk`, `TargetFramework=net9.0`.
- [ ] RF-02: Crear `src/TallerPro.Application/TallerPro.Application.csproj` con `Sdk=Microsoft.NET.Sdk`, `TargetFramework=net9.0`.
- [ ] RF-03: Crear `src/TallerPro.Infrastructure/TallerPro.Infrastructure.csproj` con `Sdk=Microsoft.NET.Sdk`, `TargetFramework=net9.0`.
- [ ] RF-04: Crear `src/TallerPro.Shared/TallerPro.Shared.csproj` con `Sdk=Microsoft.NET.Sdk`, `TargetFramework=net9.0`.
- [ ] RF-05: Crear `src/TallerPro.Components/TallerPro.Components.csproj` con `Sdk=Microsoft.NET.Sdk.Razor`, `TargetFramework=net9.0` (decisión PA-01).
- [ ] RF-06: Crear `src/TallerPro.Hybrid/TallerPro.Hybrid.csproj` con `Sdk=Microsoft.NET.Sdk`, `TargetFrameworks=net9.0-android;net9.0-windows10.0.19041.0` (decisiones PA-02, PA-03), `UseMaui=true`, `SingleProject=true`, `OutputType=Exe`. Sin iconos/splash/appxmanifest aún. Sin iOS/MacCatalyst.
- [ ] RF-07: Crear `src/TallerPro.Api/TallerPro.Api.csproj` con `Sdk=Microsoft.NET.Sdk.Web`, `TargetFramework=net9.0`. Sin `Program.cs`.
- [ ] RF-08: Crear `src/TallerPro.LocalDb/TallerPro.LocalDb.csproj` con `Sdk=Microsoft.NET.Sdk`, `TargetFramework=net9.0`.
- [ ] RF-09: Crear `src/TallerPro.Web/TallerPro.Web.csproj` con `Sdk=Microsoft.NET.Sdk.Web`, `TargetFramework=net9.0`. Sin `Program.cs` ni `Views/`.
- [ ] RF-10: Crear `src/TallerPro.Admin/TallerPro.Admin.csproj` con `Sdk=Microsoft.NET.Sdk.Web`, `TargetFramework=net9.0`. Sin `Program.cs` ni `Views/`.
- [ ] RF-11: Crear `src/TallerPro.Observability/TallerPro.Observability.csproj` con `Sdk=Microsoft.NET.Sdk`, `TargetFramework=net9.0`.
- [ ] RF-12: Crear `src/TallerPro.Security/TallerPro.Security.csproj` con `Sdk=Microsoft.NET.Sdk`, `TargetFramework=net9.0`.
- [ ] RF-13: Crear `src/TallerPro.Analyzers/TallerPro.Analyzers.csproj` con `Sdk=Microsoft.NET.Sdk`, `TargetFramework=netstandard2.0` (decisión PA-04), `IsRoslynComponent=true`, `EnforceExtendedAnalyzerRules=true`, `IncludeBuildOutput=false`. Sin `DiagnosticAnalyzer` ni `DiagnosticDescriptor`.
- [ ] RF-14: En **todos** los `.csproj`: incluir `<Nullable>enable</Nullable>` y `<ImplicitUsings>enable</ImplicitUsings>`. No declarar `TreatWarningsAsErrors`, `LangVersion`, `AnalysisLevel` ni otras props comunes — eso corresponde a `Directory.Build.props` (spec 001).
- [ ] RF-15: **Ninguna** referencia (`PackageReference` ni `ProjectReference`) dentro de los `.csproj` de esta spec. El grafo de dependencias se resuelve en specs posteriores.
- [ ] RF-16: Preservar el `CLAUDE.md` existente en cada carpeta `src/TallerPro.*/` sin modificarlo.
- [ ] RF-17: No crear `.sln`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, ni archivos en `tests/`, `build/`, `.github/`. Esta spec es exclusivamente `src/*/TallerPro.*.csproj`.
- [ ] RF-18: Los 13 `.csproj` se escriben a mano con contenido byte-exact determinista (decisión PA-07). Prohibido usar `dotnet new classlib|web|maui|razorclasslib` como generador, ya que introduce variación por versión de plantillas del SDK local.

## Requisitos no funcionales

- **Rendimiento**: `dotnet restore` individual de cada proyecto < 3 s (sin paquetes que resolver).
- **Seguridad**: N/A. Ningún proyecto ejecuta código ni accede a red.
- **Observabilidad**: N/A en esta spec.
- **Accesibilidad**: N/A (sin UI).
- **Reproducibilidad**: cada `.csproj` debe ser idéntico byte-a-byte entre clones (no depende de plantillas `dotnet new` que varían por versión de SDK).
- **Compatibilidad workloads**: la spec **no** requiere que el dev tenga workloads MAUI/Android instalados para restaurar/compilar los 12 proyectos no-Hybrid. `TallerPro.Hybrid` requiere workload MAUI instalado; si falta, solo ese proyecto falla (aislado).

## Criterios de aceptación

- [ ] CA-01: `ls src/*/*.csproj | wc -l` devuelve exactamente `13`.
- [ ] CA-02: Cada `.csproj` contiene `<Nullable>enable</Nullable>` y `<ImplicitUsings>enable</ImplicitUsings>`.
- [ ] CA-03: Ningún `.csproj` contiene `<PackageReference ...>` ni `<ProjectReference ...>`.
- [ ] CA-04: `dotnet restore src/TallerPro.Domain/TallerPro.Domain.csproj` pasa exit 0 en Ubuntu-latest, Windows-latest y macOS-latest. Mismo resultado para los otros 11 proyectos no-Hybrid.
- [ ] CA-05: `TallerPro.Hybrid.csproj` declara exactamente `net9.0-android` y `net9.0-windows10.0.19041.0` en `<TargetFrameworks>` y `UseMaui=true`.
- [ ] CA-06: `TallerPro.Analyzers.csproj` declara `netstandard2.0` y `IsRoslynComponent=true`.
- [ ] CA-07: Los `SDK` usados son exactamente: `Microsoft.NET.Sdk` (9 proyectos), `Microsoft.NET.Sdk.Razor` (1: Components), `Microsoft.NET.Sdk.Web` (3: Api, Web, Admin).
- [ ] CA-08: Tras commit, cada carpeta `src/TallerPro.*/` contiene **exactamente** dos archivos versionados: el nuevo `.csproj` y el `CLAUDE.md` preexistente sin modificar (hash sha256 de `CLAUDE.md` inalterado). `obj/`/`bin/` quedan ignorados por `.gitignore` (decisión PA-06).
- [ ] CA-09: `tests/`, `build/`, raíz del repo — sin cambios nuevos atribuibles a esta spec.
- [ ] CA-10: Ningún `.csproj` ha sido generado por `dotnet new`; el contenido es determinista y reproducible entre clones (decisión PA-07).

## Fuera de alcance

- `.sln` (va en spec 001 o una derivada).
- `Directory.Build.props` y `Directory.Packages.props` (spec 001).
- `global.json` (spec 001).
- Cualquier `PackageReference` o `ProjectReference` (specs posteriores por capa).
- `Program.cs`, `appsettings.json`, `wwwroot/`, `Views/`, `Pages/`, `Components/`, `MauiProgram.cs`, `Platforms/` de MAUI.
- Iconos, splash, `appxmanifest`, code signing config en Hybrid.
- `DiagnosticAnalyzer`, `DiagnosticDescriptor`, reglas `TP0001`-`TP0005` reales (spec posterior) o stubs (spec 001 RF-10).
- `.github/workflows/*` y cualquier automatización CI.
- `README.md` raíz, `CHANGELOG.md`, `CONTRIBUTING.md`, etc.
- Tests unitarios o de integración, archivos en `tests/`.
- Dockerfiles, docker-compose.

## Preguntas abiertas

_(ninguna — resueltas en `clarify.md` ronda 1, 2026-04-22)_

## Conflictos con constitución y con spec 001

- **Constitución**: sin conflicto. Esta spec es estrictamente habilitante; no introduce código de dominio, ni dependencias, ni lógica. Respeta `Nullable=enable` y `ImplicitUsings=enable` (RF-14). La restricción "sin spec+plan+tasks+analyze(READY) → no tocar `src/`" se satisface creando esta spec antes de tocar `src/`.
- **Spec 001 (bootstrap-monorepo)**: solapamiento resuelto en PA-05 → **opción A: 002 precede, 001 se recorta**. Esta spec 002 ejecuta la creación de los 13 `.csproj`. La spec 001 queda vigente pero, cuando entre a su propio `/speckit.clarify`, **debe eliminar RF-06** y concentrarse en: `.sln`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, stub del analyzer (`DiagnosticDescriptor` TP0001-TP0005 registrados), `.editorconfig`, `ci.yml`, `README.md` raíz.
  - **TODO externo a 002** (se aplicará al arrancar clarify/plan de 001, no ahora): re-redactar `specs/001-bootstrap-monorepo/spec.md` para marcar RF-06 como cubierto por 002 y ajustar CA-01/CA-06 en consecuencia.

## Historial de clarificaciones

- **2026-04-22 — Ronda 1** (7/7 resueltas, ver `clarify.md`):
  - PA-01 → `Microsoft.NET.Sdk.Razor` para Components.
  - PA-02 → `net9.0-windows10.0.19041.0` para Hybrid Windows.
  - PA-03 → Solo Android + Windows en Hybrid (sin iOS/MacCatalyst).
  - PA-04 → `netstandard2.0` para Analyzers.
  - PA-05 → 002 precede, 001 se recortará eliminando RF-06.
  - PA-06 → Solo `.csproj` + `CLAUDE.md` por carpeta tras commit.
  - PA-07 → `.csproj` manuales byte-exact, sin `dotnet new`.

## Referencias

- `.specify/memory/constitution.md` §Restricciones técnicas 1-2 (.NET 9, Blazor Hybrid + MudBlazor).
- `.specify/memory/stack.md` §Runtime, §Frontend, §Backend, §Herramientas (SDKs y target frameworks por proyecto).
- `src/CLAUDE.md` — mapa de proyectos y capas.
- `src/TallerPro.Analyzers/CLAUDE.md` — reglas futuras TP0001-TP0005.
- `src/TallerPro.Hybrid/CLAUDE.md` — targets Windows/Android.
- `specs/001-bootstrap-monorepo/spec.md` RF-06 (solapamiento a resolver en clarify).
- `docs/decisions/files/P1-fundaciones.md` §4 Estructura de la Solución .sln.
- `docs/decisions/files/P8-consolidador-repo.md` §3 Estructura del Repo Inicial.
