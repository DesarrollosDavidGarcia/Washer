using Microsoft.EntityFrameworkCore;
using Shouldly;
using TallerPro.Domain.Tenants;
using TallerPro.Integration.Tests.Fixtures;
using Xunit;

namespace TallerPro.Integration.Tests.Persistence;

/// <summary>
/// T-26 / CA-09: Verifies concurrency conflict detection via RowVersion.
/// When two contexts modify the same entity independently, the second update raises DbUpdateConcurrencyException.
/// </summary>
[Collection("SqlServerCollection")]
[Trait("Docker", "true")]
public sealed class ConcurrencyTests
{
    private readonly SqlServerFixture _fixture;

    public ConcurrencyTests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Concurrency_SecondContextModification_ThrowsDbUpdateConcurrencyException()
    {
        await _fixture.ResetAsync();

        var tenantContext = new TestTenantContext();
        var currentUser = new TestCurrentUser();

        // Arrange: create tenant
        await using (var ctx1 = _fixture.CreateContext(tenantContext, currentUser))
        {
            var tenant = new Tenant(name: "Concurrency Test", slug: "concurrency-test");
            ctx1.Tenants.Add(tenant);
            await ctx1.SaveChangesAsync();
        }

        // Arrange: load same tenant in two separate contexts
        long tenantId;
        await using (var ctx1 = _fixture.CreateContext(tenantContext, currentUser))
        {
            var tenant1 = await ctx1.Tenants.FirstAsync(t => t.Slug == "concurrency-test");
            tenantId = tenant1.Id;
        }

        // Arrange: load in second context (fresh RowVersion)
        await using var ctx2 = _fixture.CreateContext(tenantContext, currentUser);
        var tenant2 = await ctx2.Tenants.FirstAsync(t => t.Id == tenantId);
        var rowVersion2 = tenant2.RowVersion;

        // Act: modify and save in context 1 (this increments RowVersion in DB)
        await using (var ctx1 = _fixture.CreateContext(tenantContext, currentUser))
        {
            var tenant1 = await ctx1.Tenants.FirstAsync(t => t.Id == tenantId);
            // Force a dummy update to increment RowVersion
            ctx1.Tenants.Update(tenant1);
            await ctx1.SaveChangesAsync();
        }

        // Act: attempt to modify in context 2 with now-stale RowVersion
        // Mark entity as modified to force update attempt
        ctx2.Tenants.Update(tenant2);

        // Assert: SaveChangesAsync throws DbUpdateConcurrencyException due to stale RowVersion
        var ex = await Should.ThrowAsync<DbUpdateConcurrencyException>(
            () => ctx2.SaveChangesAsync());
        ex.ShouldNotBeNull();
    }
}
