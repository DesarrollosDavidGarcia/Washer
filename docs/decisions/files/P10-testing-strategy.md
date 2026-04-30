# Prompt 10 — Estrategia de Testing Unificada

## Contexto del Proyecto

**TallerPro**: SaaS multitenant + multitaller para talleres mecánicos en México.

**Stack**: .NET Core 9 + Blazor Hybrid (MAUI) + MudBlazor + Mediator (Othamar) + Mapster + EF Core + SQL Server + SignalR + Stripe + DeepSeek vía Novita + SW Sapien + Serilog/Seq.

**Decisiones fundacionales ya tomadas (asumir)**:
- Aislamiento tenant es invariante del sistema (8 capas defense-in-depth + Roslyn Analyzer).
- Usage tracking y Stripe Meters son transversales a todo evento facturable.
- Impersonation tiene governance estricta con audit inmutable.
- PII masking dual (antes de Novita + en todos los logs Serilog).
- Soft delete estricto; inmutables no se borran nunca.
- Stack de testing aprobado: xUnit + Shouldly + NSubstitute + Testcontainers + Playwright + NBomber + Stryker.NET + Pact.NET + Coverlet + ReportGenerator.
- CI: GitHub Actions.
- El proyecto `TallerPro.Isolation.Tests` ya se diseñó en P2 (aquí lo integramos al resto).

**Estructura de tests del repo** (ya definida en P1):
```
tests/
├── TallerPro.Domain.Tests/           # Tests unitarios de Domain (sin infra)
├── TallerPro.Application.Tests/      # Tests de handlers Mediator (con mocks)
├── TallerPro.Integration.Tests/      # Tests con DB real via Testcontainers
├── TallerPro.Isolation.Tests/        # Suite dedicada a aislamiento tenant (P2)
├── TallerPro.E2E.Tests/              # Playwright contra stack levantado
├── TallerPro.LoadTests/              # NBomber scenarios
├── TallerPro.ContractTests/          # Pact.NET para integraciones externas
├── TallerPro.Analyzers.Tests/        # Tests del Roslyn analyzer (ya en P2)
└── TallerPro.TestFixtures/           # Proyecto compartido: builders, mocks, helpers
```

---

## Tu Rol

Actúa como **QA Lead + Test Automation Architect Senior** con experiencia comprobada en:
- Test pyramid aplicada a SaaS B2B .NET multitenant en producción real.
- Testcontainers para integration tests con SQL Server + Redis.
- Playwright para E2E de aplicaciones Blazor Hybrid + marketing MVC.
- NBomber para load testing contra APIs .NET con SignalR.
- Stryker.NET para mutation testing en código crítico.
- Pact.NET para contract testing con Stripe, Novita, SW Sapien, WhatsApp.
- Chaos engineering con Polly + Simmy (failure injection).
- Test data builders pattern, fixtures compartidas, helpers.
- Quality gates en CI con coverage thresholds por proyecto.

Responde con **código C# ejecutable, configuración YAML/JSON lista, y pipelines CI funcionales**. No descripciones — artefactos commiteables.

---

## Alcance de ESTE prompt (P10)

Entregar la **estrategia global de testing** del proyecto + código de fundaciones + ejemplos representativos + pipelines CI.

**SÍ incluir**:
1. Estrategia global (pyramid, cobertura por proyecto, ejecución en CI).
2. Stack completo de herramientas con justificación.
3. Proyecto `TallerPro.TestFixtures/` con builders, fixtures, helpers, WebApplicationFactory base.
4. Ejemplos representativos de cada tipo de test (unit, integration, E2E, load, chaos, contract, mutation).
5. Tests específicos críticos (aislamiento integrado, pagos, impersonation, PII, CFDI, sync offline).
6. Quality gates y thresholds.
7. CI integration con GitHub Actions (pipeline completo).
8. Coverage reporting con Coverlet + ReportGenerator.
9. Testing runbook.
10. Anti-patrones a evitar.

**NO incluir**:
- Tests específicos ya cubiertos en P2-P9 que no necesiten actualización (solo referencias a ellos cuando relevante).
- Código de la aplicación misma (otros prompts).

---

## Formato de Respuesta Esperado

### 1. Assumptions a validar

