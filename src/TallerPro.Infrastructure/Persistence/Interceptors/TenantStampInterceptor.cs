using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TallerPro.Domain.Auth;
using TallerPro.Domain.Tenants;

namespace TallerPro.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps TenantId on new UserBranchAccess entries from their associated Branch.
/// Prefers the ChangeTracker lookup to avoid extra SQL; falls back to FindAsync if Branch is not tracked.
/// N+1 on the fallback path is intentional and documented — callers should load Branch before UBA for efficiency.
/// </summary>
internal sealed class TenantStampInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            StampTenantIds(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await StampTenantIdsAsync(eventData.Context, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void StampTenantIds(DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries<UserBranchAccess>())
        {
            if (entry.State != EntityState.Added)
            {
                continue;
            }

            var entity = entry.Entity;

            if (entity.TenantId != 0)
            {
                continue;
            }

            var branch = FindBranchInChangeTracker(context, entity.BranchId)
                ?? context.Set<Branch>().Find(entity.BranchId);

            if (branch is not null)
            {
                entry.Property(nameof(UserBranchAccess.TenantId)).CurrentValue = branch.TenantId;
            }
        }
    }

    private static async Task StampTenantIdsAsync(DbContext context, CancellationToken cancellationToken)
    {
        foreach (var entry in context.ChangeTracker.Entries<UserBranchAccess>())
        {
            if (entry.State != EntityState.Added)
            {
                continue;
            }

            var entity = entry.Entity;

            if (entity.TenantId != 0)
            {
                continue;
            }

            var branch = FindBranchInChangeTracker(context, entity.BranchId)
                ?? await context.Set<Branch>().FindAsync([entity.BranchId], cancellationToken);

            if (branch is not null)
            {
                entry.Property(nameof(UserBranchAccess.TenantId)).CurrentValue = branch.TenantId;
            }
        }
    }

    private static Branch? FindBranchInChangeTracker(DbContext context, long branchId) =>
        context.ChangeTracker
            .Entries<Branch>()
            .FirstOrDefault(e => e.Entity.Id == branchId)
            ?.Entity;
}
