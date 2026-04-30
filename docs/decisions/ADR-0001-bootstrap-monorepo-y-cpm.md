# ADR-0001 — Bootstrap del monorepo y Central Package Management

- **Estado**: Accepted
- **Fecha**: 2026-04-30
- **Spec**: `specs/001-bootstrap-monorepo`
- **Decisores**: founder, security-reviewer

## Contexto

El monorepo TallerPro tiene 13 proyectos `src/` (creados en spec 002) y 5 suites `tests/` (por crear en spec 001). Se necesita:
1. Compilación centralizada y reproducible entre dev local, CI y futuros devs.
2. Gestión central de versiones NuGet para evitar divergencia entre proyectos.
3. Enforcement del Roslyn Analyzer propio (`TallerPro.Analyzers`) en todos los proyectos sin configuración por proyecto.
4. CI biplatforma (ubuntu + windows) para el repo que incluye `TallerPro.Hybrid` (MAUI).
5. Supply-chain security desde el primer commit.

## Decisiones

### D-1 — Central Package Management con `Directory.Packages.props`

Se usa `ManagePackageVersionsCentrally=true`. Ningún `.csproj` declara `<Version>` en `<PackageReference>` (excepto la excepción documentada en D-3).

**Razón**: una sola fuente de verdad para versiones NuGet. Un `dotnet list package --outdated` en la raíz actualiza todo el monorepo.

### D-2 — Inyección del analyzer vía `Directory.Build.props` con `ProjectReference`

`TallerPro.Analyzers` se inyecta en todos los proyectos excepto sí mismo mediante:
```xml
<ProjectReference Include="..." OutputItemType="Analyzer" ReferenceOutputAssembly="false" Private="false" />
```
con `Condition="'$(MSBuildProjectName)' != 'TallerPro.Analyzers'"`.

**Razón**: un punto de configuración sin archivos adicionales. El `ProjectReference` garantiza que cualquier cambio en el analyzer se aplica en el próximo build sin ciclo de publicación NuGet.

**Alternativa rechazada**: NuGet local en `/packages` — añade complejidad de packaging y un ciclo de versión interno innecesario para un analyzer que evoluciona junto con el código de negocio.

### D-3 — Excepción a CPM en `TallerPro.Analyzers.csproj`

`Microsoft.CodeAnalysis.CSharp` y `Microsoft.CodeAnalysis.Analyzers` se declaran con `<Version>` explícita en el `.csproj` del analyzer. Esta es la **única excepción permitida** a RF-04/CA-06.

**Razón**: estos paquetes deben ser `PrivateAssets=all` — no deben propagarse como dependencias transitivas a los consumidores del analyzer. CPM no permite expresar `PrivateAssets` por paquete cuando la versión está en `Directory.Packages.props`. La versión exacta del SDK de Roslyn debe estar coordinada con la versión de `Microsoft.NET.Sdk` del proyecto, lo que hace deseable el control fino.

### D-4 — `NuGetAudit=true` + `RestorePackagesWithLockFile=true` desde el bootstrap

Ambas propiedades en `Directory.Build.props` desde el primer commit:
- `NuGetAuditMode=all`, `NuGetAuditLevel=moderate`: falla el restore si hay CVE moderada o superior.
- `RestorePackagesWithLockFile=true` + `packages.lock.json` commiteado: el grafo de dependencias está fijo en git; CI falla si el lock no coincide.

**Razón**: el bootstrap es el momento más barato para establecer la postura de seguridad. Añadirlo después obliga a revisar todos los paquetes ya instalados.

### D-5 — `TallerPro.Linux.slnf` como solution filter para CI ubuntu

CI ubuntu usa `TallerPro.Linux.slnf` (excluye `TallerPro.Hybrid`). CI windows usa `TallerPro.sln` completo.

**Razón**: `TallerPro.Hybrid` requiere el workload MAUI de .NET en ubuntu para compilar. Instalarlo en CI linux es lento, caro en minutos de Actions, e innecesario para el CI de bootstrap. Los builds MAUI para Android/Windows se gestionarán en `mobile-build.yml` (spec posterior).

**Alternativa rechazada**: `continue-on-error: true` en el step de Hybrid — enmascara fallos reales de compilación.

### D-6 — SHA fijo para GitHub Actions + `pull_request_target` prohibido

Todas las Actions se referencian por SHA inmutable, no por tag flotante. `pull_request_target` está prohibido en todos los workflows sin aprobación de `security-reviewer`.

**Razón**: tags flotantes en Actions son un vector de supply-chain attack si la Action es comprometida y el mantenedor mueve el tag. `pull_request_target` expone secretos del repo a PRs de forks.

### D-7 — 2 reviewers para `TallerPro.Analyzers/` desde el primer commit

El CODEOWNERS del analyzer incluye founder + al menos un dev senior. Toda PR que modifique `src/TallerPro.Analyzers/` requiere 2 aprobaciones.

**Razón**: el analyzer ejecuta código arbitrario en el proceso del compilador durante `dotnet build`. Una modificación maliciosa podría exfiltrar variables de entorno del runner de CI.

## Consecuencias

- `packages.lock.json` se commitea al repo y debe actualizarse cuando se cambian versiones en `Directory.Packages.props`.
- `dotnet restore --locked-mode` en CI garantiza que el grafo sea idéntico al lock commiteado.
- Devs de backend no necesitan el workload MAUI para trabajar en proyectos no-Hybrid.
- La excepción de D-3 es visible en cualquier `dotnet list package --include-transitive` sobre `TallerPro.Analyzers`.

## Referencias

- `specs/001-bootstrap-monorepo/plan.md`
- `specs/001-bootstrap-monorepo/clarify.md`
- `.specify/memory/constitution.md` §Convenciones de calidad, §Restricciones técnicas
- `.specify/memory/stack.md` §Herramientas
