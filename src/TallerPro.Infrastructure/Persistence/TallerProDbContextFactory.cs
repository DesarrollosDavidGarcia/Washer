using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TallerPro.Security;

namespace TallerPro.Infrastructure.Persistence;

/// <summary>
/// Provides a TallerProDbContext instance for design-time tools (dotnet ef migrations).
/// Uses a stub ITenantContext that returns 0 — EF does not execute queries during model construction,
/// so global filter lambdas referencing CurrentTenantId will not throw.
/// Connection string: TALLERPRO_DB_CONNECTION env var or LocalDB fallback.
/// </summary>
public sealed class TallerProDbContextFactory : IDesignTimeDbContextFactory<TallerProDbContext>
{
    public TallerProDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("TALLERPRO_DB_CONNECTION")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=TallerPro_Design;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<TallerProDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new TallerProDbContext(options, new DesignTimeTenantContext(), new DesignTimeCurrentUser());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        // Returns 0 in design-time; EF tooling builds the model but never executes the global filter
        // queries, so this will not trigger MissingTenantContextException.
        public long CurrentTenantId => 0;

        public bool IsResolved => false;

        public void TrySetTenant(long id) { }

        public void Clear() { }
    }

    private sealed class DesignTimeCurrentUser : ICurrentUser
    {
        public long? CurrentUserId => null;

        public long? CurrentPlatformAdminId => null;
    }
}
