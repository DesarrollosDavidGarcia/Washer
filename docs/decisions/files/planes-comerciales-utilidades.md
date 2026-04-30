# TallerPro — Planes Comerciales y Análisis de Utilidades (v2 Final)

**Versión**: 2.0 (definitiva)
**Última actualización**: 2026-04-22
**Estado**: Aprobada para implementación pre-MVP

---

## Resumen Ejecutivo

TallerPro opera con **5 planes diferenciados** — 4 en modelo pool multi-tenant (self-service via Stripe) y 1 Enterprise con deployment dedicado (sales-led). El modelo es **pay-and-use immediately sin trial y sin devolución**, con Enterprise en **contrato 24 meses sin aumento de precio**.

**Punto dulce del modelo**:
- Barrera de entrada baja ($399 Starter) captura mercado muy pequeño.
- Escalabilidad natural vía sucursales y consumo overage.
- Enterprise a $24,000 + $49,999 setup + contrato 24 meses genera **revenue predecible de $576k-625k por cliente** con margen bruto 54%.
- Sin trial acelera cash flow pero requiere onboarding concierge fuerte para evitar churn temprano.

**Path to break-even**: **~95-110 tenants pool + 2-3 Enterprise activos** (~mes 7-9 en escenario base).

---

## 1. Los 5 Planes — Definitivos

### 1.1 Starter — $399 MXN/mes

**Target**: Talleres muy pequeños (1-2 bahías, 1 mecánico + dueño). Actualmente en Excel/cuaderno.

| Concepto | Valor |
|---|---|
| Precio base | $399 MXN/mes |
| Sucursales incluidas | 1 (fija, no escala) |
| Precio sucursal extra | N/A — para escalar migra a Básico |
| Usuarios | 3 máximo |
| Órdenes/mes | 150 máximo (al llegar: prompt upgrade) |

**Pools incluidos** (pool fijo, no por sucursal):
- CFDI: 50/mes
- WhatsApp utility: 100/mes
- AI Básica: 50 pre-órdenes/mes
- Storage: 2 GB

**Features bloqueadas**:
- ❌ IA Avanzada (RAG sobre manuales)
- ❌ Multi-almacén
- ❌ Roles personalizados
- ❌ API
- ❌ Dashboard BI
- ❌ Impersonation support

**Soporte**: Email 48h, WhatsApp Business de soporte general.

**Upgrade path**: Si el taller crece (2da sucursal, >150 órdenes/mes, necesita IA RAG) → upgrade a Básico con un click.

### 1.2 Básico — $899 MXN/mes

**Target**: Talleres pequeños-medianos (3-8 bahías, 2-5 mecánicos). Posiblemente 1-2 sucursales.

| Concepto | Valor |
|---|---|
| Precio base | $899 MXN/mes |
| Sucursales incluidas | 1 |
| Precio sucursal extra | $299 MXN/mes |
| Usuarios | Ilimitados |
| Órdenes/mes | Ilimitadas |

**Pools incluidos** (por sucursal):
- CFDI: 200/mes
- WhatsApp utility: 500/mes
- AI Básica: 500 pre-órdenes/mes
- AI Avanzada (RAG): 50/mes
- Storage: 5 GB

**Features incluidas**:
- ✅ Multi-sucursal
- ✅ Multi-almacén básico (1 almacén por sucursal)
- ✅ Roles predefinidos (Owner, Admin, Advisor, Mechanic, Warehouse, Accountant)
- ✅ IA Avanzada básica (RAG) con pool reducido
- ❌ Roles personalizados
- ❌ API
- ❌ Dashboard BI

**Soporte**: Chat in-app, SLA 24h.

### 1.3 Pro — $1,499 MXN/mes

**Target**: Talleres medianos (10-25 bahías, multi-sucursal típicamente 2-4 sucursales).

| Concepto | Valor |
|---|---|
| Precio base | $1,499 MXN/mes |
| Sucursales incluidas | 1 |
| Precio sucursal extra | $449 MXN/mes |
| Usuarios | Ilimitados |
| Órdenes/mes | Ilimitadas |

**Pools incluidos** (por sucursal):
- CFDI: 500/mes
- WhatsApp utility: 1,500/mes
- AI Básica: 1,500/mes
- AI Avanzada (RAG): 200/mes
- Storage: 15 GB