2-3 cuestionamientos sobre estrategia de testing al founder. Ejemplos:
- "¿Aceptable 3-5 minutos de tiempo total de CI en cada PR, o queremos optimizar a < 2 min aunque cueste refactor?"
- "¿Requerimos mutation testing solo en `TallerPro.Security/` y `TallerPro.Application/` (código crítico) o en todo? (Impacto: 10x tiempo de CI si full)."
- "¿El equipo actual (3 devs) puede mantener la disciplina de TDD o empezamos pragmático con 'tests después'?"

### 2. Pirámide de Testing + Estrategia Global

Diagrama ASCII de la pirámide aplicada al proyecto:

```
                    /\
                   /  \    10% E2E (Playwright) - 30-50 tests
                  /----\
                 / Int. \  20% Integration (Testcontainers) - 150-200 tests
                /--------\
               /          \
              / Unit Tests \ 70% Unit (xUnit + mocks) - 500-1000 tests
             /--------------\
```

**Política**:
- Código crítico (`TallerPro.Security/`, `TallerPro.Application/`, handlers de pagos): **cobertura ≥ 90%** + mutation score ≥ 75%.
- Código estándar: **cobertura ≥ 70%**.
- UI Razor/MudBlazor: solo smoke tests E2E (no obsesionarse con unit de componentes).
- Infraestructura EF Core: tests de integración obligatorios (no mockear DB).

**Tiempo de ejecución objetivo**:
- Unit tests: < 30 s total.
- Integration tests: < 3 min total.
- Isolation tests: < 2 min total (ya definido en P2).
- E2E smoke suite: < 5 min (sub-conjunto corre en cada PR).
- E2E full suite: < 20 min (corre nightly).
- Load tests: nightly o pre-release (no en PR).
- Mutation tests: nightly solo en código crítico.

### 3. Stack de Testing — Tabla con Justificación

| Tipo | Herramienta | Licencia | Razón |
|------|------------|----------|-------|
| Test runner | **xUnit** | Apache 2.0 | Estándar .NET, mejor parallelism que NUnit, mejor DX que MSTest |
| Asserts | **Shouldly** | BSD | Más legibles que Xunit.Assert; NO FluentAssertions (comercial desde 2025) |
| Mocks | **NSubstitute** | BSD | Más limpio que Moq; NO Moq (telemetría controversial + comercial en algunas features) |
| DB real | **Testcontainers** | MIT | SQL Server + Redis reales; más confiable que in-memory |
| E2E | **Playwright** | Apache 2.0 | Cross-browser, auto-wait, soporta Blazor bien; vs Selenium es 10x mejor DX |
| Load | **NBomber** | Apache 2.0 | .NET nativo, soporta SignalR, scenarios en C# |
| Mutation | **Stryker.NET** | Apache 2.0 | Solo opción seria para .NET |
| Contract | **Pact.NET** | MIT | Estándar de contract testing, soporta HTTP y mensajería |
| Coverage | **Coverlet** + **ReportGenerator** | MIT | Estándar .NET, integra con todo |
| Chaos | **Simmy** (parte de Polly) | BSD | Failure injection integrado con Polly v8 |
| Snapshot | **Verify** | MIT | Para tests de outputs complejos (DTOs, SQL generado) |
| Benchmarks | **BenchmarkDotNet** | MIT | Para hot paths críticos (meter tracker, middleware tenant) |

### 4. Proyecto `TallerPro.TestFixtures/`

Proyecto **compartido** referenciado por todos los proyectos de tests.

#### 4.1 Estructura

