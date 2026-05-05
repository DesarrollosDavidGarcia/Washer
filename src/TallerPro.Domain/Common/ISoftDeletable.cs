namespace TallerPro.Domain.Common;

/// <summary>Marks an entity as soft-deletable instead of physically removed.</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
}
