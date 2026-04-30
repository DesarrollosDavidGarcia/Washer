# Plan: Bootstrap del monorepo TallerPro

- **Spec**: `specs/001-bootstrap-monorepo/spec.md`
- **Estado**: draft

## Arquitectura

Feature puramente infraestructural. No introduce lógica de dominio, entidades, ni flujos de datos. Los outputs son:

1. Archivos de configuración MSBuild centralizados
2. Solución `.sln` con 18 proyectos (13 `src/` + 5 `tests/`)
3. Stub funcional mínimo del Roslyn Analyzer (TP0001-TP0005) con detección básica de TP0005
4. Proyectos de test compilables y descubribles por `dotnet test`, sin tests aún
5. Workflow CI biplatforma (ubuntu + windows)
6. Repositorio git inicializado con rama `main` y primer commit

## Componentes

| Componente | Archivo | Tipo | RF |
|---|---|---|---|
| SDK version pin | `global.json` | config | RF-01 |
| Solución | `TallerPro.sln` | config | RF-02 |
| Props centrales + analyzer injection | `Directory.Build.props` | config | RF-03 |
| Central Package Management | `Directory.Packages.props` | config | RF-04 |
| Estilo y diagnósticos | `.editorconfig` | config | RF-05 |
| Analyzer: descriptores | `src/TallerPro.Analyzers/DiagnosticDescriptors.cs` | code | RF-10 |
| Analyzer: implementación stub | `src/TallerPro.Analyzers/TallerProAnalyzer.cs` | code | RF-10 |
| Analyzer: release tracking | `src/TallerPro.Analyzers/AnalyzerReleases.Unshipped.md` | config | RF-10 |
| Tests: Domain | `tests/TallerPro.Domain.Tests/TallerPro.Domain.Tests.csproj` | config | RF-07 |
| Tests: Application | `tests/TallerPro.Application.Tests/TallerPro.Application.Tests.csproj` | config | RF-07 |
| Tests: Integration | `tests/TallerPro.Integration.Tests/TallerPro.Integration.Tests.csproj` | config | RF-07 |
| Tests: Isolation | `tests/TallerPro.Isolation.Tests/TallerPro.Isolation.Tests.csproj` | config | RF-07 |
| Tests: E2E | `tests/TallerPro.E2E.Tests/TallerPro.E2E.Tests.csproj` | config | RF-07 |
| Solution filter Linux CI | `TallerPro.Linux.slnf` | config | RF-08 |
| CI workflow | `.github/workflows/ci.yml` | config | RF-08 |
| README raíz | `README.md` | docs | RF-09 |
| ADR bootstrap | `docs/decisions/ADR-0001-bootstrap-monorepo-y-cpm.md` | docs | — |
| CODEOWNERS | `.github/CODEOWNERS` | config | — (Sec-Bajo: ADR D-7) |
| Git init + primer commit | — | infra | RF-11 |

### Contenido canónico por componente

#### `global.json`
```json
{
  "sdk": {
    "version": "9.0.203",
    "rollForward": "latestFeature"
  }
}
```

#### `Directory.Build.props`

> **D-01**: `<TargetFramework>` NO se declara aquí — cada proyecto de `src/` ya lo tiene (spec 002). Los proyectos de `tests/` lo declaran en su propio `.csproj`. Duplicarlo aquí conflictuaría con `TallerPro.Hybrid` que usa `<TargetFrameworks>` plural.

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <!-- NuGet supply-chain audit (Sec-01) -->
    <NuGetAudit>true</NuGetAudit>
    <NuGetAuditMode>all</NuGetAuditMode>
    <NuGetAuditLevel>moderate</NuGetAuditLevel>
    <!-- Lock file: fija el grafo de dependencias exacto (Sec-02) -->
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  </PropertyGroup>

  <!-- Inyectar TallerPro.Analyzers en todos los proyectos excepto él mismo (D-02) -->
  <ItemGroup Condition="'$(MSBuildProjectName)' != 'TallerPro.Analyzers'">
    <ProjectReference
      Include="$(MSBuildThisFileDirectory)src/TallerPro.Analyzers/TallerPro.Analyzers.csproj"
      OutputItemType="Analyzer"
      ReferenceOutputAssembly="false"
      Private="false" />
  </ItemGroup>
