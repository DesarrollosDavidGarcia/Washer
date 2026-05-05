using Microsoft.EntityFrameworkCore;
using Shouldly;
using TallerPro.Domain.Auth;
using TallerPro.Domain.Tenants;
using TallerPro.Integration.Tests.Fixtures;
using Xunit;

namespace TallerPro.Integration.Tests.Persistence;

/// <summary>
/// T-26 / CA-06: Verifies auditing behavior.
/// CreatedAt/UpdatedAt are set automatically; CreatedByUserId/CreatedByPlatformId are populated from ICurrentUser.
/// </summary>
[Collection("SqlServerCollection")]
[Trait("Docker", "true")]
public sealed class AuditingTests
{
    private readonly SqlServerFixture _fixture;

    public AuditingTests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CreateEntity_AuditColumnsPopulated_WithCurrentUser()
    {
        await _fixture.ResetAsync();

        var tenantContext = new TestTenantContext();
        var currentUser = new TestCurrentUser { CurrentUserId = 5 };
        await using var ctx = _fixture.CreateContext(tenantContext, currentUser);

        // Arrange: create user (to have a valid UserId for audit)
        var creatingUser = new User(email: "auditor@test.test", displayName: "Auditor");
        ctx.Users.Add(creatingUser);
        await ctx.SaveChangesAsync();

        var actualUserId = creatingUser.Id;
        currentUser.CurrentUserId = actualUserId;

        // Arrange: create tenant with known actor
        var beforeCreation = DateTime.UtcNow;
        var tenant = new Tenant(name: "Audit Test Tenant", slug: "audit-test");
        ctx.Tenants.Add(tenant);
        await ctx.SaveChangesAsync();
        var afterCreation = DateTime.UtcNow;

        // Act: reload to verify audit columns
        var reloadedTenant = await ctx.Tenants.AsNoTracking().FirstAsync(t => t.Id == tenant.Id);

        // Assert: audit columns populated
        reloadedTenant.CreatedAt.ShouldBeGreaterThanOrEqualTo(beforeCreation);
        reloadedTenant.CreatedAt.ShouldBeLessThanOrEqualTo(afterCreation);
        reloadedTenant.UpdatedAt.ShouldBeGreaterThanOrEqualTo(beforeCreation);
        reloadedTenant.UpdatedAt.ShouldBeLessThanOrEqualTo(afterCreation);
        reloadedTenant.CreatedByUserId.ShouldBe(actualUserId);
        reloadedTenant.CreatedByPlatformId.ShouldBeNull();
        reloadedTenant.UpdatedByUserId.ShouldBe(actualUserId);
        reloadedTenant.UpdatedByPlatformId.ShouldBeNull();
    }

    [Fact]
    public async Task ModifyEntity_UpdatedAtAndUserIdUpdated()
    {
        await _fixture.ResetAsync();

        var tenantContext = new TestTenantContext();
        var currentUser = new TestCurrentUser { CurrentUserId = null };
        await using var ctx = _fixture.CreateContext(tenantContext, currentUser);

        // Arrange: create user for audit trail
        var user1 = new User(email: "user1@test.test", displayName: "User 1");
        ctx.Users.Add(user1);
        await ctx.SaveChangesAsync();

        currentUser.CurrentUserId = user1.Id;

        // Arrange: create tenant
        var tenant = new Tenant(name: "Modification Test", slug: "modify-test");
        ctx.Tenants.Add(tenant);
        await ctx.SaveChangesAsync();

        var createdAtInitial = tenant.CreatedAt;
        var createdByInitial = tenant.CreatedByUserId;

        // Wait a moment to ensure UpdatedAt differs
        await Task.Delay(10);

        // Act: mark tenant as modified to trigger update
        ctx.Tenants.Update(tenant);
        await ctx.SaveChangesAsync();

        // Act: reload to capture updates
        var reloadedTenant = await ctx.Tenants.AsNoTracking().FirstAsync(t => t.Id == tenant.Id);

        // Assert: CreatedAt unchanged, UpdatedAt changed
        reloadedTenant.CreatedAt.ShouldBe(createdAtInitial);
        reloadedTenant.CreatedByUserId.ShouldBe(createdByInitial);
        reloadedTenant.UpdatedAt.ShouldBeGreaterThan(createdAtInitial);
        reloadedTenant.UpdatedByUserId.ShouldBe(user1.Id);
    }

    [Fact]
    public async Task CreateEntity_AuditColumnsPopulated_WithPlatformAdmin()
    {
        await _fixture.ResetAsync();

        var tenantContext = new TestTenantContext();
        var currentUser = new TestCurrentUser { CurrentPlatformAdminId = 7 };
        await using var ctx = _fixture.CreateContext(tenantContext, currentUser);

        // Arrange: create platform admin
        var admin = new PlatformAdmin(email: "admin@tallerpro.local", displayName: "Admin");
        ctx.PlatformAdmins.Add(admin);
        await ctx.SaveChangesAsync();

        var adminId = admin.Id;
        currentUser.CurrentPlatformAdminId = adminId;

        // Arrange: create tenant as platform admin
        var tenant = new Tenant(name: "Platform Admin Creation", slug: "platform-admin-test");
        ctx.Tenants.Add(tenant);
        await ctx.SaveChangesAsync();

        // Act: reload
        var reloadedTenant = await ctx.Tenants.AsNoTracking().FirstAsync(t => t.Id == tenant.Id);

        // Assert: PlatformAdmin audit columns set, User columns null
        reloadedTenant.CreatedByPlatformId.ShouldBe(adminId);
        reloadedTenant.CreatedByUserId.ShouldBeNull();
        reloadedTenant.UpdatedByPlatformId.ShouldBe(adminId);
        reloadedTenant.UpdatedByUserId.ShouldBeNull();
    }

    [Fact]
    public async Task CreateEntity_AuditColumnsPopulated_SystemOperation()
    {
        await _fixture.ResetAsync();

        var tenantContext = new TestTenantContext();
        var currentUser = new TestCurrentUser { CurrentUserId = null, CurrentPlatformAdminId = null };
        await using var ctx = _fixture.CreateContext(tenantContext, currentUser);

        // Arrange: create tenant with no current actor (system operation)
        var tenant = new Tenant(name: "System Creation", slug: "system-test");
        ctx.Tenants.Add(tenant);
        await ctx.SaveChangesAsync();

        // Act: reload
        var reloadedTenant = await ctx.Tenants.AsNoTracking().FirstAsync(t => t.Id == tenant.Id);

        // Assert: both audit actor columns are null
        reloadedTenant.CreatedByUserId.ShouldBeNull();
        reloadedTenant.CreatedByPlatformId.ShouldBeNull();
        reloadedTenant.UpdatedByUserId.ShouldBeNull();
        reloadedTenant.UpdatedByPlatformId.ShouldBeNull();
    }
}
