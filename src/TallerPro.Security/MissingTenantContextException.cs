namespace TallerPro.Security;

/// <summary>Thrown when <see cref="ITenantContext.CurrentTenantId"/> is accessed before a tenant has been resolved.</summary>
public sealed class MissingTenantContextException : InvalidOperationException
{
    public MissingTenantContextException()
        : base("Tenant context not resolved. Call TrySetTenant before accessing CurrentTenantId.")
    {
    }

    public MissingTenantContextException(string message)
        : base(message)
    {
    }

    public MissingTenantContextException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