```
tests/TallerPro.TestFixtures/
├── TallerPro.TestFixtures.csproj
├── Builders/
│   ├── TenantBuilder.cs
│   ├── BranchBuilder.cs
│   ├── UserBuilder.cs
│   ├── ServiceOrderBuilder.cs
│   ├── CfdiBuilder.cs
│   ├── PartBuilder.cs
│   ├── CustomerBuilder.cs
│   └── VehicleBuilder.cs
├── Fixtures/
│   ├── DatabaseFixture.cs         # Testcontainers SQL Server compartido
│   ├── RedisFixture.cs            # Testcontainers Redis
│   ├── SeqFixture.cs              # Container Seq para tests de observabilidad
│   ├── WebAppFactory.cs           # WebApplicationFactory<Program> base
│   └── IntegrationTestBase.cs     # Base class de tests de integración
├── Mocks/
│   ├── StripeMockServer.cs        # WireMock para Stripe
│   ├── NovitaMockServer.cs        # WireMock para Novita
│   ├── SwSapienMockServer.cs      # WireMock para SW Sapien
│   ├── WhatsAppMockServer.cs      # WireMock para Meta
│   └── BrevoMockServer.cs
├── Helpers/
│   ├── JwtTokenGenerator.cs
│   ├── TimeProviderFake.cs        # Control de tiempo para tests
│   ├── ClockBuilder.cs
│   ├── FeatureFlagsFake.cs
│   └── SerilogTestSink.cs         # Captura logs en memoria para asserts
└── Categories/
    └── TestCategories.cs           # constantes [Trait("Category", ...)]
```

#### 4.2 `TenantBuilder` — Patrón Builder

Código C# completo del builder con fluent API:

```csharp
namespace TallerPro.TestFixtures.Builders;

public sealed class TenantBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "Test Tenant";
    private TenantStatus _status = TenantStatus.Active;
    private int _planId = 1;
    private string _rfc = "XAXX010101000";
    private DateTime? _trialEndsAt;
    private List<BranchBuilder> _branches = [];
    private List<UserBuilder> _users = [];

    public TenantBuilder WithId(Guid id) { _id = id; return this; }
    public TenantBuilder WithName(string name) { _name = name; return this; }
    public TenantBuilder InTrial(int daysRemaining = 14)
    {
        _status = TenantStatus.Trial;
        _trialEndsAt = DateTime.UtcNow.AddDays(daysRemaining);
        return this;
    }
    public TenantBuilder Suspended() { _status = TenantStatus.Suspended; return this; }
    public TenantBuilder WithBranches(int count)
    {
        for (var i = 0; i < count; i++)
            _branches.Add(new BranchBuilder().WithName($"Sucursal {i + 1}"));
        return this;
    }
    public TenantBuilder WithBranch(Action<BranchBuilder> configure)
    {
        var b = new BranchBuilder();
        configure(b);
        _branches.Add(b);
        return this;
    }
    public TenantBuilder WithUser(Action<UserBuilder> configure) { /* ... */ return this; }

    public Tenant Build()
    {
        return new Tenant
        {
            Id = _id,
            Name = _name,
            Status = _status,
            PlanId = _planId,
            RFC = _rfc,
            TrialEndsAt = _trialEndsAt,
            Branches = _branches.Select(b => b.WithTenantId(_id).Build()).ToList(),
            // ...
        };
    }

    public async Task<Tenant> PersistAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        var tenant = Build();
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(ct);
        return tenant;
    }
}
```

Ejemplo de uso en tests:
```csharp
var tenant = await new TenantBuilder()
    .WithName("Taller Los Primos")
    .WithBranches(3)
    .PersistAsync(Db);
```

Código completo de todos los builders listados en 4.1.

#### 4.3 `DatabaseFixture` — Testcontainers Compartido

```csharp
namespace TallerPro.TestFixtures.Fixtures;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;
    public string ConnectionString { get; private set; } = "";

    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Test123!Password")
            .WithPortBinding(0, 1433) // random port
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString() + ";Database=TallerPro_Tests";

        // Run migrations once for the fixture
        using var scope = BuildServiceProvider().CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container != null) await _container.DisposeAsync();
    }

    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new ApplicationDbContext(options);
    }
}

// xUnit: Fixture compartida entre todos los tests de una clase collection
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }
```

#### 4.4 `WebAppFactory` — Para integration tests con HTTP real

```csharp
public sealed class WebAppFactory : WebApplicationFactory<Program>
{
    private readonly DatabaseFixture _dbFixture;
    
    public StripeMockServer Stripe { get; }
    public NovitaMockServer Novita { get; }
    public SwSapienMockServer SwSapien { get; }

    public WebAppFactory(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        Stripe = new StripeMockServer();
        Novita = new NovitaMockServer();
        SwSapien = new SwSapienMockServer();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            // Reemplazar DB por la de Testcontainers
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(opts =>
                opts.UseSqlServer(_dbFixture.ConnectionString));
            
            // Apuntar clientes HTTP a los mocks
            services.RemoveAll<IHttpClientFactory>();
            // ... configurar HttpClient para Stripe/Novita/SwSapien apuntando a mocks
            
            // TimeProvider controlable
            services.AddSingleton<TimeProvider, FakeTimeProvider>();
        });
    }
    
    public HttpClient CreateClientAuthenticatedAs(Tenant tenant, User user)
    {
        var client = CreateClient();
        var token = JwtTokenGenerator.Generate(tenant, user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
```

