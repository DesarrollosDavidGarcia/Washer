using TallerPro.Domain.Common;
using TallerPro.Security;

namespace TallerPro.Isolation.Tests.Fixtures;

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

internal sealed class TestCurrentUser : ICurrentUser
{
    public long? CurrentUserId { get; set; }

    public long? CurrentPlatformAdminId { get; set; }
}
