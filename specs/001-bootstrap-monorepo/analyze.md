# Analyze — 001 bootstrap-monorepo

- **Fecha**: 2026-04-30
- **Veredicto**: `READY_TO_IMPLEMENT` *(tras aplicar las 12 correcciones el 2026-04-30)*
- **Bloqueantes**: 3 (B-01, B-02, B-03)
- **Importantes no bloqueantes**: 5
- **Advertencias**: 6
- **Siguiente**: corregir bloqueantes en `plan.md` y `tasks.md`, luego volver a `/speckit.analyze` o proceder directo a `/speckit.implement` si el orquestador acepta las correcciones inline.

---

## 1. Matriz de trazabilidad RF → Plan → Tasks

| RF | Descripción | Plan §Componente | Tarea | Estado |
|---|---|---|---|---|
| RF-01 | `global.json` SDK 9.0.203 | §global.json | T-01 | ✅ |
| RF-02 | `TallerPro.sln` 18 proyectos | §TallerPro.sln | T-07 | ✅ |
| RF-03 | `Directory.Build.props` | §Directory.Build.props | T-02 | ⚠️ spec dice `TargetFramework=net9.0`; plan lo omite correctamente (D-01). Discrepancia menor resuelta por el plan. |
| RF-04 | `Directory.Packages.props` CPM | §Directory.Packages.props | T-03 | ⚠️ Falta `Microsoft.AspNetCore.Mvc.Testing` — ver B-03 |
| RF-05 | `.editorconfig` | §.editorconfig | T-04 | ✅ |
| RF-06 | 13 `.csproj` en `src/` | — | — | ✅ cubierto por spec 002 implementada |
| RF-07 | 5 test `.csproj` | §Test .csproj plantilla | T-06 | ⚠️ proyectos vacíos + TreatWarningsAsErrors → CS8021 — ver B-02 |
| RF-08 | `ci.yml` biplatforma + slnf | §ci.yml + slnf | T-09, T-11 | ⚠️ `--locked-mode` ausente del YAML canónico — ver I-04 |
| RF-09 | `README.md` | §Observabilidad | T-12 | ✅ |
| RF-10 | Analyzer stub TP0001-TP0005 | §Analyzer stub | T-05 | ❌ `AnalyzerReleases.Unshipped.md` formato incorrecto — ver B-01 |
| RF-11 | `git init` + primer commit | §Despliegue | T-01, T-14 | ✅ |

## 2. Matriz de trazabilidad CA → Tasks → Criterio

| CA | Descripción | Tarea | Criterio verificable | Estado |
|---|---|---|---|---|
| CA-01 | Build exit 0, 0 warnings (ubuntu + windows) | T-10 (ubuntu) / CI (windows) | T-10 verifica ubuntu; windows solo vía CI remoto | ⚠️ windows sin verificación local — ver I-01 |
| CA-02 | 5 suites, 0 tests, exit 0 | T-10 | `dotnet test TallerPro.Linux.slnf --no-build` | ❌ proyectos vacíos + `TreatWarningsAsErrors` → CS8021 — ver B-02 |
| CA-03 | TP0005 detecta `Console.WriteLine` | T-10 (probe) | Archivo probe → build reporta TP0005 | ✅ |
| CA-04 | IDE carga 18 proyectos | Ninguna | — | ⚠️ solo manual, sin tarea ni evidencia requerida — ver I-02 |
| CA-05 | CI verde en PR | T-15 | CI badge verde | ⚠️ requiere remote git configurado, no verificado en T-14 |
| CA-06 | Sin `<Version>` en `.csproj` (excepto excepción) | T-08 | `grep` en staging | ✅ |
| CA-07 | `dotnet format --verify-no-changes` | T-10, T-11 | Step en CI + verificación local | ✅ |
| CA-08 | `CLAUDE.md` inalterados | T-10 | `git diff HEAD -- CLAUDE.md` | ❌ HEAD no existe en T-10 (antes del primer commit) — ver B-03 |