#### 4.5 `IntegrationTestBase`

```csharp
[Collection("Database")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected DatabaseFixture DbFixture { get; }
    protected WebAppFactory Factory { get; }
    protected ApplicationDbContext Db { get; private set; } = null!;

    protected IntegrationTestBase(DatabaseFixture dbFixture)
    {
        DbFixture = dbFixture;
        Factory = new WebAppFactory(dbFixture);
    }

    public async Task InitializeAsync()
    {
        Db = DbFixture.CreateDbContext();
        await CleanDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await Db.DisposeAsync();
    }

    private async Task CleanDatabaseAsync()
    {
        // Trunca todas las tablas excepto CatalogTypes y Plans (seed data)
        await Db.Database.ExecuteSqlRawAsync("""
            EXEC sp_MSforeachtable 'IF OBJECT_NAME(PARENT_OBJECT_ID) NOT IN (''CatalogTypes'', ''Plans'', ''PlanMeters'') ALTER TABLE ? NOCHECK CONSTRAINT ALL';
            EXEC sp_MSforeachtable 'IF OBJECT_NAME(PARENT_OBJECT_ID) NOT IN (''CatalogTypes'', ''Plans'', ''PlanMeters'') DELETE FROM ?';
            EXEC sp_MSforeachtable 'IF OBJECT_NAME(PARENT_OBJECT_ID) NOT IN (''CatalogTypes'', ''Plans'', ''PlanMeters'') ALTER TABLE ? CHECK CONSTRAINT ALL';
            """);
    }
}
```

#### 4.6 Mocks con WireMock

Código completo de `StripeMockServer`, `NovitaMockServer`, `SwSapienMockServer` usando `WireMock.Net` NuGet:

```csharp
public sealed class StripeMockServer : IAsyncDisposable
{
    private readonly WireMockServer _server;
    public string BaseUrl => _server.Urls[0];

    public StripeMockServer()
    {
        _server = WireMockServer.Start();
        SetupDefaultMappings();
    }

    private void SetupDefaultMappings()
    {
        _server
            .Given(Request.Create().WithPath("/v1/customers").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { id = "cus_test_123", object_ = "customer" }));
        
        _server
            .Given(Request.Create().WithPath("/v1/subscriptions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { id = "sub_test_123", status = "trialing" }));
        
        _server
            .Given(Request.Create().WithPath("/v1/billing/meter_events").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { id = "mev_test_123" }));
    }

    public void SimulateMeterEventFailure(int times = 1)
    {
        // ... configura un escenario de fallo controlado
    }

    public async ValueTask DisposeAsync()
    {
        _server.Stop();
        await Task.CompletedTask;
    }
}
```

### 5. Ejemplos Representativos por Tipo de Test

#### 5.1 Unit Test (`TallerPro.Domain.Tests`)

Código completo de 2-3 tests ejemplo:

```csharp
public class ServiceOrderTests
{
    [Fact]
    public void Close_WhenInProgress_SetsStatusToCompletedAndFinalCost()
    {
        // Arrange
        var order = new ServiceOrderBuilder()
            .InStatus(ServiceOrderStatus.InProgress)
            .WithItems(
                new OrderItem("Aceite", 1, 500m),
                new OrderItem("Mano de obra", 2, 350m))
            .Build();

        // Act
        order.Close();

        // Assert
        order.Status.ShouldBe(ServiceOrderStatus.Completed);
        order.FinalCost.ShouldBe(1200m);
        order.ClosedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Close_WhenAlreadyCompleted_Throws()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.Completed).Build();

        var ex = Should.Throw<DomainException>(() => order.Close());
        ex.Code.ShouldBe("ORDER_ALREADY_CLOSED");
    }
}
```

#### 5.2 Handler Test (`TallerPro.Application.Tests`)

