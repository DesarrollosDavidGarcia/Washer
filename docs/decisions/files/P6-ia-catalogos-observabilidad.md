# Prompt 6 de 7 — IA DeepSeek + Catálogos Genéricos + Observabilidad

## Contexto del Proyecto

**TallerPro**: SaaS multitenant + multitaller para talleres mecánicos en México.

**Stack**: .NET Core 9 + Blazor Hybrid (MAUI) + MudBlazor + Mediator (Othamar) + Mapster + EF Core + SQL Server + SignalR.

**Decisiones fundacionales ya tomadas**:
- **IA**: DeepSeek V3.2 / V3.2-Speciale / R1 vía **Novita.ai** (endpoint OpenAI-compatible: `https://api.novita.ai/openai`).
- SDK: NuGet `OpenAI` (o `Microsoft.Extensions.AI` si prefieres abstracción) apuntando a base URL de Novita.
- Cost ceiling: < $0.15 USD por orden de servicio procesada E2E.
- **PII masking** obligatorio antes de enviar a Novita (placas, nombres, teléfonos, RFC, direcciones → tokens).
- **Catálogos genéricos** para entidades `{Code, Name, Description, DisplayOrder, ExtraJson}`.
- Schema DDL ya existe (P3): `CatalogTypes`, `CatalogItems`, `AIEvents`, `AlertRules`, `AlertEvents`.
- **Observabilidad**: Serilog + Seq self-hosted + enrichers custom (TenantEnricher, BranchEnricher, UserEnricher, ImpersonationEnricher, TraceEnricher, PiiMaskingEnricher).
- Usage tracking (de P5): cada request IA dispara `UsageTracker.RecordAIRequestAsync` con `AIUseCase` enum.
- Aislamiento tenant es invariante (código de P2 existe).

**Casos de uso de IA**:
1. Clasificación de síntoma desde texto/audio/imagen (WhatsApp recepción).
2. Pre-orden estructurada con tool calling JSON.
3. Diagnóstico con RAG sobre manuales técnicos + OBD-II.
4. Resumen ejecutivo semanal al dueño del taller.
5. (NO MVP) Forecasting de inventario con ML.NET.

---

## Tu Rol

Actúa como **Arquitecto Senior con especialidad en**:
- LLMs vía endpoints OpenAI-compatibles en .NET (Novita, Together, Fireworks).
- RAG patterns con vector databases (pgvector, SQL Server Vector Search, Qdrant, Azure AI Search).
- PII masking y compliance LFPDPPP en pipelines de IA.
- Serilog avanzado con enrichers custom y PII masking automático.
- Seq como backend de logs self-hosted.
- Pattern de catálogos dinámicos con componentes UI reutilizables (MudBlazor).

Responde con código C# ejecutable, ejemplos de RAG pipeline, y configuración de observabilidad lista para Docker.

---

## Alcance de ESTE prompt (P6)

Entregar 3 bloques relacionados pero independientes:
1. **IA**: integración Novita + casos de uso + RAG + PII masking.
2. **Catálogos genéricos**: entidades + componente Razor reutilizable + controller.
3. **Observabilidad**: Serilog config completa + Seq setup + alert rules + PII masking enricher.

**SÍ incluir**:
- Código C# ejecutable de integración con Novita vía `OpenAI` SDK.
- Handlers Mediator para cada caso de uso IA.
- Pipeline RAG completo (chunking, embeddings, vector store, reranking, generación con cita).
- PII masking service con código (reversible para display).
- Componente Razor genérico `<CatalogManager>` funcional.
- Endpoints uniformes `/api/catalogs/{code}`.
- Configuración Serilog completa (Program.cs + enrichers custom).
- Docker compose para Seq self-hosted.
- 10 alert rules configuradas con código del `AlertEvaluator`.
- Tests críticos.

**NO incluir**:
- Integración con Stripe / pagos (P5).
- Auth / impersonation (P4).
- Marketing site (P7).

---

## Formato de Respuesta Esperado

### 1. Assumptions a validar
2-3 cuestionamientos sobre decisiones de IA, vector store o catálogos.

