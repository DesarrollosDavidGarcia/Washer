namespace TallerPro.Domain.Tenants;

/// <summary>Domain constants for tenant validation.</summary>
public static class TenantConstants
{
    public const string FundationalSlug = "tallerpro-platform";

    public const int SlugMinLength = 3;
    public const int SlugMaxLength = 40;

    /// <summary>Regex pattern for a valid tenant slug (lowercase alphanumeric + dashes, no leading/trailing dash).</summary>
    public const string SlugRegexPattern = @"^[a-z0-9](?:[a-z0-9-]{1,38}[a-z0-9])?$";

    public static readonly IReadOnlyCollection<string> ReservedSlugs = new HashSet<string>(StringComparer.Ordinal)
    {
        "system", "admin", "root", "api", "www", "mail",
        "tallerpro", "tallerpro-platform", "test", "demo",
        "platform", "super", "internal", "app", "dashboard",
        "support", "billing", "status", "public", "static"
    };
}
