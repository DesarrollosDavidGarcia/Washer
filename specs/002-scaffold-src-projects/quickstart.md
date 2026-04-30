# Quickstart: Scaffold de los 13 proyectos en `src/`

- **Spec**: `specs/002-scaffold-src-projects/spec.md`
- **Plan**: `specs/002-scaffold-src-projects/plan.md`

## Pre-requisitos

- [ ] .NET 9 SDK instalado (`dotnet --version` ≥ `9.0.100`).
- [ ] Clon local del repo `Washer` con `src/TallerPro.*/CLAUDE.md` presentes (13 carpetas).
- [ ] `git` funcional (para verificar `git ls-files`).
- [ ] **Opcional** para verificar Hybrid: workload MAUI instalado (`dotnet workload install maui`). Si no está, los 12 proyectos no-Hybrid siguen verificándose.
- [ ] Env vars: **ninguna**.

## Pasos

### 1. Aplicar la spec

Tras `/speckit.tasks` y `/speckit.analyze(READY)`, ejecutar `/speckit.implement` sobre `specs/002-scaffold-src-projects`. El agente `dotnet-dev` escribirá los 13 `.csproj` a mano siguiendo los bloques canónicos del plan (§Componentes).

### 2. Verificar estructura

```bash
# Debe imprimir exactamente 13
find src -maxdepth 2 -name '*.csproj' | wc -l
```

### 3. Verificar contenido de cada `.csproj`

```bash
# Cada .csproj debe contener estas dos líneas (sin excepción)
for f in src/TallerPro.*/TallerPro.*.csproj; do
  grep -q '<Nullable>enable</Nullable>' "$f" || echo "FAIL Nullable: $f"
  grep -q '<ImplicitUsings>enable</ImplicitUsings>' "$f" || echo "FAIL ImplicitUsings: $f"
  grep -q '<PackageReference' "$f" && echo "FAIL PackageReference present: $f"
  grep -q '<ProjectReference' "$f" && echo "FAIL ProjectReference present: $f"
done
```

### 4. Verificar SDK por proyecto

| Proyecto | `Sdk` esperado |
|---|---|
| Domain, Application, Infrastructure, Shared, LocalDb, Observability, Security, Hybrid, Analyzers | `Microsoft.NET.Sdk` |
| Components | `Microsoft.NET.Sdk.Razor` |
| Api, Web, Admin | `Microsoft.NET.Sdk.Web` |

### 5. Verificar `TargetFramework(s)`

- `Hybrid` → `<TargetFrameworks>net9.0-android;net9.0-windows10.0.19041.0</TargetFrameworks>`.
- `Analyzers` → `<TargetFramework>netstandard2.0</TargetFramework>`.
- Los 11 restantes → `<TargetFramework>net9.0</TargetFramework>`.

### 6. Restore individual (sin `.sln`)

```bash
# Los 12 no-Hybrid: restore debe pasar exit 0
for f in src/TallerPro.Domain src/TallerPro.Application src/TallerPro.Infrastructure \
         src/TallerPro.Shared src/TallerPro.Components src/TallerPro.Api \
         src/TallerPro.LocalDb src/TallerPro.Web src/TallerPro.Admin \
         src/TallerPro.Observability src/TallerPro.Security src/TallerPro.Analyzers; do
  dotnet restore "$f" || echo "FAIL restore: $f"
done
```

### 7. Restore Hybrid (opcional)

```bash
# Requiere workload MAUI
dotnet restore src/TallerPro.Hybrid
```

### 8. Verificar que `CLAUDE.md` no cambió

```bash
# Opción A — si el repo tiene al menos 2 commits:
git diff --stat HEAD~1 HEAD -- 'src/TallerPro.*/CLAUDE.md'
# Debe devolver vacío

# Opción B — válida siempre (recomendada en repos con un solo commit):
sha256sum -c specs/002-scaffold-src-projects/_verification/claude-md-hashes.txt
# Debe imprimir 13 líneas con ": OK" y exit 0
```

### 9. Verificar contenido del directorio por proyecto

```bash
# Debe listar exactamente: CLAUDE.md + TallerPro.<Proj>.csproj
for d in src/TallerPro.*/; do
  echo "=== $d ==="
  git ls-files "$d"
done
```

## Esperado

- 13 archivos `.csproj` nuevos en `src/TallerPro.*/`.
- Los 13 `CLAUDE.md` preexistentes intactos (hash idéntico).
- `tests/`, `build/`, raíz del repo — sin cambios.
- `dotnet restore` individual de cada proyecto: exit 0 (salvo Hybrid sin workload MAUI).
- `dotnet build` sin código fuente produce assemblies vacíos en `bin/` (no commiteado).

## Casos límite

- [ ] Entrada vacía: un `.csproj` con solo `<Project Sdk="..."/>` y sin `<PropertyGroup>` → **no se admite**; falta `TargetFramework`.
- [ ] Entrada inválida: `TargetFramework=net8.0` → viola RF-01…RF-13 y constitución (`.NET 9`). Rechazar.
- [ ] Sin permisos: n/a (solo escritura local).
- [ ] Concurrencia: un dev ejecuta `/speckit.implement` mientras otro escribe en `src/` → conflicto de merge normal, resolver con `git`.
- [ ] Workload MAUI ausente: el restore de 12 proyectos pasa; Hybrid falla con `NETSDK1147`. Comportamiento esperado y documentado.
- [ ] `LF` vs `CRLF`: clones en Windows pueden normalizar a CRLF si `.gitattributes` no cubre `.csproj`. Verificar `eol=lf` en `.gitattributes` antes de commit.

## Troubleshooting

| Síntoma | Causa | Acción |
|---|---|---|
| `error NETSDK1147: To build this project, the following workloads must be installed: maui-windows` | Workload MAUI no instalado | `dotnet workload install maui` o saltar verificación Hybrid en local. |
| `error MSB4018: The "Microsoft.Build.NuGetSdkResolver.NuGetSdkResolver" failed` | Falta `global.json` y el SDK local es incompatible | Esto llega con spec 001. Forzar temporalmente con `DOTNET_ROLL_FORWARD=LatestFeature`. |
| `warning NU1701: Package 'X' was restored using '.NETFramework...'` | Algún NuGet transitivo (no debería haber ninguno aún) | Revisar que el `.csproj` no tenga `PackageReference` inadvertido. |
| `error NETSDK1045: The current .NET SDK does not support targeting .NET 9.0` | SDK local < 9 | Actualizar a .NET 9 SDK. |
| `git diff --stat HEAD~1 HEAD` falla con "unknown revision or path" | Repo con un solo commit (no existe `HEAD~1`) | Usar la Opción B del §Paso 8: `sha256sum -c specs/002-scaffold-src-projects/_verification/claude-md-hashes.txt`. |
| `.csproj` muestra BOM al diff en GitHub | Editor añadió BOM al guardar | Rescribir sin BOM (`file` debe decir `XML 1.0 document, ASCII text`). |
| Dos `.csproj` difieren por orden de props entre devs | `dotnet new` usado en vez de byte-exact (viola D-02) | Reescribir a partir del bloque canónico del plan. |
