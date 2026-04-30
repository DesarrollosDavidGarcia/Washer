# Prompt 12 — Enterprise Deployments (VPS Dedicada por Cliente)

## Contexto del Proyecto

**TallerPro**: SaaS multitenant + multitaller para talleres mecánicos en México.

**Stack**: .NET Core 9 + Blazor Hybrid + MudBlazor + EF Core + SQL Server + Docker + Caddy + Seq + Redis.

**Decisión fundacional (de P11)**:
- Plan Enterprise ($24,000 MXN/mes + $49,999 setup + contrato 24 meses sin aumento) tiene **modelo de deployment dedicado**: cada cliente Enterprise recibe su propia VPS aislada con su stack completo.
- Los otros 4 planes (Starter, Básico, Pro, Business) viven en el stack pool compartido.
- **Observabilidad centralizada**: cada Enterprise envía logs a Seq central del founder vía HTTPS.
- **SQL Server Web Edition** (~$1,200 USD/mes, legal para SaaS, mismo stack).
- **VPS Hetzner CX31** por default (8 vCPU dedicated, 16 GB RAM, 160 GB NVMe, ~$35 USD/mes).
- **Provisioning automatizado** desde admin portal con un botón.
- **Dominio custom** (`app.franquiciaX.com` o similar) por Enterprise.

**Schema DB** (de P11): tabla `EnterpriseDeployments` con info de cada VPS + `EnterpriseDeploymentEvents` para histórico.

**Relación con otros prompts**:
- P3: base del schema.
- P4: super-admin portal donde vive el módulo `/admin/enterprises`.
- P9: Dockerfiles reutilizables (el Enterprise usa las mismas imágenes).
- P11: planes y feature flags (Enterprise tiene `DEDICATED_DEPLOYMENT` feature).

---

## Tu Rol

Actúa como **DevOps Staff Engineer + Platform Engineer** con experiencia comprobada en:
- Provisioning automatizado de infraestructura (Ansible, Terraform, cloud-init).
- Hetzner Cloud API + alternativa multi-cloud (Azure, AWS, Contabo).
- Gestión de N instancias de un mismo stack con orquestación centralizada.
- Caddy + Let's Encrypt para dominios custom dinámicos.
- Log shipping centralizado (Seq remote ingestion, Vector, Fluent Bit).
- Pipelines CI/CD para rollouts distribuidos multi-ambiente.
- SSH key management + secrets rotation en fleets.

Responde con **código ejecutable**: Ansible playbooks, scripts bash, C# del admin portal, Terraform si aplica, docker-compose, Caddyfiles, GitHub Actions workflows.

---

## Alcance de ESTE prompt (P12)

Entregar la **arquitectura completa de Enterprise deployments dedicados**.

**SÍ incluir**:
1. Arquitectura de control plane: cómo el admin portal orquesta N VPS Enterprise.
2. Terraform/Hetzner Cloud API para provisionar VPS.
3. Ansible playbook para bootstrap inicial del stack en la VPS nueva.
4. Docker Compose específico `docker-compose.enterprise.yml` (diferente al pool).
5. Caddyfile dinámico con dominio custom + TLS Let's Encrypt.
6. Log shipping centralizado: cada Enterprise VPS → Seq maestro.
7. Pipeline de updates distribuidos (actualizar versión en fleet).
8. Backup strategy dedicada (cada Enterprise tiene backups propios + snapshot centralizado).
9. Módulo admin portal `/admin/enterprises` con wizard de creación + dashboard de fleet.
10. Runbook operacional Enterprise.
11. Health monitoring centralizado.
12. Deprovisioning flow (fin de contrato, suspensión, terminación).
13. Disaster recovery por Enterprise.
14. Pricing transparencia (setup fee breakdown, infra cost, operational cost).

**NO incluir**:
- El stack pool multi-tenant (está en P9).
- Feature flags (está en P11).
- Schema DB (está en P11).

---

## Formato de Respuesta Esperado

### 1. Assumptions a validar

