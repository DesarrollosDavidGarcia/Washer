using System.Text.RegularExpressions;
using TallerPro.Domain.Common;

namespace TallerPro.Domain.Tenants;

/// <summary>Root aggregate for a workshop tenant.</summary>
public sealed class Tenant : IAuditable, ISoftDeletable, IConcurrencyAware
{
    private static readonly Regex SlugRegex =
        new(TenantConstants.SlugRegexPattern, RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    public Tenant(string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name must not be empty.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Slug must not be empty.", nameof(slug));
        }

        if (!string.Equals(slug, slug.ToLowerInvariant(), StringComparison.Ordinal))
        {
            throw new ArgumentException("Slug must be lowercase.", nameof(slug));
        }

        if (slug.Length < TenantConstants.SlugMinLength || slug.Length > TenantConstants.SlugMaxLength)
        {
            throw new ArgumentException(
                $"Slug must be between {TenantConstants.SlugMinLength} and {TenantConstants.SlugMaxLength} characters.",
                nameof(slug));
        }

        if (!SlugRegex.IsMatch(slug))
        {
            throw new ArgumentException(
                $"Slug '{slug}' does not match the required pattern.",
                nameof(slug));
        }

        if (TenantConstants.ReservedSlugs.Contains(slug))
        {
            throw new ArgumentException($"Slug '{slug}' is reserved and cannot be used.", nameof(slug));
        }

        Name = name;
        Slug = slug;
        Status = TenantStatus.Active;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RowVersion = Array.Empty<byte>();
    }

    // EF Core constructor
    private Tenant() { }

    public long Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public TenantStatus Status { get; private set; }

    // IAuditable
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }
    public long? CreatedByPlatformId { get; private set; }
    public long? UpdatedByUserId { get; private set; }
    public long? UpdatedByPlatformId { get; private set; }

    // ISoftDeletable
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // IConcurrencyAware
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
}