**Features incluidas** (además de Básico):
- ✅ Multi-almacén avanzado (múltiples almacenes por sucursal)
- ✅ Roles personalizados (crea roles con permisos específicos)
- ✅ Dashboard BI básico
- ✅ API read-only
- ✅ Impersonation support (soporte técnico en vivo con autorización)
- ❌ RFC por sucursal
- ❌ API full con webhooks
- ❌ White-label

**Soporte**: Chat in-app + WhatsApp Business dedicado, SLA 12h.

### 1.4 Business — $3,499 MXN/mes

**Target**: Cadenas y concesionarias (25+ bahías, 3+ sucursales, múltiples RFCs).

| Concepto | Valor |
|---|---|
| Precio base | $3,499 MXN/mes |
| Sucursales incluidas | 3 |
| Precio sucursal extra | $699 MXN/mes (desde la 4ta) |
| Usuarios | Ilimitados |
| Órdenes/mes | Ilimitadas |

**Pools incluidos** (compartidos entre todas las sucursales — `PER_PLAN`):
- CFDI: 2,000/mes total
- WhatsApp utility: 5,000/mes total
- AI Básica: 10,000/mes total (fair use)
- AI Avanzada (RAG): 500/mes total
- Storage: 50 GB total

**Features incluidas** (además de Pro):
- ✅ RFC por sucursal (cada sucursal emite CFDI con su CSD propio)
- ✅ Dashboard BI avanzado con drill-down multi-sucursal + forecasting
- ✅ API full con webhooks
- ✅ Priority queue en IA (latencia <2s garantizada vs <10s en Pro)
- ✅ White-label light (color primario + logo custom en emails y portal)
- ✅ Backup diario dedicado (vs cada 6h compartido)
- ✅ SLA 99.9%

**Soporte**: 24/7 WhatsApp + email, SLA 4h, account manager senior dedicado.

**Descuento contrato anual**: -10% pagando adelantado (opcional).

### 1.5 Enterprise — $24,000 MXN/mes (Deployment Dedicado)

**Target**: Franquicias automotrices, redes grandes (10+ sucursales), empresas con requerimientos de compliance elevados (datos aislados físicamente), casos especiales.

| Concepto | Valor |
|---|---|
| **Precio base** | **$24,000 MXN/mes** |
| **Setup fee** | **$49,999 MXN one-time** |
| **Contrato** | **24 meses sin aumento de precio** |
| **Pago** | **Trimestral adelantado** (Stripe Invoicing o SPEI) |
| Sucursales | Negociables (default: hasta 15 sin cargo extra) |
| Usuarios | Ilimitados |
| Órdenes | Ilimitadas |

**Pools incluidos** (`PER_PLAN` expandido):
- CFDI: 3,000/mes
- WhatsApp utility: 10,000/mes
- AI Básica: Ilimitada fair use
- AI Avanzada (RAG): 1,500/mes
- Storage: 200 GB

**Features incluidas** (todas de Business +):
- ✅ **Deployment dedicado**: VPS exclusiva Hetzner CX31 en región de preferencia
- ✅ **SQL Server Web Edition** dedicado (datos físicamente aislados)
- ✅ **Dominio custom**: `app.franquiciaX.com` (o el que elija el cliente)
- ✅ **White-label completo**: branding, colores, logos, textos — todo customizable
- ✅ **App Hybrid con icono/cert custom** (MSIX + APK repackaged)
- ✅ **SLA 99.9% contractual** con penalty clauses
- ✅ **Implementación acompañada**: 40h consultoría incluida
- ✅ **Training presencial** en sitio (o remoto) para el corporate
- ✅ **Integraciones custom** negociables en fase setup
- ✅ **Account Manager senior** dedicado + canal Slack directo
- ✅ **Priority queue en IA** (latencia <1s)
- ✅ **Backup cada 6h + snapshot semanal**
- ✅ **Precio congelado 24 meses** (sin aumento durante vida del contrato)

**Valor total del contrato Enterprise** (24 meses):
- Setup: $49,999
- Mensualidad × 24: $24,000 × 24 = $576,000
- Overage estimado (10%): ~$57,600
- **Total: $683,599 MXN por Enterprise cerrado**

---

## 2. Matriz Comparativa de los 5 Planes