2-3 cuestionamientos al founder. Ejemplos:
- "¿Hetzner Cloud es proveedor único o soportamos multi-cloud desde día 1? (Multi-cloud agrega complejidad pero reduce concentración de riesgo.)"
- "¿Dominio custom lo gestiona el cliente Enterprise (él apunta el DNS) o nosotros? (Si nosotros, necesitamos acceso a su DNS vía API.)"
- "¿La VPS Enterprise es gestionada por TallerPro o el cliente tiene acceso SSH también? (Recomendación: gestionada por nosotros, cliente no tiene SSH.)"

### 2. Arquitectura de Control Plane

Diagrama ASCII:

```
┌─────────────────────────────────────────────────────────────────┐
│                   TallerPro Control Plane                       │
│                   (admin.tallerpro.mx)                          │
│                                                                  │
│  ┌──────────────────┐        ┌──────────────────────────────┐  │
│  │  Master DB       │        │  Admin Portal MVC+MudBlazor  │  │
│  │  (EnterpriseDeployments,  │  /admin/enterprises          │  │
│  │   Tenants, etc.)          │  Wizards de provisioning    │  │
│  └──────────────────┘        └──────────────────────────────┘  │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Orchestration Service                                    │  │
│  │  - Terraform executor (Hetzner provisioning)             │  │
│  │  - Ansible runner (stack bootstrap + updates)            │  │
│  │  - SSH key vault (Azure Key Vault)                       │  │
│  │  - Health monitor (background worker)                    │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Central Seq (logs de TODOS los Enterprises + pool)      │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                           ▲        ▲        ▲
         HTTPS log ship    │        │        │
         HTTPS health chk  │        │        │
         SSH ops           │        │        │
                           │        │        │
        ┌──────────────────┘        │        └──────────────────┐
        │                            │                            │
        ▼                            ▼                            ▼
┌────────────────┐         ┌────────────────┐         ┌────────────────┐
│ Enterprise #1  │         │ Enterprise #2  │   ...   │ Enterprise #N  │
│ VPS Hetzner    │         │ VPS Hetzner    │         │ VPS Hetzner    │
│ app.cliente1.com│        │ app.cliente2.com│        │ app.clienteN.com│
│                │         │                │         │                │
│ - API Docker   │         │ - API Docker   │         │ - API Docker   │
│ - Web Docker   │         │ - Web Docker   │         │ - Web Docker   │
│ - SQL Web Ed.  │         │ - SQL Web Ed.  │         │ - SQL Web Ed.  │
│ - Redis        │         │ - Redis        │         │ - Redis        │
│ - Caddy + TLS  │         │ - Caddy + TLS  │         │ - Caddy + TLS  │
│ - Serilog→Central│       │ - Serilog→Central│       │ - Serilog→Central│
└────────────────┘         └────────────────┘         └────────────────┘
```

### 3. Provisioning Automatizado — Pipeline Completo

Flujo end-to-end cuando el founder clickea "Provisionar Enterprise" en admin portal:

**Paso 1: Input del founder (wizard de 5 pasos)**:
- Datos del cliente: nombre empresa, contactos, RFC, dirección.
- Dominio custom deseado: `app.franquiciaX.com`.
- Región de VPS: `nbg1` (Nuremberg), `fsn1` (Falkenstein), `hel1` (Helsinki), `ash` (Ashburn), `hil` (Hillsboro). Recomendación para MX: `ash` (East Coast US, latencia <80ms a México).
- Tamaño VPS: CX31 default / CX41 opcional / CPX41 AMD opcional.
- Versión TallerPro inicial: última stable.
- Configuración inicial: N branches iniciales, 1 admin user, superadmin founder pre-configurado.

**Paso 2: Creación del registro en DB central**:
```csharp
public async Task<Guid> CreateEnterpriseDeploymentAsync(CreateEnterpriseRequest req)
{
    var deployment = new EnterpriseDeployment
    {
        Id = Guid.NewGuid(),
        CompanyName = req.CompanyName,
        CustomDomain = req.Domain,
        VpsProvider = "Hetzner",
        VpsRegion = req.Region,
        // ... campos iniciales
        Status = "Provisioning",
    };
    await _db.EnterpriseDeployments.AddAsync(deployment);
    await _db.SaveChangesAsync();
    
    // Encolar job Hangfire
    _jobs.Enqueue<EnterpriseProvisioningJob>(j => j.ExecuteAsync(deployment.Id));
    
    return deployment.Id;
}
```

