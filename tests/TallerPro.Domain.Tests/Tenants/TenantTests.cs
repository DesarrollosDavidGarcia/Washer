using Shouldly;
using TallerPro.Domain.Tenants;
using Xunit;

namespace TallerPro.Domain.Tests.Tenants;

public class TenantTests
{
    [Fact]
    public void Tenant_WithEmptySlug_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Tenant(name: "Test Tenant", slug: "")
        );
    }

    [Fact]
    public void Tenant_WithWhitespaceSlug_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Tenant(name: "Test Tenant", slug: "   ")
        );
    }

    [Fact]
    public void Tenant_WithUppercaseSlug_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Tenant(name: "Test Tenant", slug: "FOO")
        );
    }

    [Fact]
    public void Tenant_WithMixedCaseSlug_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Tenant(name: "Test Tenant", slug: "TestSlug")
        );
    }

    [Fact]
    public void Tenant_WithSlugTooShort_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Tenant(name: "Test Tenant", slug: "ab")
        );
    }

    [Fact]
    public void Tenant_WithSlugTooLong_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Tenant(name: "Test Tenant", slug: "this-is-a-slug-that-exceeds-forty-chars-limit")
        );
    }

    [Fact]
    public void Tenant_WithReservedSlug_System_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Tenant(name: "Test Tenant", slug: "system")
        );
    }

    [Fact]
    public void Tenant_WithReservedSlug_Admin_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Tenant(name: "Test Tenant", slug: "admin")
        );
    }

    [Fact]
    public void Tenant_WithReservedSlug_TallerProPlatform_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Tenant(name: "Test Tenant", slug: "tallerpro-platform")
        );
    }

    [Fact]
    public void Tenant_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Tenant(name: "", slug: "valid-slug")
        );
    }

    [Fact]
    public void Tenant_WithWhitespaceName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Tenant(name: "   ", slug: "valid-slug")
        );
    }

    [Fact]
    public void Tenant_StatusDefaultsToActive()
    {
        // Arrange & Act
        var tenant = new Tenant(name: "Test Tenant", slug: "test-slug");

        // Assert
        tenant.Status.ShouldBe(TenantStatus.Active);
    }

    [Theory]
    [InlineData("ab")]
    public void Tenant_WithSlugLengthTwo_ThrowsArgumentException(string slug)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Tenant(name: "Test Tenant", slug: slug)
        );
    }

    [Theory]
    [InlineData("abc")]
    public void Tenant_WithSlugLengthThree_Succeeds(string slug)
    {
        // Act
        var tenant = new Tenant(name: "Test Tenant", slug: slug);

        // Assert
        tenant.Slug.ShouldBe(slug);
    }

    [Theory]
    [InlineData("abcdefghijklmnopqrstuvwxyz0123456789-abc")] // 40 chars: 26 + 10 + 4 (incluye un guion del medio + 3 alfanum finales)
    public void Tenant_WithSlugLengthForty_Succeeds(string slug)
    {
        // Sanity check del fixture
        slug.Length.ShouldBe(40);

        // Act
        var tenant = new Tenant(name: "Test Tenant", slug: slug);

        // Assert
        tenant.Slug.ShouldBe(slug);
    }

    [Theory]
    [InlineData("abcdefghijklmnopqrstuvwxyz0123456789-abcd")] // 41 chars
    public void Tenant_WithSlugLengthFortyOne_ThrowsArgumentException(string slug)
    {
        // Sanity check del fixture
        slug.Length.ShouldBe(41);

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new Tenant(name: "Test Tenant", slug: slug)
        );
    }
}