| Feature | Starter $399 | Básico $899 | Pro $1,499 | Business $3,499 | Enterprise $24,000 |
|---|:---:|:---:|:---:|:---:|:---:|
| Sucursales incluidas | 1 fija | 1 | 1 | 3 | Hasta 15 |
| Precio sucursal extra | N/A | $299 | $449 | $699 | Negociable |
| Usuarios | 3 | Ilimitados | Ilimitados | Ilimitados | Ilimitados |
| Órdenes/mes | 150 | ∞ | ∞ | ∞ | ∞ |
| CFDI incluidos/mes | 50 | 200/suc | 500/suc | 2,000/plan | 3,000/plan |
| WhatsApp utility | 100 | 500/suc | 1,500/suc | 5,000/plan | 10,000/plan |
| IA Básica | 50 | 500/suc | 1,500/suc | 10k/plan | ∞ fair use |
| IA Avanzada (RAG) | ❌ | 50/suc | 200/suc | 500/plan | 1,500/plan |
| Storage | 2 GB | 5 GB/suc | 15 GB/suc | 50 GB/plan | 200 GB/plan |
| Multi-almacén | ❌ | Básico | Avanzado | Avanzado | Avanzado |
| Roles personalizados | ❌ | ❌ | ✅ | ✅ | ✅ |
| API | ❌ | ❌ | Read-only | Full + webhooks | Full |
| Dashboard BI | ❌ | ❌ | Básico | Avanzado | Avanzado |
| RFC por sucursal | ❌ | ❌ | ❌ | ✅ | ✅ |
| White-label | ❌ | ❌ | ❌ | Light | Completo |
| Dominio custom | ❌ | ❌ | ❌ | ❌ | ✅ |
| Deployment dedicado | ❌ | ❌ | ❌ | ❌ | ✅ |
| SLA | 99% | 99% | 99% | 99.9% | 99.9% contractual |
| Soporte | Email 48h | Chat 24h | Chat+WA 12h | 24/7 4h | AM dedicado |
| Contrato | Mensual | Mensual | Mensual | Mensual (opcional anual) | **24 meses sin aumento** |
| Trial | ❌ | ❌ | ❌ | ❌ | ❌ |
| Devolución | ❌ | ❌ | ❌ | ❌ | ❌ |
| Setup fee | — | — | — | — | $49,999 |

---

## 3. Overage Pricing (Universal)

Se cobra cuando el tenant excede pool incluido. Mismas tarifas para todos los planes:

| Meter | Precio overage (MXN) | Costo variable nuestro | Margen bruto |
|-------|---------------------|------------------------|--------------|
| CFDI timbres | $2.50 | $1.50 | 40% |
| WhatsApp utility | $0.50 | $0.35 | 30% |
| WhatsApp marketing | $1.80 | $1.20 | 33% |
| SMS México | $0.85 | $0.50 | 41% |
| AI Básica | $0.30 | $0.02 | 93% |
| AI Avanzada (RAG) | $2.00 | $0.20 | 90% |
| Storage adicional | $4.00/GB | $0.30/GB | 93% |

**Overage se cobra mensualmente** al cierre del ciclo de facturación (junto con el plan base).

---

## 4. Política de Ciclo de Vida

### 4.1 Signup (sin trial)

1. Usuario ingresa datos en `/registro`.
2. Redirect a Stripe Checkout con pago inmediato.
3. Webhook confirma pago → tenant provisionado con `Status=Active`.
4. Email de bienvenida con credenciales + link MSIX + agendamiento de onboarding call.
5. CS llama en primeras 24h para onboarding concierge.

### 4.2 Onboarding Concierge (obligatorio)

Dado que no hay trial, el onboarding es **crítico para retención**:
- Llamada de bienvenida 15 min primeras 24h.
- Setup de catálogos iniciales con apoyo (refacciones, servicios, mecánicos).
- Primera orden creada con acompañamiento.
- Primer CFDI timbrado con acompañamiento.
- Check-in día 7, 14, 30.

**Métrica**: 80%+ de tenants activos deben haber timbrado al menos 1 CFDI en primeras 72h. Si no, llamada de rescate.

### 4.3 Upgrade/Downgrade

- **Upgrade** (ej: Básico → Pro): inmediato con proration automática de Stripe. Acceso a nuevos features instantáneo.
- **Downgrade** (ej: Pro → Básico): efectivo al **siguiente ciclo de facturación**. Previene abuso intraciclo.
- **Downgrade con pérdida de data activa** (ej: tiene roles custom y baja a Básico): bloqueado hasta limpiar. UI explica qué debe cleanup-ear antes.