**Paso 3: Background job de provisioning** (`EnterpriseProvisioningJob`):

Código C# completo que ejecuta secuencia:

```csharp
public async Task ExecuteAsync(Guid deploymentId)
{
    var dep = await _db.EnterpriseDeployments.FindAsync(deploymentId);
    
    try
    {
        await RecordEventAsync(deploymentId, "ProvisioningStarted");
        
        // Step 1: Crear VPS en Hetzner via API
        var vps = await _hetzner.CreateServerAsync(new HetznerCreateServerRequest
        {
            Name = $"tp-ent-{dep.Id:N}",
            ServerType = "cx31",
            Image = "ubuntu-22.04",
            Location = dep.VpsRegion,
            SshKeys = new[] { _sshKeyId }, // founder SSH key pre-registrada
        });
        dep.VpsIpAddress = vps.PublicNet.Ipv4.Ip;
        dep.VpsHostname = vps.Name;
        await _db.SaveChangesAsync();
        await RecordEventAsync(deploymentId, "VpsCreated", new { vps.Id, vps.PublicNet.Ipv4.Ip });
        
        // Step 2: Esperar VPS ready
        await _hetzner.WaitForServerRunningAsync(vps.Id, TimeSpan.FromMinutes(3));
        
        // Step 3: Configurar DNS (si usamos Cloudflare como registrar)
        await _cloudflare.CreateDnsRecordAsync(dep.CustomDomain, dep.VpsIpAddress);
        await RecordEventAsync(deploymentId, "DnsConfigured");
        
        // Step 4: Esperar DNS propagation (dig test)
        await WaitForDnsResolutionAsync(dep.CustomDomain, dep.VpsIpAddress);
        
        // Step 5: Ejecutar Ansible playbook
        var ansibleResult = await _ansible.RunPlaybookAsync(
            playbook: "build/enterprise/provision.yml",
            inventory: new[] { dep.VpsIpAddress },
            extraVars: new
            {
                deployment_id = dep.Id,
                custom_domain = dep.CustomDomain,
                company_name = dep.CompanyName,
                version = dep.DesiredVersion,
                central_seq_url = _config["Seq:PublicUrl"],
                central_seq_api_key = await _vault.GetSecretAsync($"seq-api-key-{dep.Id}"),
                stripe_customer_id = dep.StripeCustomerId,
                db_sa_password = GenerateSecurePassword(),
                app_admin_email = req.AdminEmail,
                app_admin_password = GenerateSecurePassword(),
            });
        await RecordEventAsync(deploymentId, "StackBootstrapped");
        
        // Step 6: Health check
        await WaitForHealthyAsync(dep.CustomDomain, TimeSpan.FromMinutes(5));
        
        // Step 7: Seed inicial (first admin, branches, catálogos)
        await _orchestrator.SeedInitialDataAsync(dep);
        await RecordEventAsync(deploymentId, "SeedCompleted");
        
        // Step 8: Update status
        dep.Status = "Active";
        dep.ProvisionedAt = DateTime.UtcNow;
        dep.CurrentVersion = dep.DesiredVersion;
        await _db.SaveChangesAsync();
        
        // Step 9: Notificar founder + cliente
        await _notifier.SendEnterpriseProvisionedEmailsAsync(dep);
        await _slack.PostAsync("#enterprises", $"✅ Enterprise {dep.CompanyName} provisioned at {dep.CustomDomain}");
        
        await RecordEventAsync(deploymentId, "ProvisioningCompleted");
    }
    catch (Exception ex)
    {
        dep.Status = "ProvisioningFailed";
        await _db.SaveChangesAsync();
        await RecordEventAsync(deploymentId, "ProvisioningFailed", new { ex.Message });
        await _slack.PostAsync("#alerts", $"❌ Enterprise provisioning failed: {dep.CompanyName} — {ex.Message}");
        throw;
    }
}
```

### 4. Ansible Playbook `build/enterprise/provision.yml`

Playbook completo ejecutable:

