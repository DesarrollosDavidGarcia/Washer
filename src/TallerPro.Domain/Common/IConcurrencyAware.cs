namespace TallerPro.Domain.Common;

/// <summary>Marks an entity as carrying an optimistic-concurrency token.</summary>
public interface IConcurrencyAware
{
    byte[] RowVersion { get; }
}
