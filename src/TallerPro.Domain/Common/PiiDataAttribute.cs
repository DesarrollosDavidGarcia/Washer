namespace TallerPro.Domain.Common;

/// <summary>Marks a property as containing PII data that must be masked in logs.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PiiDataAttribute : Attribute
{
    public PiiDataAttribute(PiiLevel level = PiiLevel.Low)
    {
        Level = level;
    }

    public PiiLevel Level { get; }
}