</Project>
```

#### `Directory.Packages.props`

Versiones semilla. El implementador verifica latest-stable compatible con .NET 9 en el momento de implementación.

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>false</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup Label="Build tools">
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.12.*" />
    <PackageVersion Include="coverlet.collector" Version="6.*" />
  </ItemGroup>

  <ItemGroup Label="Test">
    <PackageVersion Include="xunit" Version="2.9.*" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.*" />
    <PackageVersion Include="Shouldly" Version="4.2.*" />
    <PackageVersion Include="NSubstitute" Version="5.3.*" />
    <PackageVersion Include="bunit" Version="1.34.*" />
    <PackageVersion Include="Testcontainers" Version="3.10.*" />
    <PackageVersion Include="Testcontainers.MsSql" Version="3.10.*" />
    <PackageVersion Include="Respawn" Version="6.2.*" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.*" />
  </ItemGroup>

  <ItemGroup Label="Domain / Application">
    <PackageVersion Include="Mediator" Version="2.*" />
    <PackageVersion Include="Mediator.SourceGenerator" Version="2.*" />
    <PackageVersion Include="Mapster" Version="7.*" />
    <PackageVersion Include="FluentValidation" Version="11.*" />
    <PackageVersion Include="Polly" Version="8.*" />
    <PackageVersion Include="Microsoft.Extensions.Http.Polly" Version="9.*" />
  </ItemGroup>

  <ItemGroup Label="Persistencia">
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="9.*" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.*" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Tools" Version="9.*" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="9.*" />
    <PackageVersion Include="Microsoft.Data.Sqlite" Version="9.*" />
  </ItemGroup>

  <ItemGroup Label="Observabilidad">
    <PackageVersion Include="Serilog" Version="4.*" />
    <PackageVersion Include="Serilog.Extensions.Hosting" Version="8.*" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="6.*" />
    <PackageVersion Include="Serilog.Sinks.File" Version="6.*" />
    <PackageVersion Include="Serilog.Sinks.Seq" Version="8.*" />
    <PackageVersion Include="Serilog.Enrichers.Environment" Version="3.*" />
    <PackageVersion Include="Serilog.Enrichers.Thread" Version="4.*" />
  </ItemGroup>

  <ItemGroup Label="UI">
    <PackageVersion Include="MudBlazor" Version="7.*" />
  </ItemGroup>

  <ItemGroup Label="Pagos / Integraciones">
    <PackageVersion Include="Stripe.net" Version="46.*" />
    <PackageVersion Include="OpenAI" Version="2.*" />
  </ItemGroup>

  <ItemGroup Label="Analyzer (compile-time only — no runtime)">
    <PackageVersion Include="Microsoft.CodeAnalysis.CSharp" Version="4.*" />
    <PackageVersion Include="Microsoft.CodeAnalysis.Analyzers" Version="3.*" />
  </ItemGroup>
</Project>
```

> **Nota**: versiones con wildcard `x.*` deben resolverse a la última estable en el momento de implementación. Verificar con `dotnet nuget list` o NuGet.org. Ajustar si hay incompatibilidades .NET 9.

#### Test `.csproj` — plantilla base

Todos los proyectos de `tests/` siguen esta estructura (sin `<Version>` — CPM provee versiones):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="coverlet.collector" />
    <PackageReference Include="Shouldly" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../src/TallerPro.{X}/TallerPro.{X}.csproj" />
  </ItemGroup>
</Project>
```

Variaciones por suite:

| Suite | ProjectReference src | Paquetes adicionales |
|---|---|---|
| `Domain.Tests` | `TallerPro.Domain` | — |
| `Application.Tests` | `TallerPro.Application` | `NSubstitute` |
| `Integration.Tests` | `TallerPro.Api`, `TallerPro.Infrastructure` | `NSubstitute`, `Testcontainers.MsSql`, `Respawn`, `Microsoft.AspNetCore.Mvc.Testing` |
| `Isolation.Tests` | `TallerPro.Infrastructure` | `NSubstitute`, `Testcontainers.MsSql`, `Respawn` |
| `E2E.Tests` | — | — (stub vacío, sin referencias aún) |

#### Stub `TallerPro.Analyzers` — archivos C#

**`AnalyzerReleases.Unshipped.md`** (requerido por `EnforceExtendedAnalyzerRules=true`):

> **B-01 fix**: el archivo Unshipped NO puede contener un bloque `Release X.Y.Z` — eso es exclusivo de `AnalyzerReleases.Shipped.md`. El contenido correcto es solo el encabezado + tabla de rules sin número de versión.

```
; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