### 2. Integración con Novita — Configuración en .NET

Código C# completo:

**Program.cs** setup:
```csharp
builder.Services.AddHttpClient("Novita", client =>
{
    client.BaseAddress = new Uri("https://api.novita.ai/openai/");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
        "Bearer", builder.Configuration["Novita:ApiKey"]);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.CircuitBreaker.MinimumThroughput = 10;
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddScoped<INovitaClient, NovitaClient>();
builder.Services.AddScoped<IPiiMaskingService, PiiMaskingService>();
```

**`INovitaClient`** interface con métodos específicos para cada caso de uso:
```csharp
public interface INovitaClient
{
    Task<ChatCompletionResult> ChatCompleteAsync(ChatCompletionRequest request, CancellationToken ct);
    IAsyncEnumerable<ChatStreamChunk> ChatStreamAsync(ChatCompletionRequest request, CancellationToken ct);
    Task<EmbeddingResult> CreateEmbeddingAsync(string input, string model, CancellationToken ct);
    Task<ToolCallResult<T>> ChatWithToolsAsync<T>(ChatWithToolsRequest request, CancellationToken ct) where T : class;
}
```

Implementación usando `OpenAI` NuGet (o llamadas HTTP directas con `HttpClient` + `System.Text.Json`).

### 3. Selección de Modelos por Caso de Uso

Tabla con cálculo de costo real (tokens × precio):

| Caso | Modelo Novita | Input tokens típico | Output tokens típico | Costo USD/request | Costo MXN/request |
|---|---|---|---|---|---|
| Clasificación síntoma | `deepseek/deepseek-v3.2` non-thinking | 500 | 200 | ~$0.00022 | ~$0.004 |
| Pre-orden tool calling | `deepseek/deepseek-v3.2` + JSON mode | 1,500 | 500 | ~$0.00061 | ~$0.011 |
| Diagnóstico RAG | `deepseek/deepseek-v3.2-speciale` | 4,000 | 800 | ~$0.003 | ~$0.054 |
| Resumen semanal | `deepseek/deepseek-v3.2` | 8,000 | 2,000 | ~$0.003 | ~$0.054 |

**Cost routing**: código C# de `IAIModelRouter` que elige modelo según caso + fallback a modelo económico cuando caso simple.

### 4. Caso de Uso: Recepción Inteligente (Clasificación de Síntoma)

**Flujo**: Cliente manda mensaje WhatsApp con foto/audio/texto → Sistema clasifica → Pre-orden estructurada.

Handler Mediator completo `ClassifySymptomCommand`:
- Input: `{ TenantId, BranchId, CustomerId, ChannelMessageId, Content: { Text?, ImageUrl?, AudioUrl? } }`.
- Steps:
  1. PII masking del texto.
  2. Si hay imagen, descarga y codifica base64 (con límite 2MB).
  3. Si hay audio, transcribe primero (Whisper vía Novita si disponible, o DeepSeek-OCR para sugerencias).
  4. Construye prompt estructurado con system message.
  5. Llama Novita con `deepseek-v3.2` en modo JSON + tool calling.
  6. Valida response contra schema `PreOrderSuggestion`.
  7. Rehidrata PII en el resultado para mostrar al asesor.
  8. Registra `AIEvent` + dispara `UsageTracker.RecordAIRequestAsync`.
  9. Push vía SignalR `OrderHub` al asesor activo para notificar.
- Output: `Result<PreOrderSuggestion>`.

Tool calling schema JSON del `PreOrderSuggestion` con validación.

### 5. Caso de Uso: Diagnóstico con RAG

**Pipeline RAG completo**:

**a) Chunking de manuales técnicos**:
- Estrategia: split por secciones + tablas/diagramas extraídos aparte.
- Para PDFs con OCR: usar `PdfPig` NuGet + heurísticas de estructura.
- Chunk size: 512 tokens con overlap 50.
- Metadata por chunk: marca, modelo, año, sección, página.

