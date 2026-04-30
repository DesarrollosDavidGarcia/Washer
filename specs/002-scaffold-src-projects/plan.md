# Plan: Scaffold de los 13 proyectos en `src/`

- **Spec**: `specs/002-scaffold-src-projects/spec.md`
- **Estado**: draft

## Arquitectura

Feature puramente estructural. **No** introduce código ejecutable, componentes, ni flujos de datos. El output son 13 archivos `.csproj` en `src/TallerPro.*/`, cada uno declarando el `Sdk` y `TargetFramework(s)` correctos con las dos props obligatorias (`Nullable=enable`, `ImplicitUsings=enable`). Sin referencias entre proyectos, sin paquetes NuGet, sin fuentes.

El plan se articula en **tres bandas** de proyectos según SDK:

1. **Banda genérica** (`Microsoft.NET.Sdk`, `TargetFramework=net9.0`) — 8 proyectos:
   `Domain`, `Application`, `Infrastructure`, `Shared`, `LocalDb`, `Observability`, `Security`, y `Analyzers` se separa por TFM distinto (ver banda 3).
2. **Banda Razor/Web** — 4 proyectos:
   - `Components` con `Microsoft.NET.Sdk.Razor` + `net9.0` (PA-01).
   - `Api`, `Web`, `Admin` con `Microsoft.NET.Sdk.Web` + `net9.0`.
3. **Banda especial** — 2 proyectos:
   - `Hybrid` con `Microsoft.NET.Sdk`, `TargetFrameworks=net9.0-android;net9.0-windows10.0.19041.0`, `UseMaui=true`, `SingleProject=true`, `OutputType=Exe` (PA-02, PA-03).
   - `Analyzers` con `Microsoft.NET.Sdk`, `TargetFramework=netstandard2.0`, `IsRoslynComponent=true`, `EnforceExtendedAnalyzerRules=true`, `IncludeBuildOutput=false` (PA-04).

Total: 8 genéricos + 1 Razor + 3 Web + 1 Hybrid + 1 Analyzers = **13 `.csproj`**.

## Componentes

| Componente | Cambio | Tipo |
|---|---|---|
| `src/TallerPro.Domain/TallerPro.Domain.csproj` | nuevo | config |
| `src/TallerPro.Application/TallerPro.Application.csproj` | nuevo | config |
| `src/TallerPro.Infrastructure/TallerPro.Infrastructure.csproj` | nuevo | config |
| `src/TallerPro.Shared/TallerPro.Shared.csproj` | nuevo | config |
| `src/TallerPro.Components/TallerPro.Components.csproj` | nuevo | config |
| `src/TallerPro.Hybrid/TallerPro.Hybrid.csproj` | nuevo | config |
| `src/TallerPro.Api/TallerPro.Api.csproj` | nuevo | config |
| `src/TallerPro.LocalDb/TallerPro.LocalDb.csproj` | nuevo | config |
| `src/TallerPro.Web/TallerPro.Web.csproj` | nuevo | config |
| `src/TallerPro.Admin/TallerPro.Admin.csproj` | nuevo | config |
| `src/TallerPro.Observability/TallerPro.Observability.csproj` | nuevo | config |
| `src/TallerPro.Security/TallerPro.Security.csproj` | nuevo | config |
| `src/TallerPro.Analyzers/TallerPro.Analyzers.csproj` | nuevo | config |

### Contenido canónico por banda (byte-exact, PA-07)

**Banda genérica** (`Domain`, `Application`, `Infrastructure`, `Shared`, `LocalDb`, `Observability`, `Security`):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

**Components** (Razor Class Library):

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

**Web** (`Api`, `Web`, `Admin`):

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

**Hybrid** (MAUI, multi-target):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net9.0-android;net9.0-windows10.0.19041.0</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

**Analyzers** (Roslyn):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <IsRoslynComponent>true</IsRoslynComponent>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

Todos los `.csproj` terminan con una línea de `LF` final (EOL `LF`, según `.editorconfig` y `.gitattributes` del repo). Sin BOM.

## Datos

No aplica. Spec habilitante sin persistencia. `data-model.md` **no se genera**.

## Contratos

No aplica. Sin APIs, sin eventos, sin interfaces. `contracts/` **no se crea**.

## Decisiones

- **D-01**: **Bootstrap incremental** — se ejecuta 002 (solo `.csproj`) antes que 001 (sln/props/CI/README). Alternativas: (a) esperar a 001 completo, (b) refundir ambas en una. Motivo: crear los `.csproj` desbloquea razonar sobre CPM, sln y ADRs posteriores sin depender de 7 PAs aún abiertas en 001. Riesgo: 001 debe recortar su RF-06 cuando entre a clarify. No requiere ADR (decisión de proceso/spec, sin impacto arquitectónico global).
- **D-02**: **Contenido byte-exact manual** (PA-07) — los 13 `.csproj` se escriben a mano. Alternativa: `dotnet new`. Motivo: plantillas del SDK local introducen variación (`Nullable` puede o no estar, orden de props cambia, comentarios varían). Consecuencia: el contenido es verificable con un diff simple.
- **D-03**: **Sin `PackageReference`/`ProjectReference`** (RF-15) — el grafo de dependencias no se introduce en esta spec. Motivo: cada referencia requiere decidir versión (CPM de 001) y grafo (ADR cuando toque). Introducirlas ahora obliga a revisar cada vez que se toque `Directory.Packages.props`. Consecuencia: los 13 proyectos compilan aislados; feature entrelazado se difiere.
- **D-04**: **`TallerPro.Hybrid` en el `.sln` único** (compat con 001) — se modela como un proyecto más. Alternativa: sln separado `TallerPro.Hybrid.sln` (PA-03 de 001). Motivo: 002 no crea el `.sln`, pero deja listo para que 001 decida; no prejuzga. Consecuencia: si 001 elige separar, solo cambia en 001, no en los `.csproj` producidos por 002.
- **D-05**: **No tocar `CLAUDE.md`** por subcarpeta (RF-16, CA-08) — los `CLAUDE.md` existentes declaran contratos ya aprobados en la fase de constitución. Motivo: cualquier modificación requeriría re-validación. Consecuencia: verificación por hash sha256.