---

## 3. Hallazgos bloqueantes

### B-01 — `AnalyzerReleases.Unshipped.md`: bloque `Release 1.0.0` inválido en Unshipped
**Fuente**: code-reviewer  
**Severidad**: Bloqueante  
**Archivo afectado**: `plan.md` §Analyzer stub — `AnalyzerReleases.Unshipped.md` canónico  

El plan incluye `Release 1.0.0` dentro del archivo `AnalyzerReleases.Unshipped.md`. Con `EnforceExtendedAnalyzerRules=true` activo, el SDK de Roslyn Analyzers (RS2000/RS2001) rechaza números de versión en el archivo Unshipped — ese bloque pertenece exclusivamente a `AnalyzerReleases.Shipped.md`. El build del analyzer fallará en T-05 con un diagnóstico Roslyn antes de llegar a los tests.

**Corrección en plan.md**: el contenido canónico de `AnalyzerReleases.Unshipped.md` debe ser:
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
Sin línea `Release X.Y.Z`.

---

### B-02 — Proyectos de test vacíos + `TreatWarningsAsErrors=true` → CS8021 rompe build
**Fuente**: qa-reviewer  
**Severidad**: Bloqueante  
**Archivo afectado**: `plan.md` §Test .csproj plantilla; `tasks.md` T-06  

xUnit/`dotnet test` requiere que el ensamblado compile. Con `TreatWarningsAsErrors=true` global (desde `Directory.Build.props`) y proyectos de test sin ningún archivo `.cs`, el compilador emite CS8021 ("No files were found to compile"), que al ser un warning se convierte en error. Resultado: CA-02 no es alcanzable con el plan actual.

**Opciones de corrección** (plan.md debe elegir una y documentarla):
- **A (recomendada)**: añadir `<NoWarn>CS8021</NoWarn>` en la plantilla base de los `.csproj` de test. Simple, no requiere archivos `.cs`.
- **B**: añadir un archivo `GlobalUsings.cs` o `Placeholder.cs` vacío (solo `// placeholder`) en cada suite de test — convierte el proyecto en compilable sin tests reales.

T-06 debe incluir esta solución en su criterio de hecho.

---

### B-03 — T-10 usa `git diff HEAD` antes de que exista el primer commit
**Fuente**: qa-reviewer (I-04), orquestador  
**Severidad**: Bloqueante  
**Archivo afectado**: `tasks.md` T-10 — criterio CA-08  

T-10 ocurre antes de T-14 (primer commit). El repositorio está en estado "initial — no commits yet". `git diff --name-only HEAD` falla con `fatal: ambiguous argument 'HEAD'`. El criterio de CA-08 en T-10 es técnicamente no ejecutable en ese momento.

**Corrección en tasks.md T-10**: sustituir el criterio de CA-08 por:
```bash
# Verificar que ningún CLAUDE.md tiene cambios staged o sin stagear
git diff --name-only -- 'src/TallerPro.*/CLAUDE.md' 'tests/TallerPro.*/CLAUDE.md'
# O bien:
git status --porcelain -- 'src/TallerPro.*/CLAUDE.md' 'tests/TallerPro.*/CLAUDE.md'
# Ambos funcionan sin requerir commits previos
```

---

## 4. Hallazgos importantes (no bloquean, pero se recomienda corregir antes de implementar)

### I-01 — CA-01 windows sin verificación local
**Fuente**: qa-reviewer  
CA-01 requiere build exit 0 en ubuntu **y** windows. T-10 solo verifica ubuntu. La verificación windows queda diferida a que CI corra, pero CA-05 (CI verde) requiere un remote configurado. Si el repo se queda en local (sin push), CA-01 windows nunca se verifica.

**Corrección**: en T-10 añadir como criterio explícito: "Si el entorno de desarrollo es Windows, también ejecutar `dotnet build TallerPro.sln -c Release --no-restore`. Si el entorno es Linux/Mac, este criterio se delega a CA-05 (CI en windows-latest)."

