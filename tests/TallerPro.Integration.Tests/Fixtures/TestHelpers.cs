using TallerPro.Domain.Common;
using TallerPro.Security;

namespace TallerPro.Integration.Tests.Fixtures;

/// <summary>
/// Test implementations of security contexts used during integration tests.
/// </summary>
internal sealed class TestTenantContext : ITenantContext
{
    public long? Tenant { get; set; }

    public long CurrentTenantId
    {
        get => Tenant ?? throw new MissingTenantContextException("No tenant context has been resolved.");
        private init { }
    }

    public bool IsResolved => Tenant.HasValue;

    public void TrySetTenant(long id) => Tenant = id;

    public void Clear() => Tenant = null;
}

/// <summary>
/// Test implementation of current user, tracking User and PlatformAdmin actors independently.
/// </summary>
internal sealed class TestCurrentUser : ICurrentUser
{
    public long? CurrentUserId { get; set; }

    public long? CurrentPlatformAdminId { get; set; }
}