```yaml
---
- name: Bootstrap TallerPro Enterprise Stack
  hosts: all
  become: yes
  vars:
    deployment_dir: /opt/tallerpro
    
  tasks:
    - name: Update apt cache
      apt:
        update_cache: yes
        cache_valid_time: 3600
    
    - name: Install prerequisites
      apt:
        name:
          - ca-certificates
          - curl
          - gnupg
          - lsb-release
          - fail2ban
          - ufw
          - jq
        state: present
    
    - name: Install Docker
      shell: |
        curl -fsSL https://get.docker.com | sh
      args:
        creates: /usr/bin/docker
    
    - name: Enable Docker
      systemd:
        name: docker
        enabled: yes
        state: started
    
    - name: Configure UFW firewall
      ufw:
        rule: allow
        port: "{{ item }}"
        proto: tcp
      loop: [22, 80, 443]
    
    - name: Enable UFW
      ufw:
        state: enabled
        policy: deny
    
    - name: Configure fail2ban
      copy:
        dest: /etc/fail2ban/jail.local
        content: |
          [sshd]
          enabled = true
          maxretry = 3
          bantime = 3600
      notify: restart fail2ban
    
    - name: Create deployment directory
      file:
        path: "{{ deployment_dir }}"
        state: directory
        mode: '0750'
    
    - name: Copy docker-compose.enterprise.yml
      template:
        src: templates/docker-compose.enterprise.yml.j2
        dest: "{{ deployment_dir }}/docker-compose.yml"
        mode: '0640'
    
    - name: Copy Caddyfile
      template:
        src: templates/Caddyfile.enterprise.j2
        dest: "{{ deployment_dir }}/Caddyfile"
    
    - name: Generate .env file
      template:
        src: templates/.env.enterprise.j2
        dest: "{{ deployment_dir }}/.env"
        mode: '0600'
    
    - name: Login to Azure Container Registry
      shell: |
        echo "{{ acr_password }}" | docker login {{ acr_url }} -u {{ acr_username }} --password-stdin
    
    - name: Pull images
      shell: docker compose -f {{ deployment_dir }}/docker-compose.yml pull
    
    - name: Start stack
      shell: docker compose -f {{ deployment_dir }}/docker-compose.yml up -d
    
    - name: Wait for API healthy
      uri:
        url: "https://{{ custom_domain }}/health/ready"
        status_code: 200
      retries: 30
      delay: 10
    
    - name: Run DB migrations
      shell: |
        docker compose -f {{ deployment_dir }}/docker-compose.yml run --rm migrations
    
    - name: Configure automated backup cron
      cron:
        name: "TallerPro backup"
        minute: "0"
        hour: "*/6"
        job: "{{ deployment_dir }}/scripts/backup.sh"
    
    - name: Configure log shipping systemd timer
      template:
        src: templates/log-shipper.timer.j2
        dest: /etc/systemd/system/tallerpro-log-shipper.timer
      notify: reload systemd
    
  handlers:
    - name: restart fail2ban
      systemd:
        name: fail2ban
        state: restarted
    
    - name: reload systemd
      systemd:
        daemon_reload: yes
```

### 5. `docker-compose.enterprise.yml` Template

Specific para single-tenant dedicated. Diferencias clave vs pool:
- 1 sola DB (no hay multi-tenant, es un solo tenant grande).
- SQL Server Web Edition (no Developer ni Standard).
- Backup volume dedicado.
- Log shipping configurado.

Código completo del YAML jinja template.

### 6. Caddyfile Template para Dominio Custom

```
{{ custom_domain }} {
    reverse_proxy api:8080 {
        health_uri /health/ready
        health_interval 10s
    }
    
    header {
        Strict-Transport-Security "max-age=31536000"
        X-Content-Type-Options "nosniff"
        X-Frame-Options "SAMEORIGIN"
        Content-Security-Policy "default-src 'self'; img-src 'self' data: https:; script-src 'self' 'unsafe-inline'"
    }
    
    encode zstd gzip
    
    log {
        output file /var/log/caddy/access.log
        format json
    }
}
```

Caddy gestiona TLS automático con Let's Encrypt, sin configuración adicional.

### 7. Log Shipping Centralizado

