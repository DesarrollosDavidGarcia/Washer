using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TallerPro.Domain.Tenants;

namespace TallerPro.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Blocks soft delete and status changes to Suspended/Cancelled on the fundational tenant.
/// Identification is by Slug == TenantConstants.FundationalSlug (R-1), never by Id.
/// </summary>
internal sealed class FundationalTenantGuard : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            GuardFundationalTenant(eventData.Context);
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
            GuardFundationalTenant(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void GuardFundationalTenant(DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries<Tenant>())
        {
            var tenant = entry.Entity;

            if (tenant.Slug != TenantConstants.FundationalSlug)
            {
                continue;
            }

            if (entry.State == EntityState.Deleted)
            {
                throw new InvalidOperationException(
                    $"Cannot hard-delete the fundational tenant '{TenantConstants.FundationalSlug}'.");
            }

            var isDeletedProp = entry.Property(nameof(Tenant.IsDeleted));
            if (isDeletedProp.IsModified && isDeletedProp.CurrentValue is true)
            {
                throw new InvalidOperationException(
                    $"Cannot soft-delete the fundational tenant '{TenantConstants.FundationalSlug}'.");
            }

            var statusProp = entry.Property(nameof(Tenant.Status));
            if (statusProp.IsModified)
            {
                var newStatus = (TenantStatus)statusProp.CurrentValue!;
                if (newStatus is TenantStatus.Suspended or TenantStatus.Cancelled)
                {
                    throw new InvalidOperationException(
                        $"Cannot set status '{newStatus}' on the fundational tenant '{TenantConstants.FundationalSlug}'.");
                }
            }
        }
    }
}
