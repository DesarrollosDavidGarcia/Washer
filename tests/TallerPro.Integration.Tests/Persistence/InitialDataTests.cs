using Shouldly;
using TallerPro.Domain.Auth;
using TallerPro.Domain.Tenants;
using TallerPro.Integration.Tests.Fixtures;
using Xunit;

namespace TallerPro.Integration.Tests.Persistence;

/// <summary>
/// T-25 / CA-03: Verifies that initial data can be created and persisted correctly.
/// Tests creation of tenant, branches, users, platform admins, and access relationships.
/// </summary>
[Collection("SqlServerCollection")]
[Trait("Docker", "true")]
public sealed class InitialDataTests
{
    private readonly SqlServerFixture _fixture;

    public InitialDataTests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CreateInitialData_AllEntitiesPersist_VerifyBasicQueries()
    {
        // Reset database to clean state
        await _fixture.ResetAsync();

        var tenantContext = new TestTenantContext();
        var currentUser = new TestCurrentUser();
        await using var ctx = _fixture.CreateContext(tenantContext, currentUser);

        // Arrange: create tenant
        var tenant = new Tenant(name: "ACME Corporation", slug: "acme");
        ctx.Tenants.Add(tenant);
        await ctx.SaveChangesAsync();

        tenant.Id.ShouldBeGreaterThan(0);

        // Set tenant context for subsequent operations
        tenantContext.TrySetTenant(tenant.Id);

        // Arrange: create branches
        var branch1 = new Branch(tenantId: tenant.Id, name: "Mexico City", code: "B001");
        var branch2 = new Branch(tenantId: tenant.Id, name: "Guadalajara", code: "B002");
        ctx.Branches.Add(branch1);
        ctx.Branches.Add(branch2);
        await ctx.SaveChangesAsync();

        branch1.Id.ShouldBeGreaterThan(0);
        branch2.Id.ShouldBeGreaterThan(0);

        // Arrange: create user
        var user = new User(email: "owner@acme.test", displayName: "Owner User");
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        user.Id.ShouldBeGreaterThan(0);

        // Arrange: create platform admin
        var platformAdmin = new PlatformAdmin(email: "dev@tallerpro.local", displayName: "Platform Dev");
        ctx.PlatformAdmins.Add(platformAdmin);
        await ctx.SaveChangesAsync();

        platformAdmin.Id.ShouldBeGreaterThan(0);

        // Arrange: create user-branch accesses
        var access1 = new UserBranchAccess(
            userId: user.Id,
            branchId: branch1.Id,
            tenantId: tenant.Id,
            role: Role.Admin);
        var access2 = new UserBranchAccess(
            userId: user.Id,
            branchId: branch2.Id,
            tenantId: tenant.Id,
            role: Role.Admin);
        ctx.UserBranchAccesses.Add(access1);
        ctx.UserBranchAccesses.Add(access2);
        await ctx.SaveChangesAsync();

        access1.Id.ShouldBeGreaterThan(0);
        access2.Id.ShouldBeGreaterThan(0);

        // Act: query all entities to verify persistence
        var retrievedTenant = await ctx.Tenants.FindAsync(tenant.Id);
        var retrievedBranches = ctx.Branches.Where(b => b.TenantId == tenant.Id).ToList();
        var retrievedUser = await ctx.Users.FindAsync(user.Id);
        var retrievedAdmin = await ctx.PlatformAdmins.FindAsync(platformAdmin.Id);
        var retrievedAccesses = ctx.UserBranchAccesses
            .Where(a => a.UserId == user.Id && a.TenantId == tenant.Id)
            .ToList();

        // Assert: all entities persist and are retrievable
        retrievedTenant.ShouldNotBeNull();
        retrievedTenant.Name.ShouldBe("ACME Corporation");
        retrievedTenant.Slug.ShouldBe("acme");

        retrievedBranches.Count.ShouldBe(2);
        retrievedBranches.Any(b => b.Code == "B001").ShouldBeTrue();
        retrievedBranches.Any(b => b.Code == "B002").ShouldBeTrue();

        retrievedUser.ShouldNotBeNull();
        retrievedUser.Email.ShouldBe("owner@acme.test");

        retrievedAdmin.ShouldNotBeNull();
        retrievedAdmin.Email.ShouldBe("dev@tallerpro.local");

        retrievedAccesses.Count.ShouldBe(2);
    }
}
