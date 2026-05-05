using System.Reflection;
using Shouldly;
using TallerPro.Domain.Auth;
using TallerPro.Domain.Common;
using Xunit;

namespace TallerPro.Domain.Tests.Auth;

public class UserTests
{
    [Fact]
    public void User_WithUppercaseEmail_StoresAsLowercase()
    {
        // Act
        var user = new User(email: "FOO@BAR.COM", displayName: "Foo Bar");

        // Assert
        user.Email.ShouldBe("foo@bar.com");
    }

    [Fact]
    public void User_WithMixedCaseEmail_StoresAsLowercase()
    {
        // Act
        var user = new User(email: "FoO@BaR.cOm", displayName: "Foo Bar");

        // Assert
        user.Email.ShouldBe("foo@bar.com");
    }

    [Fact]
    public void User_WithEmptyEmail_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new User(email: "", displayName: "Foo Bar")
        );
    }

    [Fact]
    public void User_WithWhitespaceEmail_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new User(email: "   ", displayName: "Foo Bar")
        );
    }

    [Fact]
    public void User_WithInvalidEmailNoAtSymbol_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new User(email: "no-arroba", displayName: "Foo Bar")
        );
    }

    [Fact]
    public void User_WithEmptyDisplayName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new User(email: "test@example.com", displayName: "")
        );
    }

    [Fact]
    public void User_WithWhitespaceDisplayName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new User(email: "test@example.com", displayName: "   ")
        );
    }

    [Fact]
    public void User_DoesNotHaveTenantIdProperty()
    {
        // Arrange
        var userType = typeof(User);

        // Act
        var tenantIdProperty = userType.GetProperty("TenantId");

        // Assert
        tenantIdProperty.ShouldBeNull();
    }

    [Fact]
    public void User_EmailPropertyHasPiiDataAttributeWithHighLevel()
    {
        // Arrange
        var emailProperty = typeof(User).GetProperty(nameof(User.Email))!;

        // Act
        var attribute = emailProperty.GetCustomAttribute<PiiDataAttribute>();

        // Assert
        attribute.ShouldNotBeNull();
        attribute!.Level.ShouldBe(PiiLevel.High);
    }

    [Fact]
    public void User_DisplayNamePropertyHasPiiDataAttributeWithLowLevel()
    {
        // Arrange
        var displayNameProperty = typeof(User).GetProperty(nameof(User.DisplayName))!;

        // Act
        var attribute = displayNameProperty.GetCustomAttribute<PiiDataAttribute>();

        // Assert
        attribute.ShouldNotBeNull();
        attribute!.Level.ShouldBe(PiiLevel.Low);
    }

    [Fact]
    public void User_WithValidEmailAndDisplayName_Succeeds()
    {
        // Act
        var user = new User(email: "test@example.com", displayName: "Test User");

        // Assert
        user.Email.ShouldBe("test@example.com");
        user.DisplayName.ShouldBe("Test User");
    }
}
