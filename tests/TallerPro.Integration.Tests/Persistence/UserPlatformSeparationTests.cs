using Shouldly;
using TallerPro.Domain.Auth;
using TallerPro.Integration.Tests.Fixtures;
using Xunit;

namespace TallerPro.Integration.Tests.Persistence;

/// <summary>
/// T-26 / CA-11: Verifies separation between Users and PlatformAdmins.
/// Querying Users does not return PlatformAdmins and vice versa.
/// </summary>
[Collection("SqlServerCollection")]
[Trait("Docker", "true")]
public sealed class UserPlatformSeparationTests
{
    private readonly SqlServerFixture _fixture;

    public UserPlatformSeparationTests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Users_Query_DoesNotIncludePlatformAdmins()
    {
        await _fixture.ResetAsync();

        var tenantContext = new TestTenantContext();
        var currentUser = new TestCurrentUser();
        await using var ctx = _fixture.CreateContext(tenantContext, currentUser);

        // Arrange: create a user
        var user = new User(email: "user@test.test", displayName: "Regular User");
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        // Arrange: create a platform admin
        var platformAdmin = new PlatformAdmin(email: "admin@tallerpro.local", displayName: "Platform Admin");
        ctx.PlatformAdmins.Add(platformAdmin);
        await ctx.SaveChangesAsync();

        // Act: query users
        var allUsers = ctx.Users.ToList();
        var allAdmins = ctx.PlatformAdmins.ToList();

        // Assert: users list contains only the user, not the admin
        allUsers.Count.ShouldBe(1);
        allUsers[0].Email.ShouldBe("user@test.test");
        allUsers.Any(u => u.Email == "admin@tallerpro.local").ShouldBeFalse();

        // Assert: admins list contains only the admin, not the user
        allAdmins.Count.ShouldBe(1);
        allAdmins[0].Email.ShouldBe("admin@tallerpro.local");
        allAdmins.Any(a => a.Email == "user@test.test").ShouldBeFalse();
    }
}
