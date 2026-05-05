using System.Reflection;
using Shouldly;
using TallerPro.Domain.Common;
using Xunit;

namespace TallerPro.Domain.Tests.Common;

public class PiiDataAttributeTests
{
    [Fact]
    public void PiiDataAttribute_WithHighLevel_ReadCorrectlyByReflection()
    {
        // Arrange
        var propertyInfo = typeof(TestClassWithHighPii).GetProperty(nameof(TestClassWithHighPii.SensitiveField))!;
        var attribute = propertyInfo.GetCustomAttribute<PiiDataAttribute>();

        // Act
        var level = attribute!.Level;

        // Assert
        level.ShouldBe(PiiLevel.High);
    }

    [Fact]
    public void PiiDataAttribute_WithoutLevel_DefaultsToLow()
    {
        // Arrange
        var propertyInfo = typeof(TestClassWithDefaultPii).GetProperty(nameof(TestClassWithDefaultPii.MildData))!;
        var attribute = propertyInfo.GetCustomAttribute<PiiDataAttribute>();

        // Act
        var level = attribute!.Level;

        // Assert
        level.ShouldBe(PiiLevel.Low);
    }

    [Fact]
    public void PiiDataAttribute_CanOnlyBeAppliedToProperty()
    {
        // Arrange
        var attributeType = typeof(PiiDataAttribute);

        // Act
        var attributeUsage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        attributeUsage.ShouldNotBeNull();
        (attributeUsage!.ValidOn & AttributeTargets.Property).ShouldBe(AttributeTargets.Property);
    }

    [Fact]
    public void PiiDataAttribute_AttributeUsageExcludesClass()
    {
        // Arrange
        var attributeType = typeof(PiiDataAttribute);

        // Act
        var attributeUsage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        attributeUsage.ShouldNotBeNull();
        (attributeUsage!.ValidOn & AttributeTargets.Class).ShouldNotBe(AttributeTargets.Class);
    }

    // Test classes for reflection
    private class TestClassWithHighPii
    {
        [PiiData(level: PiiLevel.High)]
        public string SensitiveField { get; set; } = string.Empty;
    }

    private class TestClassWithDefaultPii
    {
        [PiiData]
        public string MildData { get; set; } = string.Empty;
    }
}