Cada Enterprise VPS corre un **log shipper** que envía logs a Seq central.

Opciones evaluadas:
- **Seq HTTP ingestion** nativo: Serilog dentro del API ya escribe a Seq. Config apunta a `central.tallerpro.mx/seq` con API key dedicada.
- **Vector** (alternativa): más flexible pero agrega complejidad.
- **Fluent Bit**: similar.

Decisión: **Seq HTTP ingestion directo desde Serilog**. Cada Enterprise tiene API key única:
- Genera al provisionar.
- Stored en Key Vault.
- Inyectada al `.env` del Enterprise durante Ansible.
- Seq central dashboard filtra por property `DeploymentId`.

Config Serilog del Enterprise (mismo de P6 pero con sink adicional):

```csharp
.WriteTo.Seq(
    serverUrl: ctx.Configuration["Seq:CentralServerUrl"],
    apiKey: ctx.Configuration["Seq:CentralApiKey"])
.Enrich.WithProperty("DeploymentId", ctx.Configuration["Enterprise:DeploymentId"])
.Enrich.WithProperty("EnterpriseName", ctx.Configuration["Enterprise:Name"])
```

### 8. Pipeline de Updates Distribuidos

Cuando sale una versión nueva de TallerPro:

1. CI/CD builds + pushes imágenes a ACR con tag `v2.5.0`.
2. Admin portal lista todos los Enterprises con columna "Versión actual".
3. Founder clickea "Update all Enterprises to v2.5.0" o uno por uno.
4. Por cada Enterprise, se ejecuta workflow:

```yaml
# .github/workflows/enterprise-update.yml
name: Update Enterprise Deployment
on:
  workflow_dispatch:
    inputs:
      deployment_id:
        required: true
      target_version:
        required: true

jobs:
  update:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Fetch deployment info
        # Query master DB via API
      - name: Run Ansible update playbook
        run: |
          ansible-playbook build/enterprise/update.yml \
            -i "${{ env.vps_ip }}," \
            -e "target_version=${{ inputs.target_version }}"
      - name: Health check
      - name: Rollback on failure
      - name: Update master DB
      - name: Notify Slack
```

Ansible `update.yml` hace:
- Backup snapshot pre-update.
- `docker compose pull` nueva versión.
- `docker compose up -d --no-deps api web admin` (rolling restart).
- Run migrations one-shot.
- Smoke tests.
- Rollback automático si falla.

### 9. Backup Strategy Dedicada

Cada Enterprise tiene:
- **Backup SQL Server cada 6 horas** con `mssql-tools` en container one-shot.
- **Backup Redis diario** (BGSAVE + RDB copy).
- **Backup Seq semanal** (snapshot API).
- **Upload a Cloudflare R2** con bucket por Enterprise: `tp-enterprise-backups/{DeploymentId}/...`.
- **Retención 30 días** para daily, 12 semanas para weekly, 12 meses para monthly.
- **Tarball encriptado** con age (key per Enterprise almacenada en Vault).

Script `backup.sh` completo.

**Restore flow**: admin portal tiene botón "Restore from backup" con selector de snapshot, confirmación explícita doble, ejecuta Ansible restore playbook.

### 10. Módulo Admin Portal `/admin/enterprises`

Vista principal `EnterprisesList.razor`:
- Tabla con todos los Enterprise deployments:
  - Nombre empresa, dominio, versión, health, último deploy, contrato vence.
  - Indicadores visuales: 🟢 healthy / 🟡 degraded / 🔴 down.
- Filtros: status, región, versión.
- Botón "+ Crear Enterprise" abre wizard.

Vista detalle `EnterpriseDetail.razor` por deployment:
- **Info**: dominio, IP, VPS specs, región, versión actual vs desired.
- **Métricas últimos 7 días**: CPU, RAM, disk, network (via Hetzner metrics API).
- **Health**: uptime, último check, errores.
- **Uso**: consumo de meters del tenant (CFDI, WhatsApp, AI, storage).
- **Contrato**: inicio, fin, valor total, pagos realizados, próximos cargos.
- **Eventos**: timeline de `EnterpriseDeploymentEvents`.
- **Acciones**:
  - Actualizar versión.
  - Triggear backup manual.
  - Restore desde snapshot.
  - Suspender (detiene stack).
  - Terminar (con confirmación doble + período de gracia 7 días).
  - Ver logs en central Seq (deep link con filtro pre-aplicado).
  - Redimensionar VPS (escalar CX31 → CX41 o AMD alternatives).