_Ninguna decisión requiere ADR nuevo._ Todas derivan de la constitución y `stack.md` ya aprobados.

## Pruebas

No hay código ejecutable, así que **no se escriben tests xUnit/bUnit** en esta spec (`tests/` sigue vacío, RF-17). La verificación es:

- **Unit**: n/a.
- **Integración**: n/a.
- **E2E**: n/a.
- **Estructural** (automatizable en CA de spec 001, no aquí):
  1. `find src -maxdepth 2 -name '*.csproj' | wc -l` → `13`.
  2. Para cada `.csproj`, parsear XML y validar: `<Sdk>` esperado, `<TargetFramework(s)>` esperado, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, **sin** `<PackageReference>` ni `<ProjectReference>`.
  3. Para cada carpeta `src/TallerPro.<Proj>/`: `git ls-files` devuelve exactamente `CLAUDE.md` + `TallerPro.<Proj>.csproj`.
  4. `dotnet restore src/TallerPro.<Proj>/TallerPro.<Proj>.csproj` → exit 0 (sin workloads para los 12 no-Hybrid; Hybrid requiere workload MAUI y se marca opcional en esta pasada).
  5. SHA-256 de cada `CLAUDE.md` pre-aplicación == post-aplicación.
- **Manual**: ver `quickstart.md`.

## Riesgos

| Riesgo | Impacto | Mitigación |
|---|---|---|
| Dev sin workload MAUI no puede restaurar `TallerPro.Hybrid` | Medio — un proyecto de 13 falla restore en local | Documentar en `quickstart.md`; el restore de los 12 restantes es independiente; workload se pedirá en spec de Hybrid real. |
| `TreatWarningsAsErrors` no se aplica aquí → proyectos con warnings "pasan" | Bajo — props comunes llegan con 001 (`Directory.Build.props`) | Orden de merge: 002 merge → 001 merge inmediatamente después antes de cualquier feature. |
| Conflicto real con spec 001 si se implementa 001 primero | Alto — duplicación de archivos | Gobernanza ya acordada en PA-05: 001 debe recortar RF-06 en su clarify. TODO asentado en `spec.md` §Conflictos. |
| Futuras bandas (p. ej. blob storage con TF distinto) no encajan en las 3 bandas actuales | Bajo | Cualquier nuevo proyecto abre spec propia con su banda; no rompe los 13 existentes. |
| Orden de props dentro del `.csproj` diverge entre devs si lo editan a mano | Bajo | Contenido byte-exact fijado en este plan; cualquier `dotnet format --verify-no-changes` futuro detectaría drift. |
| `.gitattributes` fuerza CRLF en `.csproj` en Windows y rompe CA-07 de 001 | Bajo | Validar que `.gitattributes` normaliza `*.csproj` a `LF` (ya cubierto por `text=auto eol=lf` del archivo existente). |

## Despliegue

- **Flags**: n/a (sin runtime).
- **Backfill**: n/a.
- **Rollback**: `git revert` del commit de 002. Consecuencia inmediata: las 13 carpetas vuelven a contener solo `CLAUDE.md`. Ningún sistema externo afectado.
- **Secuencia con 001**: 002 → recorte de 001 → merge de 001. No invertir (crearía dupe de `.csproj`).

## Observabilidad

- **Logs**: n/a (sin código en ejecución).
- **Métricas**: n/a.
- **Alertas**: n/a.
- **Build logs**: cuando 001 aporte el `ci.yml`, la salida de `dotnet restore`/`dotnet build` de los 13 proyectos ya creados por 002 servirá como señal de salud.

## Seguridad

Feature sin superficie de ataque directa: no procesa input, no expone endpoints, no accede a red, no toca PII ni secretos. `security-reviewer` **no invocado** en esta fase (sin PII/auth/pagos/datos sensibles).

Amenazas residuales evaluadas:

- **Supply chain**: ningún `PackageReference` → superficie NuGet nula en este commit.
- **Secrets**: cero variables, cero claves en los `.csproj`.
- **Prompt injection / IA**: n/a.

Cuando 001 introduzca `Directory.Packages.props` sí aplicará review de supply chain.

## Artefactos generados por esta fase

- `specs/002-scaffold-src-projects/plan.md` (este archivo).
- `specs/002-scaffold-src-projects/quickstart.md`.
- **No** se genera `data-model.md`, `contracts/`, `research.md` ni ADR nuevo.

## Siguiente fase

`/speckit.tasks` sobre `specs/002-scaffold-src-projects`.
