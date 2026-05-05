using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TallerPro.Infrastructure.Persistence;
using TallerPro.Security;
using Testcontainers.MsSql;
using Xunit;

namespace TallerPro.Integration.Tests.Persistence;

/// <summary>
/// T-23b: Verifies that the initial migration is idempotent — applying the EF-generated
/// idempotent script against an already-migrated database produces no changes.
/// </summary>
[Trait("Docker", "true")]
public sealed class MigrationIdempotencyTests : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public async Task InitializeAsync() => await _container.StartAsync();

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task IdempotentScript_AppliedTwice_ProducesIdenticalSchema()
    {
        var connectionString = _container.GetConnectionString();
        await using var context = BuildContext(connectionString);

        // Step 1: apply migration from clean state.
        await context.Database.MigrateAsync();

        // Step 2: capture schema snapshot after first apply.
        var snapshotBefore = await CaptureSnapshotAsync(context);

        // Step 3: generate idempotent script and re-execute it.
        // The idempotent script wraps each batch inside:
        //   IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '...')
        // so all DDL is skipped on second run. The seed INSERT is also inside that guard,
        // so it is not repeated.
        var migrator = context.GetService<IMigrator>();
        var idempotentScript = migrator.GenerateScript(
            fromMigration: null,
            toMigration: null,
            options: MigrationsSqlGenerationOptions.Idempotent);

        // SQL Server does not support multiple batches in a single ExecuteSqlRaw call;
        // split on GO statements (EF emits "GO\r\n" or "GO\n").
        var batches = idempotentScript.Split(
            ["\nGO\r\n", "\nGO\n", "GO\r\n", "GO\n"],
            StringSplitOptions.RemoveEmptyEntries);

        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await context.Database.ExecuteSqlRawAsync(batch);
        }

        // Step 4: capture schema snapshot after second apply.
        var snapshotAfter = await CaptureSnapshotAsync(context);

        // Assertions: snapshots must be identical.
        snapshotAfter.Schemas.ShouldBe(snapshotBefore.Schemas);
        snapshotAfter.Tables.ShouldBe(snapshotBefore.Tables);
        snapshotAfter.FoundationalTenantCount.ShouldBe(snapshotBefore.FoundationalTenantCount);
        snapshotAfter.FoundationalTenantCount.ShouldBe(1);
    }

    private static TallerProDbContext BuildContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TallerProDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new TallerProDbContext(options, new TestTenantContext(), new TestCurrentUser());
    }

    private static async Task<DbSnapshot> CaptureSnapshotAsync(TallerProDbContext context)
    {
        var schemas = await context.Database
            .SqlQueryRaw<string>(
                "SELECT name FROM sys.schemas WHERE name IN ('core', 'auth') ORDER BY name")
            .ToListAsync();

        var tables = await context.Database
            .SqlQueryRaw<string>(
                "SELECT TABLE_SCHEMA + '.' + TABLE_NAME FROM INFORMATION_SCHEMA.TABLES ORDER BY TABLE_SCHEMA, TABLE_NAME")
            .ToListAsync();

        var foundationalTenantCount = await context.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(*) AS Value FROM [core].[Tenants] WHERE Slug = 'tallerpro-platform'")
            .FirstAsync();

        return new DbSnapshot(schemas, tables, foundationalTenantCount);
    }

    private sealed record DbSnapshot(
        List<string> Schemas,
        List<string> Tables,
        int FoundationalTenantCount);

    private sealed class TestTenantContext : ITenantContext
    {
        // Design-time stub: returns 0; global filter lambdas are not executed during migration apply.
        public long CurrentTenantId => 0;
        public bool IsResolved => false;
        public void TrySetTenant(long id) { }
        public void Clear() { }
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public long? CurrentUserId => null;
        public long? CurrentPlatformAdminId => null;
    }
}