### 4.4 Enterprise — Flow Especial

1. Lead entra por formulario `/enterprise` o contacto directo.
2. Discovery call con founder/CS (60 min).
3. Propuesta custom con precio + setup fee + contrato 24 meses.
4. POC de 2-4 semanas opcional ($15,000 MXN aplicables al setup si firma).
5. Contrato firmado + primer pago trimestral.
6. Provisioning automatizado desde admin portal (wizard de 5 pasos, ~30 min).
7. Migración de data + training presencial/remoto.
8. Go-live con soporte reforzado primeras 2 semanas.

### 4.5 Sin Devolución (Pay Firm)

**Política clara en aviso de privacidad y términos**:
> "Los pagos realizados a TallerPro son no reembolsables. El usuario puede cancelar su suscripción en cualquier momento; la cancelación es efectiva al siguiente ciclo de facturación. El período pagado se mantiene utilizable hasta el fin del ciclo."

**Excepciones** (case-by-case a discreción de founder):
- Falla crítica del producto que impide uso por >72h (credit, no cash).
- Error de cobro duplicado (obvious refund).
- Enterprise con issue material de SLA que invoca penalty clauses.

---

## 5. Unit Economics por Plan

### 5.1 Starter ($399)

**Consumo típico**: 30 CFDI, 50 WhatsApp, 20 IA.

| Concepto | Valor MXN |
|---|---|
| Revenue | $399 |
| Stripe fees (3.6% + $3) | $17 |
| Infra prorrateada | $25 |
| COGS variable | $57 (CFDI + WA + IA + storage) |
| Soporte humano prorrateado | $30 |
| **COGS total** | **$129** |
| **Contribución bruta** | **$270** |
| **Margen bruto** | **68%** ✅ |

**CAC objetivo**: <$800 MXN (payback 3 meses).
**LTV** (18 meses promedio): $270 × 18 = $4,860 MXN.
**LTV/CAC**: 6.1× ✅ excelente.

### 5.2 Básico ($899, 1 sucursal)

**Consumo típico**: 120 CFDI, 300 WhatsApp, 300 IA, 30 RAG.

| Concepto | Valor MXN |
|---|---|
| Revenue | $899 + overage mínimo $80 = **$979** |
| Stripe fees | $38 |
| Infra prorrateada | $40 |
| COGS variable | $230 |
| Soporte humano prorrateado | $35 |
| **COGS total** | **$343** |
| **Contribución bruta** | **$636** |
| **Margen bruto** | **65%** ✅ |

**LTV/CAC**: 5.2× ✅.

### 5.3 Pro ($1,499, 1.8 sucursales promedio)

**Consumo típico**: 400 CFDI, 900 WhatsApp, 800 IA, 60 RAG.

| Concepto | Valor MXN |
|---|---|
| Revenue (base + 0.8 sucursal + overage) | $1,499 + $359 + $150 = **$2,008** |
| Stripe fees | $75 |
| Infra prorrateada | $60 |
| COGS variable | $650 |
| Soporte humano prorrateado | $60 |
| **COGS total** | **$845** |
| **Contribución bruta** | **$1,163** |
| **Margen bruto** | **58%** ✅ |

**LTV/CAC**: 4.5× ✅.

### 5.4 Business ($3,499, 4 sucursales promedio)

**Consumo típico**: 1,400 CFDI, 3,500 WhatsApp, 7,000 IA, 350 RAG.

| Concepto | Valor MXN |
|---|---|
| Revenue (base + 1 sucursal extra + overage) | $3,499 + $699 + $400 = **$4,598** |
| Stripe fees | $172 |
| Infra prorrateada | $120 |
| COGS variable | $1,870 (pool casi tope) |
| Account manager prorrateado (1 AM × 30 tenants) | $400 |
| **COGS total** | **$2,562** |
| **Contribución bruta** | **$2,036** |
| **Margen bruto** | **44%** ✅ |

**LTV/CAC**: 3.8× ✅.

### 5.5 Enterprise ($24,000 + $49,999 setup)

**Consumo típico**: 2,000 CFDI, 7,500 WhatsApp, uso intensivo IA.