```csharp
public class CreateServiceOrderHandlerTests
{
    private readonly ITenantContext _tenantCtx = Substitute.For<ITenantContext>();
    private readonly IApplicationDbContext _db = Substitute.For<IApplicationDbContext>();
    private readonly IUsageTracker _usage = Substitute.For<IUsageTracker>();
    private readonly CreateServiceOrderHandler _sut;

    public CreateServiceOrderHandlerTests()
    {
        _tenantCtx.CurrentTenantId.Returns(Guid.NewGuid());
        _tenantCtx.CurrentBranchId.Returns(Guid.NewGuid());
        _sut = new CreateServiceOrderHandler(_db, _tenantCtx, _usage);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesOrderAndIncrementsCounter()
    {
        var cmd = new CreateServiceOrderCommand(
            CustomerId: Guid.NewGuid(),
            VehicleId: Guid.NewGuid(),
            Description: "Servicio mayor 50k km");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

#### 5.3 Integration Test (`TallerPro.Integration.Tests`)

```csharp
public class ServiceOrdersIntegrationTests(DatabaseFixture db) : IntegrationTestBase(db)
{
    [Fact]
    public async Task CreateOrder_PersistsAndReturnsWithCorrectTenant()
    {
        // Arrange
        var tenant = await new TenantBuilder().WithBranches(1).PersistAsync(Db);
        var user = await new UserBuilder().ForTenant(tenant.Id).PersistAsync(Db);
        var customer = await new CustomerBuilder().ForTenant(tenant.Id).PersistAsync(Db);
        var vehicle = await new VehicleBuilder().ForCustomer(customer.Id).PersistAsync(Db);

        var client = Factory.CreateClientAuthenticatedAs(tenant, user);

        // Act
        var response = await client.PostAsJsonAsync("/api/orders", new
        {
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            Description = "Cambio de balatas"
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<ServiceOrderDto>();
        order.ShouldNotBeNull();
        
        var stored = await Db.ServiceOrders.FindAsync(order.Id);
        stored.ShouldNotBeNull();
        stored.TenantId.ShouldBe(tenant.Id);
        stored.BranchId.ShouldBe(tenant.Branches[0].Id);
    }
}
```

#### 5.4 E2E Test (`TallerPro.E2E.Tests`) — Playwright

```csharp
public class SignupE2ETests : IAsyncLifetime
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    [Fact]
    public async Task CompleteSignupFlow_CreatesTenantAndLogsIn()
    {
        var page = await _browser.NewPageAsync();
        await page.GotoAsync("https://staging.tallerpro.mx");

        await page.ClickAsync("text=Empezar prueba gratis");
        await page.FillAsync("input[name='Email']", $"test+{Guid.NewGuid():N}@tallerpro.mx");
        await page.FillAsync("input[name='TenantName']", "Taller Playwright Test");
        await page.FillAsync("input[name='RFC']", "XAXX010101000");
        await page.ClickAsync("button[type='submit']");

        // Redirige a Stripe Checkout (mock en staging)
        await page.WaitForURLAsync("**checkout.stripe.com/**");
        // ... interacciones con Stripe test mode
        
        // Vuelve a success page
        await page.WaitForURLAsync("**signup/success**");
        var heading = await page.TextContentAsync("h1");
        heading.ShouldContain("Bienvenido");
    }

    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }
}
```

#### 5.5 Load Test (`TallerPro.LoadTests`) — NBomber

```csharp
public class UsageTrackerLoadTest
{
    [Fact]
    public async Task UsageTracker_ConcurrentCfdiStamps_HandlesLoad()
    {
        using var factory = new WebAppFactory(DbFixture.Instance);
        var tenant = await CreateTestTenantAsync(factory);
        var client = factory.CreateClientAuthenticatedAs(tenant);

        var scenario = Scenario.Create("cfdi_stamp_spike", async ctx =>
        {
            var response = await client.PostAsJsonAsync("/api/cfdi/timbrar",
                new TimbrarRequest { /* ... */ });
            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.RampingInject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2)),
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(3))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("load-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Md)
            .Run();

        stats.AllOkCount.ShouldBeGreaterThan(8000);
        stats.AllFailCount.ShouldBeLessThan(100);
        stats.ScenarioStats[0].Ok.Request.RPS.ShouldBeGreaterThan(40);
        stats.ScenarioStats[0].Ok.Latency.Percent95.ShouldBeLessThan(500);
    }
}
```

**Scenarios objetivo**:
1. UsageTracker concurrent spike (como arriba).
2. 200 usuarios simultáneos creando órdenes (smoke capacity).
3. Búsqueda de clientes con 500 concurrent (caches validation).
4. Sync offline reconnect spike (50 clientes Hybrid reconectan simultáneo).
5. SignalR connections sostenidas (validar backplane Redis).

#### 5.6 Chaos Test — Simmy + Polly

```csharp
public class StripeMeterReporterChaosTests
{
    [Fact]
    public async Task MeterReporter_SurvivesRandomStripeOutages()
    {
        // Setup: inyectar policy Simmy que 30% de llamadas a Stripe fallen aleatoriamente
        var chaosPolicy = MonkeyPolicy.InjectExceptionAsync(with =>
            with.Fault(new HttpRequestException("Simulated Stripe outage"))
                .InjectionRate(0.3)
                .Enabled());

        // ... setup ejecuta reporter con policy chaos durante 2 min
        // Assert: todos los eventos eventualmente reportados (retry + backoff)
        // Assert: cero eventos duplicados (idempotency)
    }
}
```

#### 5.7 Contract Test — Pact.NET

```csharp
public class StripeConsumerContractTests
{
    private readonly IPactBuilderV3 _pact;

