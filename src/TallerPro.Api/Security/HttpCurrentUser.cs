using TallerPro.Security;

namespace TallerPro.Api.Security;

/// <summary>
/// HTTP-request-scoped implementation of <see cref="ICurrentUser"/>.
/// Returns null for both Ids until spec 004 introduces JWT authentication.
/// </summary>
internal sealed class HttpCurrentUser : ICurrentUser
{
    // Both properties return null: no authentication in spec 003.
    // Spec 004 will populate these from JWT claims.
    public long? CurrentUserId => null;
    public long? CurrentPlatformAdminId => null;
}