### I-02 — CA-04 (IDE) sin tarea ni evidencia requerida
**Fuente**: qa-reviewer  
Ninguna tarea cubre CA-04. El plan lo marca como "verificación manual fuera de CI" pero tasks.md no tiene una tarea que lo exija antes de marcar T-16 (spec implemented).

**Corrección**: añadir en T-16 (o en una tarea T-10b) el criterio: "CA-04 verificado manualmente: abrir `TallerPro.sln` en VS2022 17.12+, confirmar 18 proyectos cargados sin errores, anotar resultado en `clarify.md §Cierre`."

### I-03 — CPM exception: `<Version>` → `<VersionOverride>`
**Fuente**: code-reviewer  
Con `ManagePackageVersionsCentrally=true`, `<PackageReference Include="..." Version="..."/>` en un `.csproj` genera `NU1008` a menos que el paquete también esté declarado en `Directory.Packages.props`. La forma correcta bajo CPM activo es `<VersionOverride>`:
```xml
<PackageReference Include="Microsoft.CodeAnalysis.CSharp" VersionOverride="4.12.*" />
```

**Corrección en plan.md**: cambiar `Version=` por `VersionOverride=` en el bloque de excepción CPM de `TallerPro.Analyzers.csproj`.

### I-04 — `--locked-mode` ausente del YAML canónico de CI
**Fuente**: code-reviewer (I-02) + qa-reviewer (I-03)  
`RestorePackagesWithLockFile=true` genera el lock, pero sin `--locked-mode` en CI el restore no valida que el lock esté en sync. Un `Directory.Packages.props` modificado sin regenerar el lock pasará CI silenciosamente.

**Corrección en plan.md ci.yml**: cambiar los steps de restore a `dotnet restore TallerPro.Linux.slnf --locked-mode` (ubuntu) y `dotnet restore TallerPro.sln --locked-mode` (windows). Nota: el primer run de CI (antes de commitear el lock) debe hacerse sin `--locked-mode`; el YAML debe tener un comentario que indique esto o usar `continue-on-error: true` solo para el primer run.

### I-05 — `Microsoft.AspNetCore.Mvc.Testing` faltante en `Directory.Packages.props`
**Fuente**: code-reviewer  
El plan lista `Microsoft.AspNetCore.Mvc.Testing` como paquete adicional de `Integration.Tests`, pero no aparece en ninguna sección de `Directory.Packages.props`. CPM generará `NU1604` y romperá el restore de T-06.

**Corrección en plan.md Directory.Packages.props**: añadir en el grupo de Test:
```xml
<PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.*" />
```

---

## 5. Hallazgos de seguridad

### Sec-Medio — `upload-artifact@v4` SHA flotante
**Fuente**: security-reviewer + code-reviewer (S-02)  
El plan fija SHAs para `actions/checkout` y `actions/setup-dotnet` (Sec-03) pero deja `actions/upload-artifact@v4` con tag flotante. Inconsistente con D-6 del ADR.

**Corrección**: el implementador de T-11 debe verificar el SHA vigente de `actions/upload-artifact@v4` y fijarlo.

### Sec-Bajo — `dotnet format` ausente del YAML canónico
**Fuente**: security-reviewer  
T-11 exige añadir el step, pero el YAML del plan no lo muestra. Riesgo de drift si el implementador sigue el YAML canónico literalmente.

**Corrección**: añadir el step en el YAML canónico de plan.md.

### Sec-Bajo — CODEOWNERS sin tarea ni contenido canónico
**Fuente**: security-reviewer  
ADR D-7 exige 2 reviewers para `TallerPro.Analyzers`. Sin `.github/CODEOWNERS`, esta protección no existe en la práctica.

**Corrección**: añadir en T-11 o T-14 la creación de `.github/CODEOWNERS` con la regla mínima:
```
src/TallerPro.Analyzers/ @<founder-github-handle> @<dev-senior-handle>
```

---

## 6. Advertencias del orquestador

