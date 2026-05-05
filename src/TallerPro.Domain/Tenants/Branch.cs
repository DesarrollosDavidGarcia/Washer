using System.Text.RegularExpressions;
using TallerPro.Domain.Common;

namespace TallerPro.Domain.Tenants;

/// <summary>A physical or logical branch belonging to a tenant.</summary>
public sealed class Branch : IAuditable, ISoftDeletable, IConcurrencyAware, ITenantOwned
{
    private static readonly Regex CodeRegex =
        new(@"^[A-Z0-9-]{2,20}$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    public Branch(long tenantId, string name, string code)
    {
        if (tenantId <= 0)
        {
            throw new ArgumentException("TenantId must be greater than zero.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name must not be empty.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code must not be empty.", nameof(code));
        }

        if (!CodeRegex.IsMatch(code))
        {
            throw new ArgumentException(
                $"Code '{code}' is invalid. Must match ^[A-Z0-9-]{{2,20}}$.",
                nameof(code));
        }

        TenantId = tenantId;
        Name = name;
        Code = code;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RowVersion = Array.Empty<byte>();
    }

    // EF Core constructor
    private Branch() { }

    public long Id { get; private set; }
    public long TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;

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