| Concepto | Valor MXN |
|---|---|
| Revenue mensual (base + overage típico) | $24,000 + $2,400 = **$26,400** |
| Stripe fees (invoicing 1.6% vs 3.6% tarjeta) | $425 |
| Infra VPS dedicada (Hetzner CX31) | $900 |
| SQL Server Web Edition | $1,200 |
| Backups + R2 + bandwidth | $500 |
| Monitoring + Seq central | $400 |
| COGS variable (CFDI overage, WA, etc.) | $2,370 |
| AM senior dedicado (1 AM × 8 Enterprises) | $4,500 |
| Soporte 24/7 SLA 4h | $2,000 |
| Updates + mantenimiento | $1,000 |
| **COGS total** | **$13,295** |
| **Contribución bruta** | **$13,105** |
| **Margen bruto** | **50%** ✅ |

**Valor total contrato 24 meses**:
- Setup: $49,999
- Mensualidades × 24: $576,000
- Overage total: ~$57,600
- **Revenue total**: ~$683,599
- **COGS total 24 meses**: ~$319,080
- **Contribución bruta total**: ~$364,519
- **Margen bruto 24m**: ~53%

**LTV por Enterprise**: ~$365,000 MXN (si no renueva, solo contrato inicial).
**CAC objetivo Enterprise**: <$50,000 MXN (payback 3-4 meses).
**LTV/CAC**: 7.3× ✅ excelente.

---

## 6. Proyecciones 12 Meses

### 6.1 Supuestos del Modelo

- **Mix pool**: 25% Starter, 35% Básico, 30% Pro, 10% Business.
- **Enterprise**: ventas lentas pero alto valor. Target 3 cerrados en año 1.
- **Churn pool**: 4% primer trimestre → 3% mensual sostenido.
- **Onboarding concierge**: reduce churn temprano 40% vs sin onboarding.
- **Referrals**: 15% de nuevos tenants vienen de referidos (CAC $0).

### 6.2 Escenario Conservador

Adopción lenta, marketing limitado, 1 Enterprise.

| Mes | Starter | Básico | Pro | Business | Enterprise | MRR Pool (MXN) | MRR Ent. | MRR Total |
|-----|---------|--------|-----|----------|------------|----------------|----------|-----------|
| 1 | 2 | 3 | 2 | 0 | 0 | $6,595 | $0 | $6,595 |
| 3 | 8 | 10 | 7 | 1 | 0 | $25,290 | $0 | $25,290 |
| 6 | 15 | 20 | 15 | 3 | 0 | $60,950 | $0 | $60,950 |
| 9 | 22 | 30 | 24 | 5 | 1 | $97,780 | $24,000 | $121,780 |
| 12 | 30 | 42 | 32 | 8 | 1 | $138,500 | $24,000 | **$162,500** |

**MRR fin de año 1**: $162,500 MXN. ARR ~$1.95M MXN.
**Setup fees recibidos**: 1 × $49,999 = $49,999 (one-time).
**Revenue total año 1**: ~$1.5M MXN (ramp-up gradual).

### 6.3 Escenario Base (esperado)

Ejecución normal, referrals activos, primer partner firmado mes 4-5, 2 Enterprise.

| Mes | Pool total | Enterprise | MRR Total (MXN) |
|-----|-----------|------------|------------------|
| 1 | 8 | 0 | $10,100 |
| 3 | 28 | 0 | $38,500 |
| 6 | 65 | 1 | $103,000 |
| 9 | 110 | 2 | $177,000 |
| 12 | **170** | **2** | **$260,000** |

**MRR fin de año 1**: $260,000 MXN. ARR ~$3.12M MXN.
**Setup fees recibidos**: 2 × $49,999 = $99,998.
**Revenue total año 1**: ~$2.4M MXN.

### 6.4 Escenario Optimista

Partner franquicia firma mes 5 aportando 20+ pool + 1 Enterprise. 3 Enterprise total año 1.

| Mes | Pool total | Enterprise | MRR Total (MXN) |
|-----|-----------|------------|------------------|
| 1 | 10 | 0 | $12,500 |
| 3 | 35 | 0 | $48,000 |
| 6 | 95 | 2 | $195,000 |
| 9 | 195 | 3 | $355,000 |
| 12 | **320** | **3** | **$528,000** |

