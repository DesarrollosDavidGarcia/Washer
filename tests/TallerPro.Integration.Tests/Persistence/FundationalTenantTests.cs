using Microsoft.EntityFrameworkCore;
using Shouldly;
using TallerPro.Domain.Tenants;
using TallerPro.Integration.Tests.Fixtures;
using Xunit;

namespace TallerPro.Integration.Tests.Persistence;

/// <summary>
/// T-26 / CA-10: Verifies protection of the foundational tenant.
/// The 'tallerpro-platform' tenant is seeded and protected against soft delete and status changes.
/// </summary>
[Collection("SqlServerCollection")]
[Trait("Docker", "true")]
public sealed class FundationalTenantTests
{
    private readonly SqlServerFixture _fixture;

    public FundationalTenantTests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task FoundationalTenant_Exists_IdentifiedBySlug()
    {
        // Reset does not delete the foundational tenant (seeded in migration)
        await _fixture.ResetAsync();

        var tenantContext = new TestTenantContext();
        var currentUser = new TestCurrentUser();
        await using var ctx = _fixture.CreateContext(tenantContext, currentUser);

        // Act: query for foundational tenant
        var foundationalTenant = await ctx.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == TenantConstants.FundationalSlug);

        // Assert: foundational tenant exists with correct properties
        foundationalTenant.ShouldNotBeNull();
        foundationalTenant.Slug.ShouldBe("tallerpro-platform");
        foundationalTenant.Status.ShouldBe(TenantStatus.Active);
        foundationalTenant.Id.ShouldBeGreaterThan(0);
        foundationalTenant.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task RemoveFoundationalTenant_ThrowsInvalidOperationException()
    {
        await _fixture.ResetAsync();

        var tenantContext = new TestTenantContext();
        var currentUser = new TestCurrentUser();
        await using var ctx = _fixture.CreateContext(tenantContext, currentUser);

        // Arrange: load foundational tenant
        var foundationalTenant = await ctx.Tenants
            .FirstAsync(t => t.Slug == TenantConstants.FundationalSlug);

        // Act & Assert: attempt to soft delete throws InvalidOperationException
        ctx.Tenants.Remove(foundationalTenant);
        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => ctx.SaveChangesAsync());
        ex.Message.ShouldContain("foundational");
    }

    [Theory]
    [InlineData(TenantStatus.Suspended)]
    [InlineData(TenantStatus.Cancelled)]
    public async Task ChangeFoundationalTenantStatus_ThrowsInvalidOperationException(TenantStatus newStatus)
    {
        await _fixture.ResetAsync();

        var tenantContext = new TestTenantContext();
        var currentUser = new TestCurrentUser();
        await using var ctx = _fixture.CreateContext(tenantContext, currentUser);

        // Arrange: load foundational tenant
        var foundationalTenant = await ctx.Tenants
            .FirstAsync(t => t.Slug == TenantConstants.FundationalSlug);

        // Act: attempt to change status using EF Core's entry mechanism
        // (Status is read-only in code, but EF Core tracks and allows property state changes)
        var entry = ctx.Entry(foundationalTenant);
        entry.Property("Status").CurrentValue = newStatus;
        entry.State = EntityState.Modified;

        // Assert: SaveChangesAsync throws due to FundationalTenantGuard interceptor
        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => ctx.SaveChangesAsync());
        ex.Message.ShouldContain("foundational");
    }
}
