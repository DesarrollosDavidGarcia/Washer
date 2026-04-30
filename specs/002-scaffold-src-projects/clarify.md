# Clarify — 002 scaffold-src-projects

- **Fecha**: 2026-04-22
- **Ronda**: 1 (7 preguntas, 7 respondidas)
- **Ejes cubiertos**: alcance, modelo de empaquetado, integraciones de MAUI, ejecución/reproducibilidad, gobernanza entre specs.
- **No invocados**: `security-reviewer` (sin PII/auth/pagos) ni `ui-ux-designer` (sin UI).

## Q&A

### PA-01 — SDK de `TallerPro.Components`

- **Opciones**:
  - A. `Microsoft.NET.Sdk.Razor` (Recomendado)
  - B. `Microsoft.NET.Sdk` + `<AddRazorSupportForMvc>true</AddRazorSupportForMvc>`
- **Respuesta**: **A — `Microsoft.NET.Sdk.Razor`**.
- **Impacto**: `TallerPro.Components.csproj` declara `Sdk="Microsoft.NET.Sdk.Razor"`. Compila `.razor` como Razor Class Library, con soporte de static web assets y `_Imports.razor` cuando existan. Patrón estándar para compartir componentes entre `Hybrid` y `Admin`.

### PA-02 — TargetFramework de Windows en `TallerPro.Hybrid`

- **Opciones**:
  - A. `net9.0-windows10.0.19041.0` (Recomendado)
  - B. `net9.0-windows10.0.22621.0`
- **Respuesta**: **A — `net9.0-windows10.0.19041.0`**.
- **Impacto**: Hybrid declara `net9.0-windows10.0.19041.0` (Windows 10 2004 mínimo). Máxima cobertura del parque instalado del cliente objetivo (talleres mecánicos MX). Si se requieren APIs Windows 11, se revisa vía ADR.

### PA-03 — iOS/MacCatalyst en `TallerPro.Hybrid`

- **Opciones**:
  - A. Solo `net9.0-android` + `net9.0-windows10.0.19041.0` (Recomendado)
  - B. Incluir también `net9.0-ios` y `net9.0-maccatalyst`
- **Respuesta**: **A — Solo Android + Windows**.
- **Impacto**: `<TargetFrameworks>net9.0-android;net9.0-windows10.0.19041.0</TargetFrameworks>`. Alineado con constitución (MVP Windows + Android). CI y devs no requieren Xcode/macOS. Ampliación futura → ADR.

### PA-04 — TargetFramework de `TallerPro.Analyzers`

- **Opciones**:
  - A. `netstandard2.0` (Recomendado)
  - B. `net8.0` o superior
- **Respuesta**: **A — `netstandard2.0`**.
- **Impacto**: `TallerPro.Analyzers.csproj` con `<TargetFramework>netstandard2.0</TargetFramework>`, `<IsRoslynComponent>true</IsRoslynComponent>`, `<EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>`, `<IncludeBuildOutput>false</IncludeBuildOutput>`. Máxima compatibilidad con MSBuild/VS/Rider/omnisharp.

### PA-05 — Relación con spec 001 (bootstrap-monorepo)

- **Opciones**:
  - A. 002 precede, 001 se recorta (Recomendado)
  - B. 002 reemplaza a 001 por completo
  - C. Descartar 002 y resolver 001 directamente
- **Respuesta**: **A — 002 precede, 001 se recorta**.
- **Impacto**:
  - Esta spec 002 es la primera en ejecutarse.
  - La spec 001 sigue vigente pero deberá re-redactarse eliminando RF-06 (creación de los 13 `.csproj`) y concentrándose en: `.sln`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, stub del analyzer `TallerPro.Analyzers` (reglas TP0001-TP0005 registradas como `DiagnosticDescriptor`), `ci.yml`, `README.md` raíz, `.editorconfig`.
  - El recorte de 001 se hará cuando 001 entre a su propio `/speckit.clarify`. Queda anotado como TODO externo a 002.

### PA-06 — Contenido admitido en `src/TallerPro.*/` tras aplicar la spec

- **Opciones**:
  - A. Solo `.csproj` + `CLAUDE.md` (`obj/`/`bin/` ignorados por `.gitignore`) (Recomendado)
  - B. Además permitir `Class1.cs` placeholder
- **Respuesta**: **A — solo `.csproj` + `CLAUDE.md`**.
- **Impacto**: el commit resultante deja en cada carpeta `src/TallerPro.<Proj>/` exactamente dos archivos: el `.csproj` nuevo y el `CLAUDE.md` preexistente. Ninguna fuente `.cs`/`.razor`. `obj/` y `bin/` quedan fuera del control de versiones (ya cubiertos por `.gitignore` raíz).

### PA-07 — Método de producción de los `.csproj`

- **Opciones**:
  - A. Manuales byte-exact (Recomendado)
  - B. `dotnet new` + edits manuales posteriores
- **Respuesta**: **A — manuales byte-exact**.
- **Impacto**: los 13 `.csproj` se escriben a mano con contenido determinista. No depende de la versión de plantillas del SDK local. Reproducible entre clones y entre dev/CI.

## Resumen de cambios aplicados a `spec.md`

- RF-05 actualizado para fijar `Sdk="Microsoft.NET.Sdk.Razor"` en Components (PA-01).
- RF-06 actualizado con `TargetFrameworks` exactos (PA-02, PA-03).
- RF-13 confirmado `netstandard2.0` y props de Roslyn (PA-04).
- Nueva nota de gobernanza en "Conflictos con spec 001" reemplazando las 3 opciones por la decisión A (PA-05) y TODO externo de recortar RF-06 de 001.
- CA-08 extendido: solo `.csproj` + `CLAUDE.md` por carpeta (PA-06).
- Nuevo RF-18: los `.csproj` se escriben a mano, sin `dotnet new` (PA-07).
- Sección "Preguntas abiertas" vaciada.
- Añadida sección "Historial de clarificaciones".

## Cierre

- **Fecha**: 2026-04-30
- **Estado**: `implemented`
- **verify.sh**: exit 0 — `12 passed, 0 failed, 0 skipped, 0 warnings`
- **test_verify.sh**: 3 cases PASS (CASE-A, CASE-B, CASE-A-full)
- **SHA-256 CLAUDE.md**: inalterados (sha256sum -c exit 0, 13 OK)
- **Revisores**: code-reviewer `approve` + qa-reviewer `approve`
- **Correcciones post-review**: verify.sh C5 extendido (Hybrid: OutputType/UseMaui/SingleProject; Analyzers: IsRoslynComponent/EnforceExtendedAnalyzerRules/IncludeBuildOutput); quickstart.md §Paso 8 + §Troubleshooting actualizados (H-2 sha256sum alternativa)
- **Backlog spec 001**: AssemblyName si Directory.Build.props lo redefine (S-1 code-reviewer); C13 byte-exact diff (S-2 qa); cases negativos en test harness (S-3 qa)
