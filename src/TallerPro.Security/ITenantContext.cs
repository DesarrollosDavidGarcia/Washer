namespace TallerPro.Security;

/// <summary>
/// Provides ambient access to the currently resolved tenant for the active execution context.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The Id of the current tenant.
    /// Throws <see cref="MissingTenantContextException"/> if no tenant has been resolved yet.
    /// </summary>
    long CurrentTenantId { get; }

    /// <summary>Returns <c>true</c> when a tenant has been set in the current execution context.</summary>
    bool IsResolved { get; }

    /// <summary>Sets the current tenant for the ambient execution context.</summary>
    void TrySetTenant(long id);

    /// <summary>
    /// Clears the current tenant from the ambient execution context.
    /// Idempotent — safe to call even if no tenant was set.
    /// </summary>
    void Clear();
}