### A-01 — RF-03 discrepancia spec/plan sobre `<TargetFramework>`
La spec RF-03 menciona `TargetFramework=net9.0` en `Directory.Build.props`, pero el plan (D-01) lo omite correctamente. La spec fue escrita antes de spec 002; los 13 `.csproj` ya tienen su TFM. No es un error del plan — es una ambigüedad de la spec resuelta por D-01. No requiere acción.

### A-02 — T-13 referencia D-1..D-7 pero el plan documenta D-01..D-05
tasks.md T-13 dice "cross-check D-1..D-7" pero `plan.md` solo define D-01..D-05 (más la excepción CPM mencionada separado, y la discusión de slnf). Corregir a "D-01..D-05" en T-13.

### A-03 — `LangVersion=latest` puede generar drift de warnings entre versiones del compilador
Con `TreatWarningsAsErrors=true`, un compilador nuevo puede romper el build con warnings nuevos. Considerar fijar `LangVersion=13.0` (C# del SDK 9.0.x). Bajo impacto mientras solo haya código de configuración.

---

## 7. Secciones por subagente

### code-reviewer (veredicto: `changes-requested`)
B-01 (AnalyzerReleases.Unshipped.md), I-01 (CPM VersionOverride), I-02 (--locked-mode primer run), I-03 (Microsoft.AspNetCore.Mvc.Testing), S-01 (LangVersion), S-02 (upload-artifact SHA), S-03 (T-13 D-07 reference), S-04 (TransitivePinning no justificado).

### security-reviewer (veredicto: no bloqueantes)
Hallazgos previos incorporados correctamente. Nuevos: Sec-Medio (upload-artifact SHA), Sec-Bajo (dotnet format en YAML), Sec-Bajo (CODEOWNERS sin tarea).

### qa-reviewer (veredicto: `changes-requested`)
B-01 (CA-01 windows), B-02 (CS8021 proyectos vacíos), I-01 (CA-04 sin tarea), I-02 (NuGetAudit CVE procedure), I-03 (--locked-mode en CI YAML), I-04 (git diff HEAD antes del primer commit).

---

## 8. Correcciones requeridas en plan.md y tasks.md

Para pasar a `READY_TO_IMPLEMENT`, el orquestador debe aplicar los siguientes cambios:

| # | Archivo | Corrección |
|---|---|---|
| 1 | `plan.md` | `AnalyzerReleases.Unshipped.md`: eliminar línea `Release 1.0.0` (B-01) |
| 2 | `plan.md` | Excepción CPM: `<Version>` → `<VersionOverride>` en bloque de TallerPro.Analyzers (I-03) |
| 3 | `plan.md` | Añadir `<PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.*" />` en Directory.Packages.props (I-05) |
| 4 | `plan.md` | YAML ci.yml: añadir `--locked-mode` en steps de restore (I-04) |
| 5 | `plan.md` | YAML ci.yml: fijar SHA de `upload-artifact` + añadir step de `dotnet format` (Sec-Medio, Sec-Bajo) |
| 6 | `plan.md` | Añadir `.github/CODEOWNERS` en tabla §Componentes + contenido canónico (Sec-Bajo) |
| 7 | `tasks.md` | T-06: añadir `<NoWarn>CS8021</NoWarn>` o clase placeholder como criterio (B-02) |
| 8 | `tasks.md` | T-10 CA-08: sustituir `git diff HEAD` por `git status --porcelain` (B-03) |
| 9 | `tasks.md` | T-10: aclarar que CA-01 windows se delega a CA-05 si entorno es Linux (I-01) |
| 10 | `tasks.md` | T-13: cambiar referencia "D-1..D-7" por "D-01..D-05" (A-02) |
| 11 | `tasks.md` | T-16: añadir CA-04 como verificación manual requerida con evidencia (I-02) |
| 12 | `tasks.md` | T-11 o T-14: añadir creación de `.github/CODEOWNERS` (Sec-Bajo) |