**b) Embeddings**:
- Elige modelo: ¿Novita embeddings (si tienen) vs local ONNX (ej: `all-MiniLM-L6-v2`)?
- Trade-off: Novita es caro para volumen grande de embeddings pre-calculados; local es gratis pero add complejidad.
- Recomendación con justificación.

**c) Vector store**:
Matriz comparativa:

| Opción | Costo | Integración .NET | Performance | Operación |
|---|---|---|---|---|
| SQL Server 2025 Vector Search | $0 adicional | Nativa con EF Core | Buena | Un solo stack |
| pgvector | ~$40/mes Postgres instance | Via Npgsql | Excelente | Add stack |
| Qdrant | ~$25/mes cloud | HTTP client | Excelente | Add stack |
| Azure AI Search | ~$75/mes | SDK oficial | Buena | Managed |

Decisión con justificación.

**d) Query pipeline**:
1. Usuario (mecánico) hace consulta: "Toyota Corolla 2015, código P0420, ¿qué reviso primero?".
2. Enrich query con context del vehículo (VIN lookup).
3. Embed query.
4. Vector search top 10 chunks por similarity.
5. Rerank con `bge-reranker-v2-m3` (local ONNX o vía Novita si disponible).
6. Top 3 chunks → llama DeepSeek V3.2-Speciale con RAG prompt.
7. **Guardrail**: respuesta debe incluir cita al manual fuente con página. Si el modelo no cita, la respuesta se descarta y retorna "No encontré información confiable en los manuales".
8. Dashboard muestra sugerencia + enlaces a páginas originales.

Código C# completo del handler.

### 6. PII Masking Service

Código completo de `IPiiMaskingService`:

```csharp
public interface IPiiMaskingService
{
    MaskedContent Mask(string input); // retorna texto con tokens + diccionario reverso scoped
    string Rehydrate(string maskedOutput, MaskingDictionary dictionary); // reconstruye texto original
}
```

Implementación detectora de:
- Placas MX (regex: `[A-Z]{3}-?\d{3,4}` o `\d{3}-?[A-Z]{3}`).
- RFC (regex: `[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}`).
- Teléfonos MX (regex: `(\+?52)?[\s-]?(\d{2,3})[\s-]?\d{3,4}[\s-]?\d{3,4}`).
- Emails.
- VIN (17 chars alfanuméricos).
- Nombres (lista de nombres comunes MX + apellidos — maintained list).

Tokens generados: `[PLATE_1]`, `[RFC_1]`, `[PHONE_1]`, etc. Diccionario scoped al request.

**Tests críticos**:
- Máscara funciona con variaciones (con/sin guiones, con/sin prefijo país).
- Rehydrate revierte perfectamente.
- Texto sin PII queda intacto.

### 7. PII Masking Enricher Automático para Serilog

Código de `PiiMaskingEnricher : ILogEventEnricher` que:
- Intercepta properties del log event.
- Detecta properties con nombres sensibles (`Plate`, `RFC`, `Phone`, `Email`, `Name`, `Address`, `VIN`).
- Aplica masking automático.
- Log property `PiiMasked = true` cuando aplicó masking.

Esto asegura que **ningún dev puede accidentalmente loggear PII cruda**. Capa de defensa independiente del PIIMaskingService manual.

### 8. Configuración Completa de Serilog (Program.cs)

Código completo:

```csharp
builder.Host.UseSerilog((ctx, services, config) =>
{
    config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", ctx.HostingEnvironment.ApplicationName)
        .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
        .Enrich.WithProperty("Version", GitVersion.VersionString)
        .Enrich.WithMachineName()
        .Enrich.With<TenantEnricher>()
        .Enrich.With<BranchEnricher>()
        .Enrich.With<UserEnricher>()
        .Enrich.With<ImpersonationEnricher>()
        .Enrich.With<TraceEnricher>()
        .Enrich.With<PiiMaskingEnricher>()
        .WriteTo.Console(formatter: new CompactJsonFormatter())
        .WriteTo.Seq(
            serverUrl: ctx.Configuration["Seq:ServerUrl"],
            apiKey: ctx.Configuration["Seq:ApiKey"],
            restrictedToMinimumLevel: LogEventLevel.Information,
            queueSizeLimit: 10000)
        .WriteTo.Async(a => a.File(
            path: "/var/log/tallerpro/taller-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            restrictedToMinimumLevel: LogEventLevel.Warning,
            formatter: new CompactJsonFormatter()));
});
```