Rules:
    Rule ID | Category               | Severity | Notes
    --------|------------------------|----------|------
    TP0001  | TallerPro.Security     | Error    | TallerProAnalyzer
    TP0002  | TallerPro.Security     | Error    | TallerProAnalyzer
    TP0003  | TallerPro.Security     | Error    | TallerProAnalyzer
    TP0004  | TallerPro.Security     | Error    | TallerProAnalyzer
    TP0005  | TallerPro.Style        | Warning  | TallerProAnalyzer
```

**`DiagnosticDescriptors.cs`**:
```csharp
using Microsoft.CodeAnalysis;

namespace TallerPro.Analyzers;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor TP0001 = new(
        id: "TP0001",
        title: "Direct tenant ID access without global query filter",
        messageFormat: "Tenant data must be accessed through the scoped DbContext with global query filters enabled",
        category: "TallerPro.Security",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Bypassing global query filters exposes cross-tenant data leaks.");

    public static readonly DiagnosticDescriptor TP0002 = new(
        id: "TP0002",
        title: "Missing tenant scope in query",
        messageFormat: "Queries on tenant-scoped entities must include tenant filtering",
        category: "TallerPro.Security",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TP0003 = new(
        id: "TP0003",
        title: "IgnoreQueryFilters on tenant-scoped entity",
        messageFormat: "IgnoreQueryFilters() is prohibited on tenant-scoped entities",
        category: "TallerPro.Security",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TP0004 = new(
        id: "TP0004",
        title: "Raw SQL without tenant filter",
        messageFormat: "Raw SQL queries must include tenant filtering via SESSION_CONTEXT or parameterized TenantId",
        category: "TallerPro.Security",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TP0005 = new(
        id: "TP0005",
        title: "Console.WriteLine usage prohibited",
        messageFormat: "Use Serilog instead of Console.WriteLine",
        category: "TallerPro.Style",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
```

**`TallerProAnalyzer.cs`** — stub con detección básica de TP0005 para satisfacer CA-03:
```csharp
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TallerPro.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TallerProAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DiagnosticDescriptors.TP0001,
            DiagnosticDescriptors.TP0002,
            DiagnosticDescriptors.TP0003,
            DiagnosticDescriptors.TP0004,
            DiagnosticDescriptors.TP0005);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        // TP0005: Console.WriteLine stub — lógica real de TP0001-TP0004 en spec posterior
        context.RegisterSyntaxNodeAction(AnalyzeConsoleWriteLine, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeConsoleWriteLine(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Expression is MemberAccessExpressionSyntax
            {
                Name.Identifier.Text: "WriteLine",
                Expression: IdentifierNameSyntax { Identifier.Text: "Console" }
            })
        {
            context.ReportDiagnostic(
                Diagnostic.Create(DiagnosticDescriptors.TP0005, invocation.GetLocation()));
        }
    }
}
```

> El `TallerPro.Analyzers.csproj` (ya existe de spec 002) requiere dos paquetes NuGet para compilar el analyzer. Añadir en el `.csproj` usando `<VersionOverride>` (mecanismo correcto bajo CPM activo — evita `NU1008`):
> ```xml
> <ItemGroup>
>   <PackageReference Include="Microsoft.CodeAnalysis.CSharp" VersionOverride="4.12.*" />
>   <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" VersionOverride="3.11.*">
>     <PrivateAssets>all</PrivateAssets>
>     <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
>   </PackageReference>
> </ItemGroup>
> ```
> **I-03 fix**: con `ManagePackageVersionsCentrally=true`, usar `<VersionOverride>` en el `.csproj` (no `<Version>`). `<Version>` genera `NU1008`; `<VersionOverride>` permite sobreescribir la versión de CPM o declarar una versión directa sin error. **Es la única excepción permitida a RF-04/CA-06**.

#### `TallerPro.sln` — estructura

18 proyectos organizados en 2 solution folders (`src`, `tests`). Generado con `dotnet sln`:
```bash
dotnet new sln -n TallerPro
# src projects (13)
for p in Domain Application Infrastructure Shared Components Hybrid Api LocalDb Web Admin Observability Security Analyzers; do
  dotnet sln add "src/TallerPro.$p/TallerPro.$p.csproj" --solution-folder src
done
# tests projects (5)
for t in Domain.Tests Application.Tests Integration.Tests Isolation.Tests E2E.Tests; do
  dotnet sln add "tests/TallerPro.$t/TallerPro.$t.csproj" --solution-folder tests
done
```

#### `TallerPro.Linux.slnf` — solution filter para CI ubuntu

Excluye `TallerPro.Hybrid` (requiere MAUI workload en windows):

```json
{
  "solution": {
    "path": "TallerPro.sln",
    "projects": [
      "src/TallerPro.Domain/TallerPro.Domain.csproj",
      "src/TallerPro.Application/TallerPro.Application.csproj",
      "src/TallerPro.Infrastructure/TallerPro.Infrastructure.csproj",
      "src/TallerPro.Shared/TallerPro.Shared.csproj",
      "src/TallerPro.Components/TallerPro.Components.csproj",
      "src/TallerPro.Api/TallerPro.Api.csproj",
      "src/TallerPro.LocalDb/TallerPro.LocalDb.csproj",
      "src/TallerPro.Web/TallerPro.Web.csproj",
      "src/TallerPro.Admin/TallerPro.Admin.csproj",
      "src/TallerPro.Observability/TallerPro.Observability.csproj",
      "src/TallerPro.Security/TallerPro.Security.csproj",
      "src/TallerPro.Analyzers/TallerPro.Analyzers.csproj",
      "tests/TallerPro.Domain.Tests/TallerPro.Domain.Tests.csproj",
      "tests/TallerPro.Application.Tests/TallerPro.Application.Tests.csproj",
      "tests/TallerPro.Integration.Tests/TallerPro.Integration.Tests.csproj",
      "tests/TallerPro.Isolation.Tests/TallerPro.Isolation.Tests.csproj",
      "tests/TallerPro.E2E.Tests/TallerPro.E2E.Tests.csproj"
    ]
  }
}
```

#### `.github/workflows/ci.yml`

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

permissions:
  contents: read

jobs:
  build:
    name: build-and-test (${{ matrix.os }})
    strategy:
      fail-fast: false
      matrix:
        os: [ubuntu-latest, windows-latest]
    runs-on: ${{ matrix.os }}

    steps:
      - name: Checkout
        # SHA fijo de actions/checkout@v4 — sustituir con SHA vigente al implementar (Sec-03)
        uses: actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af68 # v4

      - name: Setup .NET
        # SHA fijo de actions/setup-dotnet@v4 — sustituir con SHA vigente al implementar (Sec-03)
        uses: actions/setup-dotnet@3951f0dfe7a714d19c3b9bbc22f9e4eeaed4cb64 # v4
        with:
          dotnet-version: '9.0.203'
          dotnet-quality: 'ga'
          cache: true

      - name: Restore (Linux — sin Hybrid)
        if: runner.os == 'Linux'
        # I-04 fix: --locked-mode garantiza que packages.lock.json está en sync
        run: dotnet restore TallerPro.Linux.slnf --locked-mode

      - name: Restore (Windows — solución completa)
        if: runner.os == 'Windows'
        run: dotnet restore TallerPro.sln --locked-mode

      - name: Build (Linux)
        if: runner.os == 'Linux'
        run: dotnet build TallerPro.Linux.slnf --no-restore -c Release

      - name: Build (Windows — incluye Hybrid)
        if: runner.os == 'Windows'
        run: dotnet build TallerPro.sln --no-restore -c Release

      - name: Test (Linux)
        if: runner.os == 'Linux'
        run: dotnet test TallerPro.Linux.slnf --no-build -c Release --logger "trx" --results-directory TestResults

      - name: Test (Windows)
        if: runner.os == 'Windows'
        run: dotnet test TallerPro.sln --no-build -c Release --logger "trx" --results-directory TestResults

      - name: Format check (Linux only)
        if: runner.os == 'Linux'
        # Sec-Bajo fix: step explícito en YAML canónico para CA-07
        run: dotnet format TallerPro.Linux.slnf --verify-no-changes

      - name: Upload test results
        if: always()
        # Sec-Medio fix: SHA fijo — verificar SHA vigente de actions/upload-artifact@v4 al implementar
        uses: actions/upload-artifact@65462800fd760344b1a7b4382951275a0abb4808 # v4
        with:
          name: test-results-${{ matrix.os }}
          path: TestResults/
```

> **Nota primer run**: el primer push a `main` (T-14) debe tener `packages.lock.json` ya commiteado (generado en T-08). Si CI corre antes del lock, el restore con `--locked-mode` fallará — el lock file debe estar en el commit inicial.
> **Sec-01**: `permissions: contents: read` — mínimo necesario. Sin `write` implícito.
> **Sec-02**: `RestorePackagesWithLockFile=true` en `Directory.Build.props` + `packages.lock.json` commiteado al repo. Elimina el vector de sustitución silenciosa de paquetes NuGet.
> **Sec-03**: SHAs fijos en las Actions — los SHAs del plan son orientativos; verificar los SHAs vigentes de `actions/checkout@v4` y `actions/setup-dotnet@v4` al momento de implementación. Usar Dependabot para mantenerlos actualizados.
> **Sec-04**: `pull_request_target` está **prohibido** en todos los workflows de este repo sin aprobación de `security-reviewer`. Los valores de `github.event.*` que vayan a un `run:` step deben pasarse como variable de entorno, nunca interpolarse directamente en la shell string.

#### `.github/CODEOWNERS`

Protección mínima requerida por ADR D-7. El implementador sustituye `@handle1` y `@handle2` por los handles reales de GitHub del founder y dev senior:

```
# TallerPro.Analyzers — ejecuta código en el proceso del compilador
# Requiere 2 reviewers para prevenir exfiltración desde CI
src/TallerPro.Analyzers/ @handle1 @handle2

# Archivos de seguridad críticos
.github/CODEOWNERS @handle1
.github/workflows/ @handle1 @handle2
```

#### `.editorconfig`

Secciones obligatorias:
1. `[*]` — EOL LF, indent_size=2 para xml/json/yml, indent_size=4 para .cs
2. `[*.cs]` — naming conventions C# (interfaces `I`-prefix, async `*Async`, types PascalCase, privates `_camelCase`)
3. `[*.{csproj,props,targets,sln}]` — indent_size=2, charset=utf-8
4. Diagnósticos del analyzer:
   ```ini
   dotnet_diagnostic.TP0001.severity = error
   dotnet_diagnostic.TP0002.severity = error
   dotnet_diagnostic.TP0003.severity = error
   dotnet_diagnostic.TP0004.severity = error
   dotnet_diagnostic.TP0005.severity = warning
   ```
5. Reglas IDE de Roslyn recomendadas (IDE0055 format, IDE0010 switch, etc.)

## Datos

No aplica. Spec de infraestructura sin persistencia.

## Contratos

No aplica. Sin APIs ni eventos nuevos.

## Decisiones

**D-01 — Sin `<TargetFramework>` en `Directory.Build.props`**
Los 13 `src/` ya tienen su TFM declarado (spec 002). `TallerPro.Hybrid` usa `<TargetFrameworks>` (plural) — añadir el singular en el padre crearía conflicto. Los 5 `tests/` declararán `net9.0` en su propio `.csproj`. Trade-off: ligera redundancia en cada `.csproj`, ganancia: sin conflictos MAUI.

**D-02 — Inyección del analyzer vía `Directory.Build.props` con `Condition`**
Un solo punto de configuración. La condición `MSBuildProjectName != 'TallerPro.Analyzers'` evita auto-referencia. Alternativa rechazada: `packages.lock.json` + NuGet local — más complejo sin beneficio real para analyzer interno que evoluciona junto con el código.

**D-03 — `NuGetAudit=true` + nivel `moderate` desde el bootstrap**
Detecta vulnerabilidades CVE en dependencias NuGet antes de que entre código real. Nivel `moderate` (no `low`) para evitar ruido en packages con advisories menores. Si un paquete seed tiene vulnerabilidad moderada, se actualiza la versión o se añade `<NuGetAuditSuppress>` con issue abierto.

**D-04 — `TallerPro.Linux.slnf` como solution filter para CI ubuntu**
Evita que `dotnet build` falle en ubuntu por falta de MAUI workload. Alternativa rechazada: instalar workload MAUI en ubuntu (lento, caro en minutos CI, innecesario en bootstrap). Alternativa rechazada: `continue-on-error: true` en step de Hybrid (enmascara fallos reales). El slnf también beneficia a devs de backend que no quieren MAUI.

**D-05 — Stub TP0005 con detección básica de `Console.WriteLine`**
Satisface CA-03 sin implementar TP0001-TP0004, que requieren semantic model con acceso a EF Core (aún no hay código de dominio). El stub de TP0001-TP0004 registra los `DiagnosticDescriptor` para que `.editorconfig` los reconozca y CI no falle por IDs desconocidos. La lógica real llega en una spec posterior de `TallerPro.Analyzers`.

**Excepción a CPM en `TallerPro.Analyzers.csproj`**
`Microsoft.CodeAnalysis.CSharp` y `Microsoft.CodeAnalysis.Analyzers` van con `<Version>` explícita en el `.csproj` del analyzer. Razón: los consumers del analyzer (todos los demás proyectos) no deben resolver estas dependencias transitivamente; deben ser privadas del analyzer. Esto viola CA-06 de forma documentada — es la única excepción permitida.

## Pruebas

Esta spec no introduce código de dominio ejecutable. No hay tests xUnit en `tests/` para esta spec. La verificación es el propio build de .NET:

- `dotnet build TallerPro.Linux.slnf -c Release` → exit 0, 0 warnings → CA-01 (ubuntu)
- `dotnet build TallerPro.sln -c Release` → exit 0, 0 warnings → CA-01 (windows)
- `dotnet test --no-build` → 5 suites, 0 tests, exit 0 → CA-02
- CA-03 se verifica en el PR de esta spec añadiendo un archivo `src/TallerPro.Api/TestProbe.cs` con `Console.WriteLine("x")`, verificando que build reporta TP0005, y luego eliminando el archivo antes del commit final
- CA-04 verificación manual en IDE (fuera de scope del CI)
- CA-05 verificación automática por GitHub Actions
- CA-06 verificado por `dotnet restore` (CPM falla si hay `<Version>` en `.csproj` con CPM activo)
- CA-07: `dotnet format TallerPro.Linux.slnf --verify-no-changes` en CI (agregar como step)
- CA-08 verificado implícitamente: los `CLAUDE.md` no se tocan

## Riesgos

| Riesgo | Impacto | Mitigación |
|---|---|---|
| `TreatWarningsAsErrors` rompe build por warnings de paquetes seed | Alto | Verificar 0 warnings en cada paquete antes de añadirlo; usar `<NoWarn>NU*</NoWarn>` por proyecto si necesario. `NU` prefix son warnings de NuGet packaging (no de código). |
| `NuGetAudit` reporta vulnerabilidad en paquete seed | Medio | Actualizar a versión sin CVE; si no hay, `<NuGetAuditSuppress>` + GitHub Issue abierto con plan de remediación. |
| Versiones wildcard en `Directory.Packages.props` generan drift entre dev y CI | Medio | Fijar versiones exactas (`7.14.1` no `7.*`) al momento de implementación. El wildcard en el plan es orientativo. |
| MAUI workload ausente en el runner ubuntu falla incluso con slnf | Bajo | El slnf es explícito — Hybrid no aparece en el graph de proyectos ubuntu. Verificar con `dotnet build TallerPro.Linux.slnf` localmente antes del primer PR. |
| `AnalyzerReleases.Unshipped.md` con formato incorrecto falla el build del analyzer | Bajo | Usar el formato exacto documentado en roslyn-analyzers (incluido en §Componentes de este plan). |
| Actions con tag flotante (v4) vs SHA fijo | Medio — supply chain | Sustituir tags por SHAs en las tasks de implementación. |

## Despliegue

Secuencia de implementación (topológica, por agente):

```
RF-11 (git init) → paralelo:
  RF-01 (global.json)
  RF-03 (Directory.Build.props)
  RF-04 (Directory.Packages.props)
  RF-05 (.editorconfig)
  RF-10 (Analyzer C# files en src/TallerPro.Analyzers/)
  RF-07 (5 test .csproj)
        ↓ (todos los archivos listos)
RF-02 (TallerPro.sln + dotnet sln add) → RF-08 (TallerPro.Linux.slnf + ci.yml)
        ↓
Verificación: dotnet restore → dotnet build → dotnet test → dotnet format
        ↓
RF-09 (README.md) → ADR-0001
        ↓
git commit -m "chore: bootstrap monorepo (spec 001)"
```

## Observabilidad

- **CI badge** en `README.md`: `[![CI](https://github.com/owner/Washer/actions/workflows/ci.yml/badge.svg)](https://github.com/owner/Washer/actions/workflows/ci.yml)`
- **Build logs**: `dotnet build --verbosity minimal` en CI — una línea por proyecto, warnings y errores visibles
- **Test results**: subidos como artefacto TRX en cada run de CI
- **Analyzer diagnósticos**: visibles en IDE a través de `EnforceCodeStyleInBuild=true` + `.editorconfig`
- **NuGet audit**: warnings/errors de audit visibles en `dotnet restore` output