Wizard `CreateEnterprise.razor` (5 pasos con `MudStepper`):
1. **Datos empresa**: nombre, RFC, contactos.
2. **Dominio**: input + validación DNS (dig live test).
3. **Infraestructura**: región, tamaño VPS.
4. **Configuración inicial**: branches iniciales, admin user, configuración básica.
5. **Confirmar + precio total**: muestra setup fee + primer trimestre + confirma.

Al confirmar, encola job y redirige a detalle con progress bar.

### 11. Runbook Operacional

Documento markdown `docs/runbooks/enterprise-operations.md`:

- **Cómo provisionar un Enterprise nuevo** (con screenshots).
- **Cómo actualizar la versión** (individual vs fleet-wide).
- **Cómo hacer rollback** si una actualización falla.
- **Cómo restaurar desde backup**.
- **Cómo escalar VPS** (resize).
- **Cómo agregar sucursal al Enterprise**.
- **Cómo deprovisionar** (fin de contrato o cancelación).
- **Troubleshooting común**:
  - VPS unreachable.
  - Health check failing.
  - Log shipping no funciona.
  - Certificate no renueva.
  - Migrations fallan.
- **Contactos de escalation**.

### 12. Deprovisioning Flow

Cuando el contrato de un Enterprise termina o el cliente cancela:

**Período de gracia 30 días**:
- Stack sigue activo.
- Acceso read-only (no se permite crear nuevas órdenes).
- Cliente recibe multiple warnings.
- Opción de exportar toda su data.

**Al día 30**:
- Stack se detiene.
- VPS sigue viva 7 días más.
- Último backup completo se archiva a cold storage.

**Al día 37**:
- VPS se destruye via Hetzner API.
- Registro en DB marcado como `Terminated`.
- Datos en cold storage se retienen 5 años (compliance SAT).
- Cliente puede solicitar datos en ese período.

### 13. Disaster Recovery

Si una VPS Enterprise muere catastróficamente:
1. Alerta automática Slack `#enterprises` + email founder + llamada cliente.
2. Admin portal ofrece "Recover deployment" que ejecuta:
   - Provisiona VPS nueva en la misma región.
   - Restaura desde último backup bueno (RPO 6 horas).
   - Actualiza DNS.
   - Health check.
   - Notifica cliente cuando está operativa.
3. RTO target: **< 4 horas** (incluyendo detección + provisioning + restore).
4. Cliente recibe credit proporcional al downtime según SLA 99.9% contractual.

### 14. Pricing Transparencia — Breakdown Real

Documento público (NDA-protected) para cliente Enterprise antes de firmar:

| Concepto | Mensual MXN |
|---|---|
| Infraestructura VPS (Hetzner CX31) | $900 |
| SQL Server Web Edition | $1,200 |
| Backups + Storage R2 | $300 |
| Bandwidth + networking | $200 |
| Monitoring + Seq centralizado | $400 |
| Account manager dedicado (prorrateado) | $4,500 |
| Soporte 24/7 SLA 4h | $2,000 |
| Updates + mantenimiento | $1,000 |
| **Subtotal costo directo** | **$10,500** |
| Margen TallerPro | $13,500 |
| **Precio final** | **$24,000** |

Setup fee $49,999 amortiza: provisioning (10h × $500), migración data (8h × $500), training presencial (40h × $500), documentación custom (6h × $500), contingencia 20%.

---

## Restricciones de la Respuesta

- Código Ansible + C# + YAML + Bash ejecutables.
- Templates Jinja2 completos.
- Usa .NET 9 idioms.
- Longitud target: ~13,000-15,000 tokens.

---

## Al final de tu respuesta

Genera **"ADR Update — Enterprise Dedicated Deployments"** con decisiones cementadas (stack técnico Enterprise, provisioning approach, observabilidad, backup/DR strategy, runbook highlights).
