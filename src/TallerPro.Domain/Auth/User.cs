using System.Text.RegularExpressions;
using TallerPro.Domain.Common;

namespace TallerPro.Domain.Auth;

/// <summary>Global user identity — not tenant-scoped (ADR-0009).</summary>
public sealed class User : IAuditable, ISoftDeletable, IConcurrencyAware
{
    private static readonly Regex EmailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    public User(string email, string displayName)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email must not be empty.", nameof(email));
        }

        var normalized = email.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(normalized))
        {
            throw new ArgumentException($"Email '{email}' is not a valid email address.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("DisplayName must not be empty.", nameof(displayName));
        }

        Email = normalized;
        DisplayName = displayName;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RowVersion = Array.Empty<byte>();
    }

    // EF Core constructor
    private User() { }

    public long Id { get; private set; }

    [PiiData(PiiLevel.High)]
    public string Email { get; private set; } = string.Empty;

    [PiiData(PiiLevel.Low)]
    public string DisplayName { get; private set; } = string.Empty;

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