    public StripeConsumerContractTests()
    {
        _pact = Pact.V3("TallerProApi", "StripeApi").WithHttpInteractions();
    }

    [Fact]
    public async Task CreateMeterEvent_Contract()
    {
        _pact.UponReceiving("a request to create a meter event")
            .Given("valid API key and customer")
            .WithRequest(HttpMethod.Post, "/v1/billing/meter_events")
            .WithHeader("Authorization", Match.Regex("Bearer .+", "Bearer sk_test_xxx"))
            .WithBody(new { event_name = "cfdi_stamps", payload = new { stripe_customer_id = "cus_xxx", value = 1 } })
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithJsonBody(new { id = Match.Regex("mev_\\w+", "mev_test_123") });

        await _pact.VerifyAsync(async ctx =>
        {
            var client = new StripeClient(ctx.MockServerUri.ToString(), "sk_test_xxx");
            var result = await client.CreateMeterEventAsync("cfdi_stamps", "cus_xxx", 1);
            result.Id.ShouldStartWith("mev_");
        });
    }
}
```

Esto genera un archivo `pact.json` que se publica a Pact Broker (o se sube al repo) — cuando Stripe cambie su API, el test falla y sabemos que hay que actualizar.

#### 5.8 Mutation Test (Stryker.NET)

`stryker-config.json`:
```json
{
  "stryker-config": {
    "project": "src/TallerPro.Application/TallerPro.Application.csproj",
    "test-projects": ["tests/TallerPro.Application.Tests/TallerPro.Application.Tests.csproj"],
    "mutation-level": "Standard",
    "coverage-analysis": "perTest",
    "thresholds": { "high": 85, "low": 75, "break": 70 },
    "reporters": ["html", "cleartext", "progress"],
    "mutate": [
      "!**/Migrations/**",
      "!**/*.g.cs"
    ]
  }
}
```

Ejecutar: `dotnet stryker`.

Aplicado inicialmente a:
- `TallerPro.Security/` (impersonation, ITenantContext).
- `TallerPro.Application/UseCases/Billing/` (UsageTracker, SubscriptionService).
- `TallerPro.Application/UseCases/CFDI/`.

Target mutation score: **≥ 75%** en código crítico.

### 6. Tests Críticos Específicos

Para cada uno, código completo:

#### 6.1 PII Masking Completo End-to-End

Verifica que **ninguna petición a Novita contiene PII cruda** incluso en escenarios edge:
- Texto con múltiples placas/RFCs mezclados.
- JSON embebido con nombre y teléfono.
- Respuesta stream (chunks con PII).
- Logs Serilog capturados que verifican que enricher enmascaró.

#### 6.2 Impersonation Governance

- Founder puede impersonate Full → test.
- Support role intenta Full → retorna 403.
- Impersonation expirada rechaza write → test.
- Tenant termina sesión activa → cierra sesión → próxima request retorna 401.
- Email al tenant enviado en cada inicio → mock Brevo verifica.
- Audit log inmutable recibe entrada.

#### 6.3 Stripe Meter Reconciliation

- 1000 eventos generados local → reporter procesa → 1000 en Stripe mock.
- Drift artificial del 2% → alerta dispara.
- Idempotency: mismo evento reportado 2 veces → Stripe recibe solo 1.
- Reporter cae a mitad → reinicia → retoma sin duplicar.

#### 6.4 Sync Offline Conflictos

- LWW: dos clientes editan cliente simultáneo → wins el de mayor RowVersion.
- Stock: dos sucursales consumen stock simultáneo → ambos movimientos se aplican, stock final = suma.
- CFDI: conflicto pendiente → NO auto-resuelve → UI muestra a admin.

#### 6.5 CFDI Timbrado con SW Sapien

- XML válido → timbre exitoso → persiste UUID + PDF.
- Error XSD → reintenta 3 veces → falla → escala + retorna error clasificado.
- Timeout SW Sapien → circuit breaker abre → próximas requests fallan rápido.
- Cancelación CFDI → flujo por motivo 01/02/03/04.

### 7. Quality Gates y Thresholds

Tabla con thresholds por proyecto:

| Proyecto | Cobertura mínima | Mutation score mín | Tests obligatorios para merge |
|---|---|---|---|
| `TallerPro.Security/` | 90% | 80% | Todos passing + mutation clean |
| `TallerPro.Application/UseCases/Billing/` | 90% | 80% | Todos passing + contract tests green |
| `TallerPro.Application/UseCases/Cfdi/` | 90% | 75% | Todos passing |
| `TallerPro.Application/` (resto) | 80% | 70% | Todos passing |
| `TallerPro.Domain/` | 85% | 75% | Todos passing |
| `TallerPro.Infrastructure/` | 70% | - (excluido) | Integration tests passing |
| `TallerPro.Components/` (Razor) | - | - | Smoke E2E tests passing |

**Quality gate en PR**:
- Unit + integration + isolation tests **verdes**.
- Cobertura total del diff **≥ 70%** (Codecov).
- No nuevos `#pragma warning disable TP0001-TP0005` sin label `security-review`.
- No nuevos `[Skip]` en tests sin label `tech-debt`.
- Al menos 1 test nuevo si el PR agrega o modifica handlers/endpoints.

