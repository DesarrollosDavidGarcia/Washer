using Shouldly;
using TallerPro.Domain.Tenants;
using Xunit;

namespace TallerPro.Domain.Tests.Tenants;

public class BranchTests
{
    [Fact]
    public void Branch_WithTenantIdZero_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Branch(tenantId: 0, name: "Branch Name", code: "MAIN")
        );
    }

    [Fact]
    public void Branch_WithNegativeTenantId_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Branch(tenantId: -1, name: "Branch Name", code: "MAIN")
        );
    }

    [Fact]
    public void Branch_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Branch(tenantId: 1, name: "", code: "MAIN")
        );
    }

    [Fact]
    public void Branch_WithWhitespaceName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Branch(tenantId: 1, name: "   ", code: "MAIN")
        );
    }

    [Fact]
    public void Branch_WithEmptyCode_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Branch(tenantId: 1, name: "Branch Name", code: "")
        );
    }

    [Fact]
    public void Branch_WithWhitespaceCode_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Branch(tenantId: 1, name: "Branch Name", code: "   ")
        );
    }

    [Fact]
    public void Branch_WithCodeContainingLowercase_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Branch(tenantId: 1, name: "Branch Name", code: "main")
        );
    }

    [Fact]
    public void Branch_WithCodeContainingSpace_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Branch(tenantId: 1, name: "Branch Name", code: "MA IN")
        );
    }

    [Fact]
    public void Branch_WithCodeContainingSpecialCharacters_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Branch(tenantId: 1, name: "Branch Name", code: "MA@IN")
        );
    }

    [Fact]
    public void Branch_WithCodeContainingUnderscore_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Branch(tenantId: 1, name: "Branch Name", code: "MA_IN")
        );
    }

    [Fact]
    public void Branch_WithValidCode_Succeeds()
    {
        // Act
        var branch = new Branch(tenantId: 1, name: "Branch Name", code: "MAIN");

        // Assert
        branch.Code.ShouldBe("MAIN");
    }

    [Fact]
    public void Branch_WithCodeContainingNumbers_Succeeds()
    {
        // Act
        var branch = new Branch(tenantId: 1, name: "Branch Name", code: "BR01");

        // Assert
        branch.Code.ShouldBe("BR01");
    }

    [Fact]
    public void Branch_WithCodeContainingDash_Succeeds()
    {
        // Act
        var branch = new Branch(tenantId: 1, name: "Branch Name", code: "BR-01");

        // Assert
        branch.Code.ShouldBe("BR-01");
    }

    [Fact]
    public void Branch_WithCodeTwoCharacters_Succeeds()
    {
        // Act
        var branch = new Branch(tenantId: 1, name: "Branch Name", code: "AB");

        // Assert
        branch.Code.ShouldBe("AB");
    }

    [Fact]
    public void Branch_WithCodeTwentyCharacters_Succeeds()
    {
        // Act
        var branch = new Branch(tenantId: 1, name: "Branch Name", code: "AB-01-23-45-67-89-XY");

        // Assert
        branch.Code.ShouldBe("AB-01-23-45-67-89-XY");
    }

    [Fact]
    public void Branch_WithValidTenantId_Succeeds()
    {
        // Act
        var branch = new Branch(tenantId: 42, name: "Branch Name", code: "MAIN");

        // Assert
        branch.TenantId.ShouldBe(42);
    }
}