**MRR fin de año 1**: $528,000 MXN. ARR ~$6.3M MXN.
**Setup fees recibidos**: 3 × $49,999 = $149,997.
**Revenue total año 1**: ~$4.5M MXN.

---

## 7. Break-Even Analysis

### 7.1 Costos Fijos Mensuales (founder sin salario)

| Concepto | MXN/mes |
|---|---|
| 3 devs senior .NET | $90,000 |
| 1 PM | $35,000 |
| Infra pool + herramientas | $6,000 |
| Marketing (SEO + ads base) | $8,000 |
| Legal + contable | $3,500 |
| **Total fijos** | **$142,500** |

### 7.2 Contribución por Tenant (mix 25/35/30/10)

**Contribución blended pool**:
- 0.25 × $270 + 0.35 × $636 + 0.30 × $1,163 + 0.10 × $2,036 = **~$842 MXN/mes**.

**Contribución Enterprise**: $13,105 MXN/mes.

### 7.3 Break-Even con Enterprise

**Sin Enterprise**: $142,500 / $842 = **170 tenants pool**.

**Con 1 Enterprise**: ($142,500 - $13,105) / $842 = **154 tenants pool**.
**Con 2 Enterprises**: ($142,500 - $26,210) / $842 = **138 tenants pool**.
**Con 3 Enterprises**: ($142,500 - $39,315) / $842 = **123 tenants pool**.

**Conclusión**: cada Enterprise cerrado **acelera break-even equivalente a 15-16 tenants pool**.

### 7.4 Cuándo Alcanzamos Break-Even

| Escenario | Tenants pool necesarios + Ents | Mes break-even |
|-----------|----------------------------------|-----------------|
| Conservador | 170 pool + 1 Ent | **Mes 15-16** |
| Base | 140 pool + 2 Ents | **Mes 8-9** |
| Optimista | 110 pool + 3 Ents | **Mes 6-7** |

**Con founder con salario** ($192,500 fijos): sumar +3-4 meses a cada escenario.

### 7.5 Runway Requerido

| Escenario | Capital necesario (MXN) |
|-----------|-------------------------|
| Conservador (founder sin salario) | ~$1.8M |
| Base (founder sin salario) | ~$900k |
| Optimista (founder sin salario) | ~$500k |

**Recomendación**: levantar seed $1.2-1.5M MXN cubre escenario base + buffer para contingencias.

---

## 8. Estrategia de Ventas Enterprise

El Enterprise es **palanca clave** para acelerar break-even. Cada cierre vale ~$683k MXN.

### 8.1 Perfil de Target Enterprise

- Cadenas automotrices con 5+ sucursales.
- Franquicias automotrices (Midas, KW, Motor Tec, etc.).
- Concesionarias multimarca con taller propio.
- Flotillas corporativas con taller interno.
- Empresas de aftermarket con red de talleres afiliados.

### 8.2 Pipeline de Ventas

1. **Prospecting** (semanas 1-2):
   - LinkedIn Sales Navigator targeting.
   - Referrals de tenants pool grandes.
   - Asistencia a ANPACT (asociación de autopartes) eventos.

2. **Discovery** (semana 3):
   - Call inicial 30 min con tomador de decisión.
   - Identificar pain points actuales (ERP caro, sistemas custom, etc.).

3. **Demo customizada** (semana 4):
   - 60 min con equipo técnico del prospecto.
   - Enfoque en features Enterprise (dominio custom, RFC multi-sucursal, SLA).

4. **POC opcional** (semanas 5-6):
   - Entorno sandbox por 2 semanas con sus datos reales.
   - $15,000 MXN aplicables al setup si cierran.

5. **Propuesta formal** (semana 7):
   - Contrato 24 meses + setup fee.
   - Personalizaciones negociadas.

6. **Cierre + pago** (semana 8):
   - Firma digital.
   - Pago primer trimestre adelantado.

7. **Provisioning + go-live** (semanas 9-12):
   - Automatizado desde admin portal (4 horas).
   - Migración de data (1-2 semanas).
   - Training presencial.

**Ciclo típico**: 8-12 semanas. **Conversión estimada**: 20-30% de leads calificados.

### 8.3 Equipo de Ventas Enterprise

**Año 1**: Founder + 1 CS senior (el que hace onboarding concierge del pool).

**Año 2**: Agregar 1 Enterprise Sales Rep full-time (~$50k/mes MXN + comisión 10%).