### 8. CI Integration — GitHub Actions

Pipeline completo `.github/workflows/tests.yml`:

```yaml
name: Tests

on:
  pull_request:
  push:
    branches: [main, develop]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 9.0.x
      - name: Restore
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore --configuration Release
      - name: Unit Tests
        run: dotnet test tests/TallerPro.Domain.Tests tests/TallerPro.Application.Tests --no-build --configuration Release --collect:"XPlat Code Coverage" --logger trx
      - name: Upload Coverage
        uses: codecov/codecov-action@v4
        with:
          fail_ci_if_error: true
          flags: unit

  integration-tests:
    runs-on: ubuntu-latest
    needs: unit-tests
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
      - name: Setup Docker
        uses: docker/setup-buildx-action@v3
      - name: Integration Tests (Testcontainers)
        run: dotnet test tests/TallerPro.Integration.Tests --collect:"XPlat Code Coverage" --logger trx
        env:
          TESTCONTAINERS_RYUK_DISABLED: "true"  # para runners de GH Actions

  isolation-tests:
    runs-on: ubuntu-latest
    needs: unit-tests
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
      - name: Isolation Tests (critical)
        run: dotnet test tests/TallerPro.Isolation.Tests --logger trx
      - name: Assert All Endpoints Covered
        run: dotnet run --project tests/TallerPro.Isolation.Tests -- --assert-coverage

  analyzer-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
      - name: Roslyn Analyzer Tests
        run: dotnet test tests/TallerPro.Analyzers.Tests

  e2e-smoke:
    runs-on: ubuntu-latest
    needs: [integration-tests, isolation-tests]
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
      - name: Install Playwright
        run: |
          dotnet build tests/TallerPro.E2E.Tests
          pwsh tests/TallerPro.E2E.Tests/bin/Debug/net9.0/playwright.ps1 install --with-deps chromium
      - name: E2E Smoke Suite
        run: dotnet test tests/TallerPro.E2E.Tests --filter "Category=Smoke"
        env:
          E2E_BASE_URL: ${{ vars.E2E_STAGING_URL }}

  report:
    runs-on: ubuntu-latest
    needs: [unit-tests, integration-tests, isolation-tests, analyzer-tests, e2e-smoke]
    if: always()
    steps:
      - uses: actions/checkout@v4
      - name: Generate Coverage Report
        run: |
          dotnet tool install -g dotnet-reportgenerator-globaltool
          reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:"Html;MarkdownSummary"
      - name: Upload Coverage Report
        uses: actions/upload-artifact@v4
        with:
          name: coverage-report
          path: coveragereport
```

