using Microsoft.EntityFrameworkCore;
using Shouldly;
using TallerPro.Domain.Auth;
using TallerPro.Domain.Common;
using TallerPro.Domain.Tenants;
using TallerPro.Integration.Tests.Fixtures;
using Xunit;

namespace TallerPro.Integration.Tests.Persistence;

/// <summary>
/// T-26 / CA-05: Verifies soft delete behavior.
/// Deleted entities are hidden in normal queries but visible when using IgnoreQueryFilters().
/// </summary>
[Collection("SqlServerCollection")]
[Trait("Docker", "true")]
public sealed class SoftDeleteTests
{
    private readonly SqlServerFixture _fixture;

    public SoftDeleteTests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Remove_Branch_HiddenInNormalQuery_VisibleWithIgnoreQueryFilters()
    {
        await _fixture.ResetAsync();

        var tenantContext = new TestTenantContext();
        var currentUser = new TestCurrentUser();
        await using var ctx = _fixture.CreateContext(tenantContext, currentUser);

        // Arrange: create tenant
        var tenant = new Tenant(name: "Test Tenant", slug: "test-soft-delete");
        ctx.Tenants.Add(tenant);
        await ctx.SaveChangesAsync();

        tenantContext.TrySetTenant(tenant.Id);

        // Arrange: create branch
        var branch = new Branch(tenantId: tenant.Id, name: "Test Branch", code: "TSFT");
        ctx.Branches.Add(branch);
        await ctx.SaveChangesAsync();

        var branchId = branch.Id;

        // Act: remove the branch (soft delete)
        var toDelete = await ctx.Branches.FirstAsync(b => b.Id == branchId);
        ctx.Branches.Remove(toDelete);
        await ctx.SaveChangesAsync();

        // Assert: branch hidden in normal query
        var normalQuery = await ctx.Branches
            .FirstOrDefaultAsync(b => b.Id == branchId);
        normalQuery.ShouldBeNull();

        // Assert: branch visible with IgnoreQueryFilters (requires [AllowIgnoreQueryFilters] on caller)
        var softDeletedBranch = await GetSoftDeletedBranch(ctx, branchId);
        softDeletedBranch.ShouldNotBeNull();
        softDeletedBranch.IsDeleted.ShouldBeTrue();
        softDeletedBranch.DeletedAt.ShouldNotBeNull();
    }

    /// <summary>
    /// Test method using IgnoreQueryFilters() with required [AllowIgnoreQueryFilters] attribute.
    /// This demonstrates analyzer compliance (TP0001).
    /// </summary>
    [AllowIgnoreQueryFilters("Test: retrieve soft-deleted branches")]
    private static async Task<Branch?> GetSoftDeletedBranch(DbContext ctx, long branchId)
    {
        return await ctx.Set<Branch>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == branchId);
    }
}
