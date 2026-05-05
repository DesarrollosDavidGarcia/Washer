using Shouldly;
using TallerPro.Domain.Auth;
using Xunit;

namespace TallerPro.Domain.Tests.Auth;

public class UserBranchAccessTests
{
    [Fact]
    public void UserBranchAccess_WithUserIdZero_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new UserBranchAccess(userId: 0, branchId: 1, tenantId: 1, role: Role.Admin)
        );
    }

    [Fact]
    public void UserBranchAccess_WithNegativeUserId_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new UserBranchAccess(userId: -1, branchId: 1, tenantId: 1, role: Role.Admin)
        );
    }

    [Fact]
    public void UserBranchAccess_WithBranchIdZero_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new UserBranchAccess(userId: 1, branchId: 0, tenantId: 1, role: Role.Admin)
        );
    }

    [Fact]
    public void UserBranchAccess_WithNegativeBranchId_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new UserBranchAccess(userId: 1, branchId: -1, tenantId: 1, role: Role.Admin)
        );
    }

    [Fact]
    public void UserBranchAccess_WithTenantIdZero_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new UserBranchAccess(userId: 1, branchId: 1, tenantId: 0, role: Role.Admin)
        );
    }

    [Fact]
    public void UserBranchAccess_WithNegativeTenantId_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new UserBranchAccess(userId: 1, branchId: 1, tenantId: -1, role: Role.Admin)
        );
    }

    [Fact]
    public void UserBranchAccess_WithValidAllIds_Succeeds()
    {
        // Act
        var access = new UserBranchAccess(userId: 1, branchId: 2, tenantId: 3, role: Role.Admin);

        // Assert
        access.UserId.ShouldBe(1);
        access.BranchId.ShouldBe(2);
        access.TenantId.ShouldBe(3);
    }

    [Theory]
    [InlineData(Role.SuperAdmin)]
    [InlineData(Role.Admin)]
    [InlineData(Role.Cliente)]
    public void UserBranchAccess_WithValidRole_Succeeds(Role role)
    {
        // Act
        var access = new UserBranchAccess(userId: 1, branchId: 1, tenantId: 1, role: role);

        // Assert
        access.Role.ShouldBe(role);
    }
}
