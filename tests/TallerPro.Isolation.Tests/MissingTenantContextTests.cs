using Shouldly;
using TallerPro.Domain.Auth;
using TallerPro.Domain.Common;
using TallerPro.Domain.Tenants;
using TallerPro.Isolation.Tests.Fixtures;
using TallerPro.Security;
using Xunit;

namespace TallerPro.Isolation.Tests;

/// <summary>
/// T-27 / CA-07: Verifies that queries to tenant-scoped entities without a resolved tenant context throw MissingTenantContextException.
/// This prevents silent fallback behavior and enforces tenant scoping discipline (D-01).
/// </summary>
[Collection("SqlServerCollection")]
[Trait("Docker", "true")]
public sealed class MissingTenantContextTests
{
    private readonly SqlServerFixture _fixture;

    public MissingTenantContextTests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not have incorrect suffix")]
    public async Task QueryBranches_WithoutTenantContext_ThrowsMissingTenantContextException()
    {
        await _fixture.ResetAsync();

        var tenantContext = new TestTenantContext();  // No tenant set
        var currentUser = new TestCurrentUser();
        await using var ctx = _fixture.CreateContext(tenantContext, currentUser);

        // Arrange: create a tenant and branch (bypassing scoping for setup)
        var tenant = new Tenant(name: "Missing Context Test", slug: "missing-context-test");
        tenantContext.TrySetTenant(1);  // Temporarily set to foundational tenant for setup
        ctx.Tenants.Add(tenant);
        await ctx.SaveChangesAsync();

        // Create branch with proper tenant context
        tenantContext.TrySetTenant(tenant.Id);
        var branch = new Branch(tenantId: tenant.Id, name: "Test Branch", code: "MCTX");
        ctx.Branches.Add(branch);
        await ctx.SaveChangesAsync();

        // Act: clear tenant context
        tenantContext.Clear();

        // Act & Assert: querying without tenant context throws
        var ex = Should.Throw<MissingTenantContextException>(
            () => ctx.Branches.ToList());
        ex.ShouldNotBeNull();
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not have incorrect suffix")]
    public async Task QueryUserBranchAccesses_WithoutTenantContext_ThrowsMissingTenantContextException()
    {
        await _fixture.ResetAsync();

        var tenantContext = new TestTenantContext();
        var currentUser = new TestCurrentUser();
        await using var ctx = _fixture.CreateContext(tenantContext, currentUser);

        // Arrange: create data (with temporary tenant context)
        var tenant = new Tenant(name: "Missing UBA Context", slug: "missing-uba-context");
        tenantContext.TrySetTenant(1);  // foundational
        ctx.Tenants.Add(tenant);
        await ctx.SaveChangesAsync();

        tenantContext.TrySetTenant(tenant.Id);
        var branch = new Branch(tenantId: tenant.Id, name: "Branch", code: "UBAC");
        ctx.Branches.Add(branch);

        var user = new TallerPro.Domain.Auth.User(email: "missing@test.test", displayName: "Missing User");
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var access = new TallerPro.Domain.Auth.UserBranchAccess(
            userId: user.Id,
            branchId: branch.Id,
            tenantId: tenant.Id,
            role: TallerPro.Domain.Auth.Role.Admin);
        ctx.UserBranchAccesses.Add(access);
        await ctx.SaveChangesAsync();

        // Act: clear tenant context
        tenantContext.Clear();

        // Act & Assert: querying UserBranchAccesses without context throws
        var ex = Should.Throw<MissingTenantContextException>(
            () => ctx.UserBranchAccesses.ToList());
        ex.ShouldNotBeNull();
    }
}
