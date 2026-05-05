namespace TallerPro.Security;

/// <summary>
/// AsyncLocal-based implementation of <see cref="ITenantContext"/>.
/// Each logical execution context (Task, request) gets its own isolated tenant value.
/// The static AsyncLocal ensures that child tasks inherit the parent's value at capture time,
/// but mutations in child tasks do not propagate back to the parent (AsyncLocal copy-on-write semantics).
/// </summary>
public sealed class AmbientTenantContext : ITenantContext
{
    // Static so that the isolation works across the logical call context (AsyncLocal semantics).
    // Each Task.Run branch gets its own copy of the value after the branch point.
    private static readonly AsyncLocal<long?> Current = new();

    /// <inheritdoc/>
    public long CurrentTenantId =>
        Current.Value ?? throw new MissingTenantContextException();

    /// <inheritdoc/>
    public bool IsResolved => Current.Value.HasValue;

    /// <inheritdoc/>
    public void TrySetTenant(long id) => Current.Value = id;

    /// <inheritdoc/>
    public void Clear() => Current.Value = null;
}
