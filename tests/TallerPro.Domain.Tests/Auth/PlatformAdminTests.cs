using System.Reflection;
using Shouldly;
using TallerPro.Domain.Auth;
using TallerPro.Domain.Common;
using Xunit;

namespace TallerPro.Domain.Tests.Auth;

public class PlatformAdminTests
{
    [Fact]
    public void PlatformAdmin_WithUppercaseEmail_StoresAsLowercase()
    {
        // Act
        var admin = new PlatformAdmin(email: "FOO@BAR.COM", displayName: "Foo Bar");

        // Assert
        admin.Email.ShouldBe("foo@bar.com");
    }

    [Fact]
    public void PlatformAdmin_WithMixedCaseEmail_StoresAsLowercase()
    {
        // Act
        var admin = new PlatformAdmin(email: "FoO@BaR.cOm", displayName: "Foo Bar");

        // Assert
        admin.Email.ShouldBe("foo@bar.com");
    }

    [Fact]
    public void PlatformAdmin_WithEmptyEmail_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new PlatformAdmin(email: "", displayName: "Foo Bar")
        );
    }

    [Fact]
    public void PlatformAdmin_WithWhitespaceEmail_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new PlatformAdmin(email: "   ", displayName: "Foo Bar")
        );
    }

    [Fact]
    public void PlatformAdmin_WithInvalidEmailNoAtSymbol_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new PlatformAdmin(email: "no-arroba", displayName: "Foo Bar")
        );
    }

    [Fact]
    public void PlatformAdmin_WithEmptyDisplayName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new PlatformAdmin(email: "test@example.com", displayName: "")
        );
    }

    [Fact]
    public void PlatformAdmin_WithWhitespaceDisplayName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new PlatformAdmin(email: "test@example.com", displayName: "   ")
        );
    }

    [Fact]
    public void PlatformAdmin_DoesNotHaveTenantIdProperty()
    {
        // Arrange
        var adminType = typeof(PlatformAdmin);

        // Act
        var tenantIdProperty = adminType.GetProperty("TenantId");

        // Assert
        tenantIdProperty.ShouldBeNull();
    }

    [Fact]
    public void PlatformAdmin_EmailPropertyHasPiiDataAttributeWithHighLevel()
    {
        // Arrange
        var emailProperty = typeof(PlatformAdmin).GetProperty(nameof(PlatformAdmin.Email))!;

        // Act
        var attribute = emailProperty.GetCustomAttribute<PiiDataAttribute>();

        // Assert
        attribute.ShouldNotBeNull();
        attribute!.Level.ShouldBe(PiiLevel.High);
    }

    [Fact]
    public void PlatformAdmin_DisplayNamePropertyHasPiiDataAttributeWithLowLevel()
    {
        // Arrange
        var displayNameProperty = typeof(PlatformAdmin).GetProperty(nameof(PlatformAdmin.DisplayName))!;

        // Act
        var attribute = displayNameProperty.GetCustomAttribute<PiiDataAttribute>();

        // Assert
        attribute.ShouldNotBeNull();
        attribute!.Level.ShouldBe(PiiLevel.Low);
    }

    [Fact]
    public void PlatformAdmin_WithValidEmailAndDisplayName_Succeeds()
    {
        // Act
        var admin = new PlatformAdmin(email: "test@example.com", displayName: "Test Admin");

        // Assert
        admin.Email.ShouldBe("test@example.com");
        admin.DisplayName.ShouldBe("Test Admin");
    }
}
