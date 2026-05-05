using Shouldly;
using TallerPro.Domain.Auth;
using Xunit;

namespace TallerPro.Domain.Tests.Auth;

public class RoleTests
{
    [Fact]
    public void Role_EnumHasExactlyThreeValues()
    {
        // Act
        var roleValues = Enum.GetValues(typeof(Role));

        // Assert
        roleValues.Length.ShouldBe(3);
    }

    [Fact]
    public void Role_ContainsSuperAdminWithValueZero()
    {
        // Arrange & Act
        var superAdmin = Role.SuperAdmin;

        // Assert
        ((int)superAdmin).ShouldBe(0);
    }

    [Fact]
    public void Role_ContainsAdminWithValueOne()
    {
        // Arrange & Act
        var admin = Role.Admin;

        // Assert
        ((int)admin).ShouldBe(1);
    }

    [Fact]
    public void Role_ContainsClienteWithValueTwo()
    {
        // Arrange & Act
        var cliente = Role.Cliente;

        // Assert
        ((int)cliente).ShouldBe(2);
    }
}
