namespace TallerPro.Domain.Common;

/// <summary>Marks an entity as auditable with typed actor FK columns (ADR-0010).</summary>
public interface IAuditable
{
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; }
    long? CreatedByUserId { get; }
    long? CreatedByPlatformId { get; }
    long? UpdatedByUserId { get; }
    long? UpdatedByPlatformId { get; }
}
