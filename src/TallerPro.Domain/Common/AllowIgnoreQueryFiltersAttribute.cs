namespace TallerPro.Domain.Common;

/// <summary>Authorizes use of IgnoreQueryFilters() on the decorated method; Reason must be non-empty.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class AllowIgnoreQueryFiltersAttribute : Attribute
{
    public string Reason { get; }

    public AllowIgnoreQueryFiltersAttribute(string reason)
    {
        Reason = reason ?? string.Empty;
    }
}