**Workflows adicionales**:

`.github/workflows/tests-nightly.yml` (corre 3 AM UTC):
- Full E2E suite (no solo smoke).
- Load tests con NBomber contra staging.
- Mutation tests con Stryker.NET en código crítico.
- Reports publicados a Slack `#qa`.

`.github/workflows/contract-tests.yml` (corre en push a main):
- Contract tests con Pact.NET.
- Publica pact files a Pact Broker (o commit al repo).
- Verifica contratos contra providers reales (Stripe staging, Novita dev).

### 9. Coverage Reporting

Setup de Coverlet + ReportGenerator:

**`Directory.Build.props`** addition:
```xml
<ItemGroup Condition="'$(IsTestProject)' == 'true'">
  <PackageReference Include="coverlet.collector" Version="6.0.2" />
  <PackageReference Include="coverlet.msbuild" Version="6.0.2">
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
</ItemGroup>
```

**`coverlet.runsettings`**:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat code coverage">
        <Configuration>
          <Format>cobertura</Format>
          <Exclude>[*.Tests]*,[*.TestFixtures]*,[*.Migrations]*,[*]*.Migrations.*</Exclude>
          <ExcludeByAttribute>GeneratedCodeAttribute,CompilerGeneratedAttribute</ExcludeByAttribute>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

Badges en `README.md`:
- Coverage badge (Codecov).
- Build status.
- Mutation score (Stryker dashboard).

### 10. Testing Runbook

Documento completo `docs/testing-guide.md`:

**Secciones**:
1. **Cómo correr tests localmente**.
2. **Cómo agregar un test nuevo** (con decisión de qué tipo según la funcionalidad).
3. **Cómo debuggear un test flaky**.
4. **Cómo actualizar snapshots** (Verify).
5. **Cómo regenerar pact files**.
6. **Cómo correr mutation tests**.
7. **Cómo interpretar reports de load tests**.
8. **Quality gate failures comunes y cómo resolver**.
9. **Cómo agregar un nuevo mock a WireMock**.
10. **Convenciones de naming de tests** (`ClassUnderTest_Scenario_ExpectedOutcome`).

### 11. Anti-Patrones a Evitar

Sección de "DON'Ts" con ejemplos concretos:

- ❌ Usar `Thread.Sleep` — usar `TimeProvider` controlable.
- ❌ Tests con dependencias de orden — cada test debe ser independiente.
- ❌ `[Skip]` permanente sin issue trackeado.
- ❌ Mockear EF Core `DbContext` — usar Testcontainers.
- ❌ Assertion explosion (50 asserts en un test) — un test = una aseveración lógica.
- ❌ "God fixtures" que setean toda la DB para cada test — usar builders focalizados.
- ❌ Tests que dependen de internet real en CI — siempre mocks.
- ❌ Probar implementación en lugar de comportamiento (tests que asumen cómo hace algo en lugar de qué hace).

---

## Restricciones de la Respuesta

- **Código C# ejecutable**. Tests deben compilar y correr.
- Usa .NET 9 idioms: primary constructors, file-scoped namespaces.
- Convenciones del proyecto: xUnit + Shouldly + NSubstitute + Testcontainers + Playwright.
- NO FluentAssertions (comercial desde 2025), NO Moq (telemetría controversial), NO NUnit.
- Prioriza código + YAML sobre prosa.
- Longitud target: ~13,000-15,000 tokens.

---

## Al final de tu respuesta

Genera **"ADR Update — Testing Strategy"** con decisiones cementadas (stack final, thresholds, quality gates).