---

## 9. Plan de Retención y Upsell

### 9.1 Retención Pool (reducir churn)

**Leading indicators de churn**:
- Sin login en 7 días.
- 0 órdenes creadas en primera semana.
- 0 CFDI timbrados en primer mes.
- Reducción >40% de órdenes/mes vs baseline.

**Acciones proactivas**:
- Email automatizado + WhatsApp de CS cuando trigger.
- Call de rescate si sigue inactivo >14 días.
- Oferta de "onboarding refresher" gratis.

### 9.2 Upsell Pool

**Paths naturales**:
- **Starter → Básico**: al llegar al límite de órdenes (150) o necesitar 2da sucursal.
- **Básico → Pro**: al necesitar roles custom, API, BI, o 3+ sucursales.
- **Pro → Business**: al necesitar RFC multi-sucursal, BI avanzado, priority AI, o 4+ sucursales.
- **Business → Enterprise**: al necesitar deployment aislado, white-label completo, o SLA contractual.

**Notificación in-app**: banner prominente cuando el tenant está cerca del límite de features del plan actual.

### 9.3 Retención Enterprise (renovación 24 meses)

- QBR (Quarterly Business Review) con el AM dedicado.
- NPS cada 6 meses con CEO/founder presente.
- Negociación de renovación empieza **6 meses antes** del fin de contrato.
- Incentivo de renovación: -10% si renueva antes de fin de contrato actual + 2 meses adicionales incluidos.

**Target**: 85%+ de renovación Enterprise.

---

## 10. KPIs Comerciales desde Día 1

Dashboard en super-admin portal con actualización diaria:

| KPI | Fórmula | Target Mes 6 | Target Mes 12 |
|-----|---------|--------------|---------------|
| MRR Pool | Σ(revenue mensual pool) | $80k | $220k |
| MRR Enterprise | Σ(revenue mensual Ent) | $24k | $48k+ |
| **MRR Total** | Pool + Enterprise | **$104k** | **$268k+** |
| ARR | MRR × 12 | $1.25M | $3.2M+ |
| Tenants pool activos | Count(Status=Active, not Enterprise) | 70 | 170 |
| Enterprise activos | Count(IsEnterpriseDedicated=1) | 1 | 2-3 |
| ARPU pool | MRR pool / tenants pool | ~$1,100 | ~$1,300 |
| ARPU Enterprise | MRR Ent / Enterprise count | $24,000 | $24,000 |
| Mix pool % (S/B/P/Biz) | Distribución | 25/35/30/10 | 20/30/35/15 |
| Overage % del MRR | Overage / MRR | 10% | 12% |
| Pool trial conversion | N/A (no hay trial) | — | — |
| Pool churn mensual | Canceled / Base inicio | <5% | <3% |
| Enterprise churn | Canceled / Base inicio | 0% | 0% (contrato 24m) |
| NRR (Net Revenue Retention) | (MRR inicio + expansion - churn) / MRR inicio | >100% | >115% |
| CAC pool | Costo adquisición / nuevos pool | <$3,000 | <$2,000 |
| CAC Enterprise | Costo adquisición / nuevos Ent | <$50,000 | <$40,000 |
| LTV pool blended | Contribución × retención | >$10k | >$15k |
| LTV Enterprise | Contribución × contrato | ~$365k | ~$365k |
| LTV/CAC pool | LTV / CAC | >3.5× | >4.5× |
| LTV/CAC Enterprise | LTV / CAC | >7× | >8× |
| Gross margin blended | (Rev - COGS) / Rev | >45% | >55% |

---

## 11. Recomendaciones Accionables Pre-Launch

### Críticas (antes de lanzar MVP)

1. ✅ **Implementar los 5 planes en el schema desde día 1** (no agregar después).
2. ✅ **Onboarding concierge obligatorio** — sin trial, el onboarding es el substitute. CS calendar bloqueado para agendar llamadas.
3. ✅ **Pricing page clara con calculadora interactiva** — ayuda al prospecto a elegir plan correcto.
4. ✅ **Términos y condiciones explícitos sobre no-reembolso** — legal validado antes de lanzar.
5. ✅ **Feature flags implementados** — infraestructura lista para enforzar límites sin workarounds.

### Importantes (primeras 4 semanas post-launch)

