using Shouldly;
using TallerPro.Domain.Auth;
using TallerPro.Domain.Common;
using TallerPro.Domain.Tenants;
using TallerPro.Isolation.Tests.Fixtures;
using Xunit;

namespace TallerPro.Isolation.Tests;

/// <summary>
/// T-27 / CA-04: Critical isolation test.
/// Verifies that queries are scoped to the current tenant and cannot leak data across tenants.
/// CRITICAL: Any failure here indicates a cross-tenant leak vulnerability.
/// </summary>
[Collection("SqlServerCollection")]
[Trait("Docker", "true")]
public sealed class CrossTenantLeakTests
{
    private readonly SqlServerFixture _fixture;

    public CrossTenantLeakTests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not have incorrect suffix")]
    public async Task QueryBranches_WithTenantA_ReturnsOnlyBranchesOfTenantA()
    {
        await _fixture.ResetAsync();

        var tenantContext = new TestTenantContext();
        var currentUser = new TestCurrentUser();
        await using var ctx = _fixture.CreateContext(tenantContext, currentUser);

        // Arrange: create two tenants
        var tenantA = new Tenant(name: "Tenant A", slug: "tenant-a-isolation");
        var tenantB = new Tenant(name: "Tenant B", slug: "tenant-b-isolation");
        ctx.Tenants.Add(tenantA);
        ctx.Tenants.Add(tenantB);
        await ctx.SaveChangesAsync();

        // Arrange: create branches for tenant A
        tenantContext.TrySetTenant(tenantA.Id);
        var branchA1 = new Branch(tenantId: tenantA.Id, name: "Branch A1", code: "A001");
        var branchA2 = new Branch(tenantId: tenantA.Id, name: "Branch A2", code: "A002");
        ctx.Branches.Add(branchA1);
        ctx.Branches.Add(branchA2);
        await ctx.SaveChangesAsync();

        // Arrange: create branches for tenant B
        tenantContext.TrySetTenant(tenantB.Id);
        var branchB1 = new Branch(tenantId: tenantB.Id, name: "Branch B1", code: "B001");
        var branchB2 = new Branch(tenantId: tenantB.Id, name: "Branch B2", code: "B002");
        ctx.Branches.Add(branchB1);
        ctx.Branches.Add(branchB2);
        await ctx.SaveChangesAsync();

        // Act: set tenant context to A and query
        tenantContext.TrySetTenant(tenantA.Id);
        var branchesForA = ctx.Branches.ToList();

        // Assert: only A's branches returned
        branchesForA.Count.ShouldBe(2);
        branchesForA.All(b => b.TenantId == tenantA.Id).ShouldBeTrue();
        branchesForA.Any(b => b.TenantId == tenantB.Id).ShouldBeFalse();

        // Act: switch context to B and query
        tenantContext.TrySetTenant(tenantB.Id);
        var branchesForB = ctx.Branches.ToList();

        // Assert: only B's branches returned
        branchesForB.Count.ShouldBe(2);
        branchesForB.All(b => b.TenantId == tenantB.Id).ShouldBeTrue();
        branchesForB.Any(b => b.TenantId == tenantA.Id).ShouldBeFalse();
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not have incorrect suffix")]
    public async Task QueryUserBranchAccesses_WithTenantA_ReturnsOnlyAccessesInTenantA()
    {
        await _fixture.ResetAsync();

        var tenantContext = new TestTenantContext();
        var currentUser = new TestCurrentUser();
        await using var ctx = _fixture.CreateContext(tenantContext, currentUser);

        // Arrange: create two tenants with branches
        var tenantA = new Tenant(name: "Tenant A Access", slug: "tenant-a-access-iso");
        var tenantB = new Tenant(name: "Tenant B Access", slug: "tenant-b-access-iso");
        ctx.Tenants.Add(tenantA);
        ctx.Tenants.Add(tenantB);
        await ctx.SaveChangesAsync();

        tenantContext.TrySetTenant(tenantA.Id);
        var branchA = new Branch(tenantId: tenantA.Id, name: "Branch A", code: "BRA");
        ctx.Branches.Add(branchA);
        await ctx.SaveChangesAsync();

        tenantContext.TrySetTenant(tenantB.Id);
        var branchB = new Branch(tenantId: tenantB.Id, name: "Branch B", code: "BRB");
        ctx.Branches.Add(branchB);
        await ctx.SaveChangesAsync();

        // Arrange: create one user and give access to both tenants' branches
        var user = new TallerPro.Domain.Auth.User(email: "multi@test.test", displayName: "Multi Tenant User");
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        tenantContext.TrySetTenant(tenantA.Id);
        var accessA = new TallerPro.Domain.Auth.UserBranchAccess(
            userId: user.Id,
            branchId: branchA.Id,
            tenantId: tenantA.Id,
            role: TallerPro.Domain.Auth.Role.Admin);
        ctx.UserBranchAccesses.Add(accessA);
        await ctx.SaveChangesAsync();

        tenantContext.TrySetTenant(tenantB.Id);
        var accessB = new TallerPro.Domain.Auth.UserBranchAccess(
            userId: user.Id,
            branchId: branchB.Id,
            tenantId: tenantB.Id,
            role: TallerPro.Domain.Auth.Role.Admin);
        ctx.UserBranchAccesses.Add(accessB);
        await ctx.SaveChangesAsync();

        // Act: query accesses with tenant A context
        tenantContext.TrySetTenant(tenantA.Id);
        var accessesForA = ctx.UserBranchAccesses.Where(a => a.UserId == user.Id).ToList();

        // Assert: only A's access returned (D-03: TenantId denormalization filters by tenant)
        accessesForA.Count.ShouldBe(1);
        accessesForA[0].TenantId.ShouldBe(tenantA.Id);
        accessesForA[0].BranchId.ShouldBe(branchA.Id);

        // Act: query accesses with tenant B context
        tenantContext.TrySetTenant(tenantB.Id);
        var accessesForB = ctx.UserBranchAccesses.Where(a => a.UserId == user.Id).ToList();

        // Assert: only B's access returned
        accessesForB.Count.ShouldBe(1);
        accessesForB[0].TenantId.ShouldBe(tenantB.Id);
        accessesForB[0].BranchId.ShouldBe(branchB.Id);
    }
}
