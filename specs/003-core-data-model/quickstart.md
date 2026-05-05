# Quickstart: Spec 003 — Modelo de datos base

> Cómo levantar SQL Server local, aplicar migraciones, correr el seed de dev y ejecutar los tests de aislamiento.

## Pre-requisitos

- .NET SDK `9.0.203+` (ver `global.json`).
- Docker Desktop (para Testcontainers + SQL Server local).
- VS Code o VS 2022 17.12+.
- Variable de entorno opcional: `TALLERPRO_DB_CONNECTION`.

## 1. Levantar SQL Server local (Docker)

```powershell
docker run -d `
  --name tallerpro-sql `
  -e "ACCEPT_EULA=Y" `
  -e "MSSQL_SA_PASSWORD=Dev_Pass_2026!" `
  -p 1433:1433 `
  mcr.microsoft.com/mssql/server:2022-latest
```

Connection string para dev (en `src/TallerPro.Api/appsettings.Development.json` o env var):

```
Server=localhost,1433;Database=TallerPro_Dev;User Id=sa;Password=Dev_Pass_2026!;TrustServerCertificate=true
```

> **No** uses `sa` en staging/prod. Solo dev local.

## 2. Aplicar migración inicial

Desde la raíz del repo:

```powershell
dotnet tool restore
dotnet ef database update `
  --project src/TallerPro.Infrastructure `
  --startup-project src/TallerPro.Api `
  --connection "Server=localhost,1433;Database=TallerPro_Dev;User Id=sa;Password=Dev_Pass_2026!;TrustServerCertificate=true"
```

Verifica que existan los schemas `core` y `auth`, y la tabla `core.Tenants` con un registro:

```sql
SELECT Id, Slug, Name, Status FROM core.Tenants;
-- 1 | tallerpro-platform | TallerPro Platform | 0
```

## 3. Seed de datos dev (opcional)

Solo en dev/test. Ejecuta el seeder:

```powershell
dotnet run --project src/TallerPro.Api -- --seed-dev
```

Crea:
- 1 tenant `acme` (Active)
- 2 branches: `acme/B001`, `acme/B002`
- 1 user `owner@acme.test` con role `Admin` en ambos branches
- 1 platform admin `dev@tallerpro.local`

## 4. Generar nueva migración (workflow de devs)

Cuando agregues entidades nuevas en specs futuras:

```powershell
dotnet ef migrations add 0002_<NombreFeature> `
  --project src/TallerPro.Infrastructure `
  --startup-project src/TallerPro.Api
```

Luego revisa el archivo generado y ajusta si EF generó algo no idempotente.

## 5. Ejecutar tests

### Tests de dominio (rápidos, sin BD)

```powershell
dotnet test tests/TallerPro.Domain.Tests
```

### Tests de integración (Testcontainers — levanta SQL Server real)

```powershell
dotnet test tests/TallerPro.Integration.Tests
```

> Tarda ~30-60s la primera vez (descarga imagen). Asegúrate de que Docker esté corriendo.

### Tests de aislamiento (críticos — verifica cross-tenant leak = 0)

```powershell
dotnet test tests/TallerPro.Isolation.Tests
```

**Si algún test de aislamiento falla, NO MERGEAR.** Cualquier fallo aquí significa que el global filter de tenant scoping está roto y hay riesgo de leak en producción.

### Suite completa con coverage

```powershell
dotnet test --collect:"XPlat Code Coverage"
```

## 6. Verificación de bloqueo del analyzer TP0001

Crea temporalmente un archivo en `src/TallerPro.Api/`:

```csharp
public class TestLeak
{
    public void Foo(TallerProDbContext ctx)
    {
        // Esto DEBE fallar al compilar:
        var all = ctx.Branches.IgnoreQueryFilters().ToList();
    }
}
```

Compila:

```powershell
dotnet build src/TallerPro.Api -c Release
```

**Esperado**: error `TP0001: IgnoreQueryFilters() requires [AllowIgnoreQueryFilters("razón")] on the containing method`. Borra el archivo después de verificar.

Para uso legítimo (super-admin, scripts de soporte):

```csharp
[AllowIgnoreQueryFilters("Listado cross-tenant para super-admin del portal")]
public List<Branch> ListAllBranchesForSupport(TallerProDbContext ctx)
    => ctx.Branches.IgnoreQueryFilters().ToList();
```

## 7. Reset de BD (entre tests / debug)

```powershell
dotnet ef database drop `
  --project src/TallerPro.Infrastructure `
  --startup-project src/TallerPro.Api `
  --force

# Luego: dotnet ef database update (paso 2)
```

## 8. Logs — qué buscar en Seq / Console

Al hacer `SaveChanges`, deberías ver entradas tipo:

```
[INF] SaveChanges entityName=Branch entityId=42 operation=Insert
      tenantId=1 actorType=User actorId=5 durationMs=12 rowVersion=AAAAAAAAB7g=
```

**Nunca** debes ver:
- `email=foo@bar.com` (sería bug en PII masking)
- `tenantId=null` accediendo a entidad tenant-scoped (sería bug crítico — abre incidente)

## Troubleshooting

| Síntoma | Causa probable | Solución |
|---|---|---|
| `MissingTenantContextException` en runtime | DI no registra `ITenantContext` o middleware no setea `CurrentTenantId` | Verificar registro en `Program.cs` y middleware de tenant resolution |
| `DbUpdateConcurrencyException` inesperada | Otro proceso modificó la entidad entre Read y Write | Re-leer + reintento; revisar UI optimistic concurrency |
| Migración 0001 falla con "schema already exists" | BD ya parcialmente migrada | `dotnet ef database drop --force` y re-aplicar |
| Test isolation falla `expected 1 branch got 2` | Global filter desactivado o `ITenantContext` mal cableado | Revisar registro DI; ejecutar test con `--logger console;verbosity=detailed` |
| Lock file conflict en CI | `packages.lock.json` desactualizado | `dotnet restore --force-evaluate` local + commit |