6. **A/B testing de Starter $399 vs $499** con 20 primeros tenants para validar price sensitivity.
7. **Tracking granular de feature usage** para identificar upsell opportunities.
8. **Dashboard super-admin con Enterprise pipeline** — tracking de leads, meetings, cierre esperado.
9. **Documentación publica de todas las features** con tag del plan mínimo requerido.
10. **Proceso formal de onboarding Enterprise** — runbook, checklist, templates.

### Importantes (primeros 6 meses)

11. **Primer caso de estudio** con tenant Pro exitoso para usar en marketing.
12. **Primer partner firmado** (reseller o franquicia pequeña).
13. **Primer Enterprise cerrado** — validar pricing y proceso de sales.
14. **Mediciones NPS mensuales** por plan para segmentar satisfaction.
15. **Evaluación de levantar seed** si MRR > $100k y unit economics sanos.

---

## 12. Riesgos Comerciales y Mitigaciones

| # | Riesgo | Prob | Impacto | Mitigación |
|---|--------|------|---------|------------|
| 1 | Sin trial → conversión baja | Medium | Alto | Onboarding concierge + demo sandbox público en marketing + garantía de satisfacción no-escrita (CS resuelve issues) |
| 2 | Starter canibaliza Básico | Medium | Medium | Features claramente diferenciadas (Starter sin IA Avanzada ni multi-almacén). Marketing posiciona Starter como "para muy pequeños" |
| 3 | Enterprise ventas lentas (0 en año 1) | Medium | Alto | Target 3 Ents pero modelo NO depende de ellos para break-even conservador. Son upside, no base |
| 4 | Enterprise primera VPS tiene problema | Medium | Alto | Runbook de DR probado, backup cada 6h, Ansible rollback automático |
| 5 | Costos ops Enterprise mayor a estimado | Medium | Alto | Contrato 24m sin aumento nos protege de inflación pero expone a cost overruns. Monitoring estricto + ajuste en renovación |
| 6 | Churn temprano pool > 10% | Medium | Muy alto | Sin trial = onboarding concierge obligatorio. Detection de leading indicators. CS proactivo |
| 7 | Stripe cambia fees | Low | Medium | Alternativa Mercado Pago lista para evaluar si Stripe sube significativamente |
| 8 | Meta WhatsApp sube pricing >30% | Medium | Alto | 360dialog / Gupshup como alternativas evaluadas. Overage pricing absorbe algo |
| 9 | SQL Server Web Edition cambia licensing | Low | Alto | Backup plan: migración a PostgreSQL para Enterprises (impacto: 3-4 meses de dev pero viable) |
| 10 | Concentración de revenue en 1 Enterprise | Alto | Alto | No aceptar Enterprise que supere 40% del MRR total sin diversificar primero |

---

## Anexo A: Posicionamiento vs Competencia

| Competidor | Precio rango MXN | Vs TallerPro | Ventaja TallerPro |
|------------|------------------|--------------|--------------------|
| Autocraft | $600-$1,200 | Starter/Básico tier | IA Avanzada, multi-sucursal nativo, offline-first |
| WorkMotor | $1,500-$3,000 | Pro/Business tier | Precio más bajo, IA nativa, UX moderna |
| Manager Pro | $400-$600 | Starter tier | Feature parity + IA + escalable a Pro sin migración |
| Carrok | $800-$1,500 | Básico/Pro tier | RAG manuales, offline-first, stack moderno |
| Odoo Enterprise | $30,000+ | Enterprise tier | **Mismo precio TallerPro pero específico automotriz**, deployment dedicado, IA integrada |
| SAP B1 | $50,000+ | Enterprise tier | TallerPro 50% más barato con mismas capabilities core para talleres |

**Punto clave**: en Enterprise, somos **significativamente más baratos** que ERPs generalistas (SAP, Odoo) con especialización automotriz. En pool, somos **competitivos** con pricing transparente.

---

## Notas Finales

Este documento es la **estrategia comercial definitiva pre-MVP**. Debe validarse con:

1. **10 entrevistas** con dueños de talleres antes de lock del pricing.
2. **Ajustes post-alpha** (primeros 30 días con los 10 tenants pioneros).
3. **Revisión trimestral** con data real para ajustar proyecciones.

**Actualizar este documento cada trimestre** o cuando haya cambio estratégico mayor.
