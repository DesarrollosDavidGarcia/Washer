namespace TallerPro.Security;

/// <summary>
/// Provides access to the identity of the actor performing the current operation.
/// Exactly one of <see cref="CurrentUserId"/> and <see cref="CurrentPlatformAdminId"/> is non-null
/// for authenticated actors; both are null for system-initiated operations (seeds, migrations).
/// </summary>
public interface ICurrentUser
{
    /// <summary>The Id of the authenticated end-user, or <c>null</c> if the actor is not an end-user.</summary>
    long? CurrentUserId { get; }

    /// <summary>The Id of the authenticated platform admin, or <c>null</c> if the actor is not a platform admin.</summary>
    long? CurrentPlatformAdminId { get; }
}
