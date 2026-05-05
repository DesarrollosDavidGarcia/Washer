using Microsoft.EntityFrameworkCore;
using Shouldly;
using TallerPro.Domain.Auth;
using TallerPro.Domain.Tenants;
using TallerPro.Integration.Tests.Fixtures;
using Xunit;

namespace TallerPro.Integration.Tests.Persistence;

/// <summary>
/// T-26 / D-03: Verifies TenantStamp denormalization and trigger protection.
/// UserBranchAccess.TenantId is automatically set from Branch.TenantId by the interceptor.
/// The trigger TR_UBA_TenantConsistency enforces consistency as defense-in-depth.
/// </summary>
[Collection("SqlServerCollection")]
[Trait("Docker", "true")]
public sealed class TenantStampTests
{
    private readonly SqlServerFixture _fixture;

    public TenantStampTests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CreateUserBranchAccess_TenantIdAutoStamped_FromBranch()
    {
        await _fixture.ResetAsync();

        var tenantContext = new TestTenantContext();
        var currentUser = new TestCurrentUser();
        await using var ctx = _fixture.CreateContext(tenantContext, currentUser);

        // Arrange: create tenant
        var tenant = new Tenant(name: "Stamp Test", slug: "stamp-test");
        ctx.Tenants.Add(tenant);
        await ctx.SaveChangesAsync();

        tenantContext.TrySetTenant(tenant.Id);

        // Arrange: create branch
        var branch = new Branch(tenantId: tenant.Id, name: "Stamp Branch", code: "STMP");
        ctx.Branches.Add(branch);
        await ctx.SaveChangesAsync();

        // Arrange: create user
        var user = new User(email: "stamp@test.test", displayName: "Stamp User");
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        // Act: create access WITHOUT explicitly setting TenantId (rely on interceptor)
        var access = new UserBranchAccess(
            userId: user.Id,
            branchId: branch.Id,
            tenantId: 0,  // Default/unset value
            role: Role.Admin);
        ctx.UserBranchAccesses.Add(access);
        await ctx.SaveChangesAsync();

        // Act: reload to verify interceptor set TenantId
        var reloadedAccess = await ctx.UserBranchAccesses.FindAsync(access.Id);

        // Assert: TenantId was automatically stamped from Branch.TenantId
        reloadedAccess!.TenantId.ShouldBe(branch.TenantId);
        reloadedAccess.TenantId.ShouldBe(tenant.Id);
    }

    [Fact]
    public async Task InsertUserBranchAccess_ViaRawSql_WithMismatchedTenantId_TriggerRejectsWithSqlException()
    {
        await _fixture.ResetAsync();

        var tenantContext = new TestTenantContext();
        var currentUser = new TestCurrentUser();
        await using var ctx = _fixture.CreateContext(tenantContext, currentUser);

        // Arrange: create two tenants
        var tenantA = new Tenant(name: "Tenant A", slug: "tenant-a");
        var tenantB = new Tenant(name: "Tenant B", slug: "tenant-b");
        ctx.Tenants.Add(tenantA);
        ctx.Tenants.Add(tenantB);
        await ctx.SaveChangesAsync();

        tenantContext.TrySetTenant(tenantA.Id);

        // Arrange: create branch owned by tenantA
        var branch = new Branch(tenantId: tenantA.Id, name: "Trigger Test Branch", code: "TRIG");
        ctx.Branches.Add(branch);
        await ctx.SaveChangesAsync();

        // Arrange: create user
        var user = new User(email: "trigger@test.test", displayName: "Trigger User");
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        // Act: bypass EF interceptors entirely via raw SQL — insert UBA with TenantId = tenantB (mismatches Branch.TenantId = tenantA).
        // The trigger TR_UBA_TenantConsistency is the only defense at this layer.
        var sql = $"""
            INSERT INTO [auth].[UserBranchAccesses]
                (UserId, BranchId, TenantId, Role, IsDeleted, CreatedAt, UpdatedAt)
            VALUES
                ({user.Id}, {branch.Id}, {tenantB.Id}, 1, 0, SYSUTCDATETIME(), SYSUTCDATETIME())
            """;

        Func<Task> act = () => ctx.Database.ExecuteSqlRawAsync(sql);

        // Assert: trigger rejects the raw insert with SqlException containing the trigger error message
        var ex = await Should.ThrowAsync<Microsoft.Data.SqlClient.SqlException>(act);
        ex.Message.ShouldContain("UserBranchAccess.TenantId must equal Branch.TenantId");
    }
}
