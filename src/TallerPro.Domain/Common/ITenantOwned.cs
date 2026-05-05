namespace TallerPro.Domain.Common;

/// <summary>Marks an entity as belonging to a specific tenant (tenant-scoped).</summary>
public interface ITenantOwned
{
    long TenantId { get; }
}
