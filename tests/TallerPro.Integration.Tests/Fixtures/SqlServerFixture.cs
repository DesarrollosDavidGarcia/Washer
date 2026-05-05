using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Respawn;
using TallerPro.Infrastructure.Persistence;
using TallerPro.Infrastructure.Persistence.Interceptors;
using TallerPro.Security;
using Testcontainers.MsSql;
using Xunit;

namespace TallerPro.Integration.Tests.Fixtures;

/// <summary>
/// T-24: Testcontainers SQL Server fixture providing database lifecycle management and test isolation via Respawn.
/// Implements IAsyncLifetime for xUnit fixture management.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;
    private string? _connectionString;
    private Respawner? _respawner;

    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();

        // Apply migrations on clean database
        var context = CreateContext(new TestTenantContext(), new TestCurrentUser());
        await context.Database.MigrateAsync();
        await context.DisposeAsync();

        // Initialize Respawn for test data reset
        await using var connection = new SqlConnection(_connectionString);
        _respawner = await Respawner.CreateAsync(connection);
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a new TallerProDbContext pointing to the Testcontainers database.
    /// Interceptors are instantiated directly here because there is no DI container in fixtures.
    /// </summary>
    public TallerProDbContext CreateContext(ITenantContext tenantContext, ICurrentUser currentUser)
    {
        if (_connectionString is null)
        {
            throw new InvalidOperationException("Fixture not initialized");
        }

        var options = new DbContextOptionsBuilder<TallerProDbContext>()
            .UseSqlServer(_connectionString)
            .AddInterceptors(
                new SoftDeleteInterceptor(),
                new AuditingInterceptor(currentUser),
                new FundationalTenantGuard(),
                new TenantStampInterceptor())
            .Options;

        return new TallerProDbContext(options, tenantContext, currentUser);
    }

    /// <summary>
    /// Resets all data in the database using Respawn, preserving __EFMigrationsHistory
    /// and the foundational tenant seed (preserved by Respawn through data-level cleanup).
    /// Call at the start of each test that modifies data.
    /// </summary>
    public async Task ResetAsync()
    {
        if (_respawner is null)
        {
            throw new InvalidOperationException("Fixture not initialized");
        }

        await using var connection = new SqlConnection(_connectionString!);
        await _respawner.ResetAsync(connection);
    }
}
