using TallerPro.Domain.Common;

namespace TallerPro.Domain.Auth;

/// <summary>Pivot entity granting a user access to a branch with a specific role.</summary>
public sealed class UserBranchAccess : IAuditable, ISoftDeletable, IConcurrencyAware
{
    public UserBranchAccess(long userId, long branchId, long tenantId, Role role)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("UserId must be greater than zero.", nameof(userId));
        }

        if (branchId <= 0)
        {
            throw new ArgumentException("BranchId must be greater than zero.", nameof(branchId));
        }

        if (tenantId <= 0)
        {
            throw new ArgumentException("TenantId must be greater than zero.", nameof(tenantId));
        }

        if (!Enum.IsDefined(role))
        {
            throw new ArgumentException($"Role value '{(int)role}' is not a valid Role.", nameof(role));
        }

        UserId = userId;
        BranchId = branchId;
        TenantId = tenantId;
        Role = role;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RowVersion = Array.Empty<byte>();
    }

    // EF Core constructor
    private UserBranchAccess() { }

    public long Id { get; private set; }
    public long UserId { get; private set; }
    public long BranchId { get; private set; }
    public long TenantId { get; private set; }
    public Role Role { get; private set; }

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