Código completo de cada enricher (`TenantEnricher`, `BranchEnricher`, `UserEnricher`, `ImpersonationEnricher`, `TraceEnricher`, `PiiMaskingEnricher`) con `IHttpContextAccessor` + `ITenantContext`.

### 9. Setup de Seq Self-Hosted

**docker-compose.yml**:
```yaml
services:
  seq:
    image: datalust/seq:latest
    environment:
      ACCEPT_EULA: Y
      SEQ_FIRSTRUN_ADMINPASSWORDHASH: ${SEQ_ADMIN_PASSWORD_HASH}
    volumes:
      - seq-data:/data
    ports:
      - "5341:80"
    restart: unless-stopped
    networks:
      - tallerpro-net
volumes:
  seq-data:
```

Config adicional:
- Backup strategy (volumen montado en NFS o S3 sync diario).
- API key management (una por ambiente).
- Retention config (90 días hot, opcional archive).
- Sample queries guardadas: "Errores últimas 24h por tenant", "Latencia p95 Novita", "Impersonation sessions".

### 10. Retention Policies de Logs

Tabla con retention por tipo + cold storage:

| Tipo | Hot (Seq) | Archive | Requisito |
|---|---|---|---|
| AuditLog | 90 días | 5 años S3 Glacier | SAT + LFPDPPP |
| MeterEvents | 90 días | 5 años | Auditoría financiera |
| ImpersonationAudits | 90 días | Permanente | Compliance + confianza |
| SupportAccessLog | 90 días | Permanente | LFPDPPP |
| Operational (Information) | 30 días | 1 año | Operación |
| Debug/Verbose | 7 días | No archive | Solo dev |
| AIEvents | 90 días | 1 año anonimizado | Observabilidad IA |

Código del job `LogArchiveJob` que exporta logs a cold storage y los elimina de Seq según policy.

### 11. Alert Rules MVP (10 reglas)

Para cada una: nombre, tipo de métrica, threshold, canal destino, código de detección.

1. **Error rate > 5/min sostenido 5 min** → Slack `#alerts`.
2. **Fatal log** → Slack inmediato + email al founder.
3. **Meter reconciliation drift > 1%** → Slack + email (crítico).
4. **Failed Stripe webhook > 3 en 10 min** → Slack.
5. **SW Sapien error rate > 10% en 5 min** → Slack.
6. **Super-admin login fuera de horario laboral (7pm-7am MX)** → Slack + email founder.
7. **Impersonation session iniciada** → Slack `#admin-audit` info.
8. **Impersonation Full iniciada** → Slack + email al founder.
9. **Novita p99 latencia > 30s** → Slack.
10. **Tenant suspendido por dunning** → Slack + email founder.

Código del `AlertEvaluator` background service que evalúa reglas cada minuto + `AlertRuleEngine` extensible.

### 12. Sistema de Catálogos Genéricos

**Entidades** (DDL ya existe en P3, aquí solo el modelo de dominio):

```csharp
public class CatalogType : Entity<int>
{
    public string Code { get; set; } // "PAYMENT_METHOD"
    public string Name { get; set; }
    public CatalogScope Scope { get; set; } // Tenant | Branch | System
    public bool IsSystem { get; set; }
    public string? MetadataSchema { get; set; } // JSON Schema of ExtraData
    public SortMode SortMode { get; set; }
}

public class CatalogItem : TenantBranchScoped<long>
{
    public int CatalogTypeId { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public string? ExtraData { get; set; } // JSON validado contra MetadataSchema
    public bool IsActive { get; set; }
    // + audit + soft delete
}
```

**Controller uniforme** (Minimal APIs):
```csharp
app.MapGroup("/api/catalogs/{code}")
   .RequireAuthorization()
   .MapCatalogEndpoints();
```

