# Prompt 9 — Docker Multi-Ambiente (Dev, Staging, Producción)

## Contexto del Proyecto

**TallerPro**: SaaS multitenant + multitaller para talleres mecánicos en México.

**Stack producto**: .NET Core 9 + Blazor Hybrid (MAUI) + MudBlazor + Mediator (Othamar) + EF Core + SQL Server + SignalR + Stripe + DeepSeek vía Novita + SW Sapien.

**Stack marketing**: ASP.NET Core MVC .NET 9 + Bootstrap 5 + SCSS + Cloudflare.

**Stack super-admin**: ASP.NET Core MVC + MudBlazor en `admin.tallerpro.mx` + Cloudflare Access.

**Decisiones ya tomadas**:
- **Alpha** (MVP, ≤ 20 tenants): Docker Compose self-hosted en VPS (Hetzner CX31 / Contabo VDS).
- **Beta** (≤ 200 tenants): Azure Container Apps consumption plan.
- **GA** (≤ 2,000 tenants): Azure Container Apps dedicated o AKS.
- **Cliente Hybrid**: MSIX (Windows) + APK Android, NO se containeriza (se instala en máquinas del taller).
- **Observabilidad**: Serilog → Seq self-hosted en mismo VPS.
- **DB**: SQL Server 2022 Developer Edition en dev/staging, SQL Server Standard o Azure SQL en prod.
- **Cache/SignalR backplane**: Redis 7.
- **Reverse proxy**: Caddy 2 (TLS automático con Let's Encrypt).
- **Secrets**: Azure Key Vault en prod; archivo `.env` local cifrado con sops + age en dev/staging.
- **CI/CD**: GitHub Actions con Azure Container Registry (ACR) como image registry.

**Tres ambientes a configurar**:
1. **Dev local** (cada dev en su máquina): hot reload, DBs locales, mocks de integraciones externas, Mailhog para emails.
2. **Staging** (VPS compartido o Azure): stack completo idéntico a prod pero con sandboxes de integraciones (Stripe test keys, Novita sandbox, SW Sapien pruebas, WhatsApp test numbers).
3. **Producción** (alpha en VPS, beta en ACA): stack optimizado con secrets management serio, backups, monitoring, restart policies estrictas, resource limits.

**Imágenes base aprobadas**:
- `mcr.microsoft.com/dotnet/aspnet:9.0` (runtime) — NO `latest`.
- `mcr.microsoft.com/dotnet/sdk:9.0` (build stage).
- `mcr.microsoft.com/mssql/server:2022-latest` (SQL Server).
- `redis:7-alpine`.
- `datalust/seq:2024.3` (pin específico, no `latest`).
- `caddy:2.8-alpine`.
- `axllent/mailpit:latest` (reemplazo moderno de Mailhog, solo dev).

---

## Tu Rol

Actúa como **DevOps Staff Engineer + Platform Engineer** con experiencia comprobada en:
- Docker multi-stage builds optimizados para .NET con layer caching inteligente.
- Docker Compose para orquestación de stacks SaaS B2B en producción (no solo dev).
- Secrets management con sops+age, Docker secrets, Azure Key Vault.
- Health checks rigurosos, restart policies, resource limits.
- Reverse proxy con Caddy 2 y TLS automático.
- CI/CD GitHub Actions con matrices por ambiente.
- Observabilidad contenedorizada (Serilog → Seq en Docker).
- Security hardening: non-root users, distroless, Trivy scanning, imágenes mínimas.
- Estrategias de migración entre Docker Compose y Azure Container Apps.

Responde con archivos **listos para commitear** (Dockerfiles, compose files, scripts bash, YAMLs de CI/CD). No descripciones — artefactos ejecutables.

---

## Alcance de ESTE prompt (P9)

Entregar la **configuración completa de Docker para los 3 ambientes** del sistema.

**SÍ incluir**:
1. Dockerfiles optimizados multi-stage para cada servicio (API, Web, Admin, Analyzers-build).
2. `docker-compose.base.yml` (servicios compartidos).
3. `docker-compose.dev.yml` (override para desarrollo local).
4. `docker-compose.staging.yml` (override para staging).
5. `docker-compose.prod.yml` (override para producción alpha).
6. `.env.dev.template`, `.env.staging.template`, `.env.prod.template` con todas las variables documentadas.
7. Estrategia de secrets management por ambiente.
8. Scripts bash para operaciones comunes (`up`, `down`, `logs`, `backup`, `restore`, `migrate`).
9. CI/CD GitHub Actions integrado: build + scan + push + deploy por ambiente.
10. Health checks específicos por servicio.
11. Resource limits por ambiente.
12. Volúmenes persistentes + backup strategy.
13. Reverse proxy Caddy con config por ambiente.
14. Mocks de integraciones para dev (WireMock o contenedores custom).
15. Runbook operacional markdown.
16. Security hardening checklist.

**NO incluir**:
- Código C# de la aplicación (otros prompts).
- Configuración de Azure Container Apps en detalle (eso es P7 y post-MVP).
- Configuración de AKS (GA, fuera de MVP).
- Configuración del cliente Hybrid (no se containeriza).

---

## Formato de Respuesta Esperado

### 1. Assumptions a validar

2-3 cuestionamientos sobre estrategia de deployment que haces al founder antes de mover código. Ejemplo:
- "¿Staging comparte VPS con prod-alpha o máquina separada? (Tradeoff costo vs isolation)."
- "¿SQL Server en contenedor o managed Azure SQL desde día 1 de prod? (Costo $70/mes vs $200/mes pero elimina ops de DB)."
- "¿Cuál es el RPO/RTO objetivo para backups? (Define si haces backup cada 6h vs diario)."

### 2. Estrategia de Ambientes — Tabla Comparativa

| Aspecto | Dev Local | Staging | Producción (alpha) |
|---------|-----------|---------|---------------------|
| Ubicación | Máquina del dev | VPS compartido o Azure VM | VPS dedicado Hetzner/Contabo |
| CPU/RAM | Host del dev | 2 vCPU / 4 GB | 4 vCPU / 16 GB |
| SQL Server | Contenedor 2022 Dev Edition | Contenedor 2022 Dev Edition | Contenedor 2022 Standard o Azure SQL |
| Redis | Contenedor `redis:7-alpine` | Contenedor con password | Contenedor con password + persistence |
| Seq | Contenedor local puerto 5341 | Contenedor con auth | Contenedor con auth + backup |
| Reverse Proxy | Ninguno (puertos directos) | Caddy con TLS staging | Caddy con TLS Let's Encrypt prod |
| Secrets | `.env.dev` sin cifrar (gitignored) | `sops+age` cifrado en repo | Azure Key Vault o `sops` + GPG |
| Integraciones externas | Mocks (WireMock, Mailpit) | Sandboxes (Stripe test, Novita test, SW Sapien pruebas) | Producción real con keys live |
| Hot reload | Sí (`dotnet watch` + bind mount) | No | No |
| Debug remoto | Puerto expuesto | No | No |
| Backup | No | Diario a Blob | Cada 6h + WAL a Blob + retención 30 días |
| Monitoreo | Seq local | Seq + Slack alerts | Seq + Slack + email + PagerDuty (post-MVP) |
| Restart policy | `no` | `unless-stopped` | `always` con backoff |
| Resource limits | Sin limits | Blandos | Estrictos (OOM killer detecta leaks) |

### 3. Estructura de Archivos Docker

Árbol del directorio `build/docker/` + archivos raíz:

```
tallerpro-saas/
├── build/
│   ├── docker/
│   │   ├── api.Dockerfile
│   │   ├── web.Dockerfile
│   │   ├── admin.Dockerfile
│   │   ├── migrations.Dockerfile    # imagen one-shot que corre EF migrations
│   │   └── caddy/
│   │       ├── Caddyfile.dev
│   │       ├── Caddyfile.staging
│   │       └── Caddyfile.prod
│   ├── compose/
│   │   ├── docker-compose.base.yml
│   │   ├── docker-compose.dev.yml
│   │   ├── docker-compose.staging.yml
│   │   ├── docker-compose.prod.yml
│   │   └── docker-compose.backup.yml   # servicios de backup bajo demanda
│   ├── scripts/
│   │   ├── up.sh
│   │   ├── down.sh
│   │   ├── logs.sh
│   │   ├── migrate.sh
│   │   ├── backup.sh
│   │   ├── restore.sh
│   │   ├── seed-dev.sh
│   │   └── health-check.sh
│   ├── mocks/
│   │   ├── stripe-mock/
│   │   │   ├── Dockerfile
│   │   │   └── mappings/              # WireMock JSON stubs
│   │   ├── novita-mock/
│   │   └── sw-sapien-mock/
│   └── env/
│       ├── .env.dev.template
│       ├── .env.staging.template
│       ├── .env.prod.template
│       └── README.md                   # cómo usar sops+age
├── .dockerignore
└── docker-compose.yml                   # symlink a base + dev para dev experience
```

### 4. Dockerfiles Optimizados (Multi-Stage)

#### 4.1 `build/docker/api.Dockerfile`

Dockerfile completo con:
- **Stage 1: build** — SDK 9.0, restore de packages con cache mount, build Release.
- **Stage 2: publish** — publish con `-c Release --no-restore /p:UseAppHost=false`.
- **Stage 3: runtime** — aspnet:9.0 runtime, non-root user, copy publish output, EXPOSE 8080, ENTRYPOINT.
- Use de `TARGETARCH` para multi-arch (amd64 + arm64).
- Labels OCI estándar.
- Health check integrado.
- Optimización de layer caching:
  - Copy `Directory.Build.props`, `Directory.Packages.props`, `.csproj` primero → restore.
  - Copy source code después → build.
- Imagen final target: < 250 MB.

Código completo ejecutable.

#### 4.2 `build/docker/web.Dockerfile`

Similar a api.Dockerfile pero para `TallerPro.Web`:
- Stage adicional para compilación de SCSS con DartSassBuilder (durante el build stage).
- Stage adicional para bundling con WebOptimizer.
- Runtime final NO necesita SDK.

#### 4.3 `build/docker/admin.Dockerfile`

Similar a api pero para `TallerPro.Admin`:
- Si usa Blazor Server components, asegurar soporte WebSockets.
- Puerto 8080, mismo patrón non-root.

#### 4.4 `build/docker/migrations.Dockerfile`

Imagen one-shot que corre EF Core migrations:
- SDK 9.0 como base (necesita dotnet ef).
- Instala dotnet-ef global tool.
- Copia solo `TallerPro.Infrastructure` + deps.
- ENTRYPOINT que ejecuta `dotnet ef database update` con connection string desde env var.
- Se usa como `depends_on` con `condition: service_completed_successfully` en compose.

#### 4.5 `.dockerignore`

Archivo completo para no contaminar build context:
```
**/.vs/
**/bin/
**/obj/
**/node_modules/
**/*.user
**/*.suo
**/.git/
**/.github/
**/docs/
**/tests/
**/*.md
**/.env*
**/Dockerfile*
**/docker-compose*
```

### 5. Docker Compose — Arquitectura en Capas

Estrategia: **base + overrides por ambiente**.

```bash
# Dev
docker compose -f docker-compose.base.yml -f docker-compose.dev.yml up

# Staging
docker compose -f docker-compose.base.yml -f docker-compose.staging.yml up -d

# Prod
docker compose -f docker-compose.base.yml -f docker-compose.prod.yml up -d
```

#### 5.1 `docker-compose.base.yml`

Servicios compartidos (definiciones mínimas comunes):
- `api`, `web`, `admin`, `migrations` — con `image` referenciando imágenes del registry.
- `sqlserver`, `redis`, `seq` — con volúmenes named persistentes.
- `caddy` — reverse proxy.
- Network `tallerpro-net` interno.
- `healthcheck` definidos.

Archivo YAML completo.

#### 5.2 `docker-compose.dev.yml`

Override para desarrollo:
- Build en lugar de image (construye localmente).
- Bind mounts del código fuente para `dotnet watch`.
- Ports expuestos al host:
  - API: `5001:8080`
  - Web: `5002:8080`
  - Admin: `5003:8080`
  - SQL Server: `1433:1433`
  - Redis: `6379:6379`
  - Seq: `5341:80`
  - Mailpit: `8025:8025` (UI) y `1025:1025` (SMTP)
- Sin Caddy (acceso directo a puertos).
- `mailpit` service para testing emails.
- `stripe-mock` service con WireMock.
- `novita-mock` service con WireMock.
- `sw-sapien-mock` service con WireMock.
- `telegram-mock` service.
- Env vars apuntando a mocks en lugar de sandboxes reales.
- Sin resource limits.
- Restart: `no`.

Archivo YAML completo con todos los overrides.

#### 5.3 `docker-compose.staging.yml`

Override para staging:
- Image desde ACR (tag `:staging-{commit-sha}` o `:staging-latest`).
- Sin build local.
- Caddy con TLS staging (Let's Encrypt staging para no agotar quotas).
- Ports cerrados al exterior excepto Caddy (443, 80).
- Env vars apuntando a sandboxes reales (Stripe test keys, Novita, SW Sapien pruebas).
- Resource limits blandos.
- Restart: `unless-stopped`.
- Backup service scheduled (cron-like) habilitado.
- `sqlserver` con volume persistente montado.

#### 5.4 `docker-compose.prod.yml`

Override para producción alpha:
- Image desde ACR (tag `:v{semver}` o `:prod-{commit-sha}`).
- Caddy con TLS Let's Encrypt producción.
- Solo 80 y 443 expuestos al exterior.
- Env vars via Docker secrets (no archivos `.env` crudos).
- Resource limits estrictos:
  - API: `cpus: '2', memory: 3G`
  - Web: `cpus: '0.5', memory: 512M`
  - Admin: `cpus: '0.5', memory: 512M`
  - SQL Server: `cpus: '3', memory: 8G`
  - Redis: `cpus: '0.5', memory: 1G`
  - Seq: `cpus: '0.5', memory: 2G`
- `restart: always` con backoff.
- `logging` configurado con rotación (json-file con max-size 10m, max-file 3).
- `read_only: true` donde sea posible + `tmpfs` para /tmp.
- `user: 1000:1000` non-root.
- `cap_drop: [ALL]` y solo las capabilities necesarias.
- Backup service activo cada 6 horas.
- Health checks más agresivos (más frecuentes, threshold menor).

### 6. Archivos `.env` Templates Documentados

#### 6.1 `.env.dev.template`

```bash
# ============================================================
# TallerPro - Dev Environment (local machine)
# Copia este archivo a `.env.dev` y llena los valores.
# NO committees .env.dev (está en .gitignore).
# ============================================================

# --- Application ---
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080
TZ=America/Mexico_City

# --- Connection Strings ---
SQLSERVER_PASSWORD=DevPassw0rd!Change
ConnectionStrings__Default=Server=sqlserver,1433;Database=TallerPro_Dev;User Id=sa;Password=${SQLSERVER_PASSWORD};TrustServerCertificate=True;MultipleActiveResultSets=true
ConnectionStrings__Redis=redis:6379

# --- Observability ---
Seq__ServerUrl=http://seq:80
Seq__ApiKey=dev-seq-api-key

# --- JWT ---
JWT__SigningKey=dev-only-signing-key-change-in-prod-abcdef1234567890
JWT__Issuer=https://api.dev.tallerpro.mx
JWT__Audience=tallerpro-hybrid

# --- Stripe (MOCK) ---
Stripe__ApiBaseUrl=http://stripe-mock:8080
Stripe__SecretKey=sk_test_mock_key
Stripe__WebhookSecret=whsec_mock

# --- Novita AI (MOCK) ---
Novita__BaseUrl=http://novita-mock:8080
Novita__ApiKey=mock-novita-key

# --- SW Sapien (MOCK) ---
SwSapien__BaseUrl=http://sw-sapien-mock:8080
SwSapien__ApiKey=mock-pac-key

# --- Email (Mailpit) ---
Brevo__ApiKey=mock
Email__SmtpHost=mailpit
Email__SmtpPort=1025

# --- Cloudflare Access (disabled in dev) ---
CloudflareAccess__Enabled=false

# ... (resto de variables)
```

#### 6.2 `.env.staging.template`

Mismo formato pero con:
- `ASPNETCORE_ENVIRONMENT=Staging`.
- Sandboxes reales (Stripe test keys, Novita, SW Sapien pruebas).
- Cloudflare Access habilitado apuntando a staging tenant.
- Connection strings a SQL Server real.
- Secrets indicados con `# SOPS-ENCRYPTED` y su reemplazo real en `.env.staging.sops.yaml`.

#### 6.3 `.env.prod.template`

Mismo formato pero con:
- `ASPNETCORE_ENVIRONMENT=Production`.
- **Todas las keys sensibles marcadas como `# KEY_VAULT:name-of-secret`** para indicar que vienen de Key Vault, no del archivo.
- Stripe live keys, Novita prod, SW Sapien producción, etc.

### 7. Secrets Management

#### 7.1 Dev

- Archivo `.env.dev` plano (gitignored).
- Valores ficticios que apuntan a mocks o no requieren secrets reales.
- Onboarding: `cp .env.dev.template .env.dev && scripts/seed-dev.sh`.

#### 7.2 Staging

- Archivos cifrados `.env.staging.sops.yaml` committeados al repo.
- Cifrado con sops + age (keys de team en GitHub Secrets + laptops de admins).
- Decifrado local con: `sops -d .env.staging.sops.yaml > .env.staging`.
- Auto-decrypt en GitHub Actions via `AGE_KEY` secret.

Incluye código de:
- `.sops.yaml` config file.
- Ejemplo de `.env.staging.sops.yaml`.
- Workflow de GitHub Actions que lo desencripta en deploy.

#### 7.3 Producción

Opción A: **Docker secrets + file mounts** (si Compose Swarm o Docker Stack):
- Secrets definidos con `echo "value" | docker secret create name -`.
- Referenciados en compose como `/run/secrets/name`.
- App lee de archivo, no de env var.

Opción B: **Azure Key Vault + Managed Identity** (si en Azure VM):
- `Microsoft.Extensions.Configuration.AzureKeyVault` en Program.cs.
- App pulls secrets en startup.
- Rotation automática posible.

**Recomendación justificada** con tradeoffs. Si alpha es en Hetzner/Contabo, Opción A. Si migras a Azure VM como bridge antes de ACA, Opción B.

### 8. Scripts Operacionales

Código bash completo (ejecutable) de:

#### 8.1 `build/scripts/up.sh`

```bash
#!/usr/bin/env bash
set -euo pipefail

ENV="${1:-dev}"
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

case "$ENV" in
  dev|staging|prod) ;;
  *) echo "Uso: $0 {dev|staging|prod}"; exit 1;;
esac

# Verifica que exista el .env correspondiente
ENV_FILE="${PROJECT_ROOT}/build/env/.env.${ENV}"
if [[ ! -f "$ENV_FILE" ]]; then
  echo "ERROR: no existe $ENV_FILE"
  echo "Copia desde template: cp ${ENV_FILE}.template ${ENV_FILE}"
  exit 2
fi

# Si staging o prod, desencriptar sops
# ...

# Up con base + override
docker compose \
  --env-file "$ENV_FILE" \
  -f "${PROJECT_ROOT}/build/compose/docker-compose.base.yml" \
  -f "${PROJECT_ROOT}/build/compose/docker-compose.${ENV}.yml" \
  up -d

# Espera a healthy
echo "Esperando servicios healthy..."
"${PROJECT_ROOT}/build/scripts/health-check.sh" "$ENV"
```

#### 8.2 `build/scripts/down.sh`, `logs.sh`, `migrate.sh`, `backup.sh`, `restore.sh`

Código completo de cada uno.

#### 8.3 `build/scripts/seed-dev.sh`

Script para bootstrapping dev:
- Levanta solo `sqlserver`.
- Espera a que esté ready (loop con `sqlcmd`).
- Corre migrations.
- Seed de data de prueba: 2 tenants, 3 branches, 10 users, catálogos, plans, 1 super-admin founder.
- Levanta el resto del stack.

#### 8.4 `build/scripts/health-check.sh`

Script que consulta `/health` de cada servicio hasta que todos retornan 200 OK. Con timeout configurable y reporte claro.

#### 8.5 `build/scripts/backup.sh`

Script de backup completo:
- `mssql-tools` en contenedor one-shot para dump de SQL Server.
- Redis BGSAVE + copy del RDB.
- Seq snapshot (con API).
- Tarball encriptado con age.
- Upload a Cloudflare R2 o Azure Blob.
- Notificación Slack con tamaño + checksum.

Cron schedule recomendado por ambiente (en `docker-compose.staging/prod.yml` con profile `backup`):
- Staging: diario 3 AM.
- Prod: cada 6 horas + WAL continuo (si usas SQL Server Standard con AGs) o log shipping.

#### 8.6 `build/scripts/restore.sh`

Script interactivo que:
- Lista backups disponibles.
- Permite seleccionar punto en el tiempo.
- Descarga, descifra, verifica checksum.
- Confirma con el usuario (prompt "estás seguro?").
- Restaura SQL Server + Redis + Seq.

### 9. Caddy Configuration por Ambiente

#### 9.1 `Caddyfile.dev` (no se usa — puertos directos).

#### 9.2 `Caddyfile.staging`

Config completa:
```caddy
{
    email admin@tallerpro.mx
    acme_ca https://acme-staging-v02.api.letsencrypt.org/directory
}

staging-api.tallerpro.mx {
    reverse_proxy api:8080 {
        health_uri /health
        health_interval 10s
    }
    header {
        Strict-Transport-Security "max-age=31536000"
        X-Content-Type-Options "nosniff"
        X-Frame-Options "DENY"
        Content-Security-Policy "default-src 'self'"
    }
    encode zstd gzip
}

staging-app.tallerpro.mx {
    reverse_proxy web:8080
    # ... similar headers
}

staging-admin.tallerpro.mx {
    reverse_proxy admin:8080
    # ... similar headers
}
```

#### 9.3 `Caddyfile.prod`

Similar pero:
- ACME producción (Let's Encrypt real).
- Rate limiting.
- WAF básico (rules de Caddy).
- Logs a stdout en formato JSON para captura por Serilog sidecar opcional.

### 10. Mocks para Dev

#### 10.1 Stripe Mock

Dockerfile + mappings WireMock JSON:
- Simula `customers.create`, `subscriptions.create`, `meter_events.create`, `checkout.sessions.create`.
- Simula webhooks (dev puede `curl` al API para simular eventos).
- Retorna fixtures consistentes.

Alternativa: usar imagen oficial `stripemock/stripe-mock`.

#### 10.2 Novita Mock

Responde a `/chat/completions` con respuesta fija de DeepSeek-like JSON.
Simula streaming con SSE mock.

#### 10.3 SW Sapien Mock

Responde a `/timbrar` con UUID ficticio + XML timbrado mock. Simula cancel.

#### 10.4 Mailpit

Servicio oficial, UI en puerto 8025 para ver emails que enviaría la app sin mandarlos realmente.

### 11. CI/CD GitHub Actions Integrado

#### 11.1 `.github/workflows/docker-build.yml`

Workflow que:
- Trigger: push a `main` o tag `v*`.
- Matrix: api, web, admin, migrations.
- Steps:
  1. Checkout code.
  2. Setup QEMU + Buildx (para multi-arch amd64+arm64).
  3. Login a ACR.
  4. Build + push con tags:
     - `{service}:{commit-sha}` (siempre).
     - `{service}:staging-latest` (si rama develop).
     - `{service}:prod-latest` (si tag semver).
     - `{service}:v{semver}` (si tag).
  5. **Trivy scan** (security) — falla el build si CRITICAL vulns.
  6. SBOM generation con Syft.
  7. Cache layer con `type=gha` para builds subsecuentes rápidos.

#### 11.2 `.github/workflows/deploy-staging.yml`

- Trigger: push a `develop`.
- Steps:
  1. SSH a VPS staging.
  2. `docker compose pull`.
  3. `docker compose up -d --no-deps --build api web admin` (rolling).
  4. Run migrations one-shot.
  5. Smoke tests contra `/health`.
  6. Notify Slack.

#### 11.3 `.github/workflows/deploy-prod.yml`

Similar pero con **GitHub Environments + manual approval**:
- Required reviewers: founder.
- Pre-deploy: backup automático.
- Deploy con blue-green strategy (corre nuevo stack en puertos alternos, switch Caddy, tumba viejo).
- Smoke tests aggressive.
- Auto-rollback si falla.

### 12. Health Checks Rigurosos

Para cada servicio, define health check en Dockerfile y compose:

**API**:
```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health/ready"]
  interval: 30s
  timeout: 5s
  start_period: 30s
  retries: 3
```

Endpoints C# que el API debe exponer (en P4 o P7):
- `/health/live` — liveness (solo proceso vivo).
- `/health/ready` — readiness (puede servir requests, DB + Redis accesibles).
- `/health/startup` — startup (inicialización completa).

**SQL Server**:
```yaml
healthcheck:
  test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"$$SA_PASSWORD\" -Q 'SELECT 1' -C -N -o /dev/null"]
  interval: 30s
  start_period: 60s
```

**Redis**:
```yaml
healthcheck:
  test: ["CMD", "redis-cli", "ping"]
  interval: 10s
```

**Seq**, **Caddy**, **Mailpit**: define health checks específicos.

### 13. Resource Limits por Ambiente (Tabla)

Tabla con CPU/RAM/PIDs por servicio por ambiente (dev sin limits, staging blandos, prod estrictos).

### 14. Volúmenes Persistentes y Backup Strategy

Tabla de volúmenes:

| Volumen | Servicio | Contenido | Criticidad | Backup |
|---------|----------|-----------|------------|--------|
| `sqlserver-data` | sqlserver | DB TallerPro | CRÍTICO | Cada 6h prod, diario staging |
| `sqlserver-log` | sqlserver | Transaction logs | CRÍTICO | Continuo (log shipping) prod |
| `redis-data` | redis | Sessions + cache | Medio | Diario (RDB snapshot) |
| `seq-data` | seq | Logs | Medio | Semanal |
| `caddy-data` | caddy | TLS certs | Bajo (regenerable) | No |
| `caddy-config` | caddy | Config cache | Bajo | No |
| `mailpit-data` | mailpit (dev) | Emails de prueba | Ninguna | No |

Script de backup cubre los críticos.

### 15. Security Hardening Checklist

- [ ] Todas las imágenes pineadas a tags específicos (no `:latest`).
- [ ] Non-root user en todos los Dockerfiles de app.
- [ ] `read_only: true` donde es posible + `tmpfs` para /tmp.
- [ ] `cap_drop: [ALL]` en todos los contenedores.
- [ ] `security_opt: [no-new-privileges:true]`.
- [ ] Trivy scan en CI.
- [ ] SBOM generation con Syft.
- [ ] Secrets nunca en imágenes, siempre en runtime.
- [ ] Network interno (no expuesto) para comunicación entre servicios.
- [ ] Solo Caddy expone puertos al exterior.
- [ ] Logs no contienen PII (enricher Serilog en P6 ya lo cubre).
- [ ] Volúmenes con permisos restrictivos (`chown` al user de la app).
- [ ] Rate limiting en Caddy.
- [ ] CSP estricto en headers.
- [ ] SSH al VPS con key-only, no password; fail2ban activo.
- [ ] `docker scan` manual pre-deploy mayor.
- [ ] Healthchecks completos (no falsa sensación de healthy).

### 16. Runbook Operacional

Documento markdown completo `docs/runbooks/docker-operations.md`:

- **Cómo hacer deploy manual** (para contingencias cuando CI/CD falla).
- **Cómo leer logs en vivo** (`docker compose logs -f api`).
- **Cómo entrar a un contenedor** (`docker compose exec api /bin/bash`).
- **Cómo correr migrations fuera del flujo normal**.
- **Cómo hacer rollback** (restore previo + down + up con imagen vieja).
- **Cómo debuggear performance** (`docker stats`, `docker top`, `docker logs`).
- **Cómo agregar una nueva variable de entorno** (proceso end-to-end).
- **Cómo rotar secrets** (Stripe key, JWT signing key, DB password).
- **Cómo escalar horizontalmente** (no es MVP pero dejarlo documentado).
- **Lista de todos los puertos y qué sirve cada uno**.
- **Contactos de escalation** si algo falla 3AM.

### 17. Migration Path: Compose Alpha → Azure Container Apps Beta

Sección breve explicando:
- Las mismas imágenes Docker sirven para ACA (no hay que reconstruir).
- Qué cambia: `docker-compose.prod.yml` se reemplaza por `azure-container-apps.bicep`.
- Los Dockerfiles no cambian.
- Los health checks se reusan (ACA los consume).
- Los resource limits se traducen directo.
- Los secrets migran de Docker secrets a Azure Key Vault (más fácil en ACA con managed identity).
- El reverse proxy se reemplaza por Azure Front Door + ACA ingress.

Esto da confianza al founder de que el trabajo de P9 **no se pierde** cuando escalemos.

### 18. Tests de Docker

Tests mínimos en CI:
- `docker compose config` (valida sintaxis).
- Build de todas las imágenes.
- Trivy scan (security).
- `docker compose up -d` en CI + health check + teardown (smoke test del stack completo).

---

## Restricciones de la Respuesta

- **Archivos ejecutables**, no pseudocódigo. Dockerfiles y compose files deben funcionar con `docker compose config` y `docker buildx build` sin errores.
- Usa sintaxis Compose v2 (sin `version:` en top-level).
- Comentarios en todos los archivos donde la decisión no sea obvia.
- Imágenes pineadas siempre a versiones específicas.
- Non-root users siempre.
- Health checks siempre.
- Longitud target: ~12,000-14,000 tokens.

---

## Al final de tu respuesta

Genera **"ADR Update — Docker Multi-Environment"** con decisiones específicas cementadas:
- Imagen base elegida (aspnet 9.0 Alpine vs Chiseled Ubuntu).
- Estrategia de secrets por ambiente.
- Backup strategy específica con RPO/RTO.
- Reverse proxy elegido (Caddy confirmed).
- Migration path a ACA.
