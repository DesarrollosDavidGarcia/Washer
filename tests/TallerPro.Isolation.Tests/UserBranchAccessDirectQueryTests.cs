using Shouldly;
using TallerPro.Domain.Auth;
using TallerPro.Domain.Tenants;
using TallerPro.Isolation.Tests.Fixtures;
using Xunit;

namespace TallerPro.Isolation.Tests;

/// <summary>
/// T-27 / CA-04 b: Verifies that direct queries to UserBranchAccesses respect tenant scoping via denormalized TenantId.
/// Even when querying the DbSet directly with no join conditions, the global filter protects against cross-tenant access.
/// </summary>
[Collection("SqlServerCollection")]
[Trait("Docker", "true")]
public sealed class UserBranchAccessDirectQueryTests
{
    private readonly SqlServerFixture _fixture;

    public UserBranchAccessDirectQueryTests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not have incorrect suffix")]
    public async Task DirectQuery_UserBranchAccessesWhereTrueWithTenantA_ReturnsOnlyAccessesInTenantA()
    {
        await _fixture.ResetAsync();

        var tenantContext = new TestTenantContext();
        var currentUser = new TestCurrentUser();
        await using var ctx = _fixture.CreateContext(tenantContext, currentUser);

        // Arrange: create two tenants with branches
        var tenantA = new Tenant(name: "Tenant A Direct", slug: "tenant-a-direct-iso");
        var tenantB = new Tenant(name: "Tenant B Direct", slug: "tenant-b-direct-iso");
        ctx.Tenants.Add(tenantA);
        ctx.Tenants.Add(tenantB);
        await ctx.SaveChangesAsync();

        tenantContext.TrySetTenant(tenantA.Id);
        var branchA = new Branch(tenantId: tenantA.Id, name: "Branch A Direct", code: "ADA");
        ctx.Branches.Add(branchA);
        await ctx.SaveChangesAsync();

        tenantContext.TrySetTenant(tenantB.Id);
        var branchB = new Branch(tenantId: tenantB.Id, name: "Branch B Direct", code: "BDB");
        ctx.Branches.Add(branchB);
        await ctx.SaveChangesAsync();

        // Arrange: create user with access to both tenants
        var user = new User(email: "direct@test.test", displayName: "Direct Query User");
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        tenantContext.TrySetTenant(tenantA.Id);
        var accessA = new UserBranchAccess(
            userId: user.Id,
            branchId: branchA.Id,
            tenantId: tenantA.Id,
            role: Role.Admin);
        ctx.UserBranchAccesses.Add(accessA);
        await ctx.SaveChangesAsync();

        tenantContext.TrySetTenant(tenantB.Id);
        var accessB = new UserBranchAccess(
            userId: user.Id,
            branchId: branchB.Id,
            tenantId: tenantB.Id,
            role: Role.Admin);
        ctx.UserBranchAccesses.Add(accessB);
        await ctx.SaveChangesAsync();

        // Act: direct query with .Where(x => true) on tenant A
        // This tests that the global filter applies even in broad direct queries (D-03).
        tenantContext.TrySetTenant(tenantA.Id);
        var directQueryA = ctx.UserBranchAccesses
            .Where(x => true)  // Broad query to force reliance on global filter
            .ToList();

        // Assert: only tenant A's access returned
        directQueryA.Count.ShouldBe(1);
        directQueryA[0].UserId.ShouldBe(user.Id);
        directQueryA[0].TenantId.ShouldBe(tenantA.Id);
        directQueryA.Any(a => a.TenantId == tenantB.Id).ShouldBeFalse();

        // Act: switch to tenant B
        tenantContext.TrySetTenant(tenantB.Id);
        var directQueryB = ctx.UserBranchAccesses
            .Where(x => true)
            .ToList();

        // Assert: only tenant B's access returned
        directQueryB.Count.ShouldBe(1);
        directQueryB[0].UserId.ShouldBe(user.Id);
        directQueryB[0].TenantId.ShouldBe(tenantB.Id);
        directQueryB.Any(a => a.TenantId == tenantA.Id).ShouldBeFalse();
    }
}