Implementación de `MapCatalogEndpoints` con:
- `GET /` → items del catálogo del branch activo.
- `GET /metadata` → CatalogType + schema.
- `POST /items` → crear.
- `PUT /items/{id}` → editar.
- `DELETE /items/{id}` → soft delete (body: reason).
- `POST /items/{id}/restore`.
- `PATCH /reorder` → bulk DisplayOrder update.

Todos los endpoints pasan por handlers Mediator con validación FluentValidation.

### 13. Componente MudBlazor `<CatalogManager>`

Componente Razor completo `CatalogManager.razor`:
- Parámetros: `CatalogCode`, `Title`, `AllowCreate`, `AllowEdit`, `AllowSoftDelete`, `ShowExtraDataEditor`.
- Fetch inicial de items + metadata del catálogo al cargar.
- `MudDataGrid` con virtualization + paginación server-side + search + sort.
- `MudDialog` para create/edit con `MudForm` validada.
- **Editor dinámico de `ExtraData`** que renderiza campos según el `MetadataSchema` JSON (int, string, bool, enum, date).
- Drag-and-drop para reordenar si `SortMode == Manual` (usa `MudDropContainer`).
- Soft delete con modal de razón obligatoria (min 20 chars).
- Restaurar desde papelera.

Código completo `.razor` + code-behind.

### 14. Seed de CatalogTypes Iniciales

Código del `CatalogSeeder` que corre en startup si la DB está vacía:

```csharp
var seed = new[]
{
    new CatalogType { Code = "PAYMENT_METHOD", Name = "Formas de pago", Scope = CatalogScope.Branch, IsSystem = false, SortMode = SortMode.Manual },
    new CatalogType { Code = "SERVICE_TYPE", Name = "Tipos de servicio", ... },
    new CatalogType { Code = "PART_CATEGORY", Name = "Categorías de refacciones", ... },
    new CatalogType { Code = "SYMPTOM_CATEGORY", Name = "Categorías de síntomas", ... },
    new CatalogType { Code = "UNIT_OF_MEASURE", Name = "Unidades de medida", Scope = CatalogScope.Tenant, ... },
    new CatalogType { Code = "MECHANIC_SPECIALTY", Name = "Especialidades del mecánico", ... },
    new CatalogType { Code = "ORDER_PRIORITY", Name = "Prioridad de orden", ... },
    new CatalogType { Code = "FAILURE_REASON", Name = "Razones de falla", ... },
};
```

Ejemplo de `MetadataSchema` JSON para `PAYMENT_METHOD` con campo extra `RequiresAuthCode: boolean`.

### 15. IA Offline en Cliente Hybrid

Código de:
- Cache de embeddings locales en SQLite del cliente.
- Similaridad por coseno en ONNX (modelo local pequeño).
- Base estática OBD-II embebida (~2 MB JSON comprimido).
- UX honesto cuando no hay conexión: "IA degradada — lookup local disponible".

### 16. Tests Críticos

Tests en `TallerPro.Integration.Tests`:
- `Novita_ChatComplete_SendsPiiMasked` (verifica que PII se enmascara antes de enviar).
- `PiiMasking_Rehydrate_ReconstructsOriginal`.
- `RAG_QueryWithoutCitationInResponse_ReturnsFallback` (guardrail).
- `CatalogManager_CreateItem_RespectsMetadataSchema`.
- `CatalogManager_SoftDelete_RequiresReasonMin20Chars`.
- `Serilog_LogWithPiiProperty_IsMaskedAutomatically`.
- `AlertEvaluator_ErrorRateThreshold_TriggersSlackWebhook`.

Código completo de cada test.

---

## Restricciones de la Respuesta

- **Código C# ejecutable**.
- Usa .NET 9 idioms.
- Convenciones: Mediator (Othamar), Mapster, Serilog, FluentValidation, MudBlazor.
- Prioriza código + configuración ejecutable sobre prosa.
- Longitud target: ~12,000-14,000 tokens.

---

## Al final de tu respuesta

Genera **"ADR Update — AI, Catalogs & Observability"** con decisiones cementadas (especialmente: vector store elegido, modelo de embeddings, estrategia RAG, retention policies).
