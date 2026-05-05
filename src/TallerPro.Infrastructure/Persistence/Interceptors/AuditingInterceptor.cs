using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TallerPro.Domain.Common;
using TallerPro.Security;

namespace TallerPro.Infrastructure.Persistence.Interceptors;

internal sealed class AuditingInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;

    public AuditingInterceptor(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            ApplyAudit(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            ApplyAudit(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContext context)
    {
        var now = DateTime.UtcNow;
        var userId = _currentUser.CurrentUserId;
        var platformAdminId = _currentUser.CurrentPlatformAdminId;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not IAuditable)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(IAuditable.CreatedAt)).CurrentValue = now;
                entry.Property(nameof(IAuditable.UpdatedAt)).CurrentValue = now;
                entry.Property(nameof(IAuditable.CreatedByUserId)).CurrentValue = userId;
                entry.Property(nameof(IAuditable.CreatedByPlatformId)).CurrentValue = platformAdminId;
                entry.Property(nameof(IAuditable.UpdatedByUserId)).CurrentValue = userId;
                entry.Property(nameof(IAuditable.UpdatedByPlatformId)).CurrentValue = platformAdminId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(IAuditable.UpdatedAt)).CurrentValue = now;
                entry.Property(nameof(IAuditable.UpdatedByUserId)).CurrentValue = userId;
                entry.Property(nameof(IAuditable.UpdatedByPlatformId)).CurrentValue = platformAdminId;
            }
        }
    }
}
