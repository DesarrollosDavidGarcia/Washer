using Microsoft.EntityFrameworkCore;
using TallerPro.Domain.Auth;
using TallerPro.Domain.Tenants;

namespace TallerPro.Infrastructure.Persistence;

/// <summary>
/// Development-only seed that creates a representative dataset for local testing.
/// Idempotent: exits immediately if the "acme" tenant already exists.
/// </summary>
public static class DevSeeder
{
    public static async Task SeedAsync(TallerProDbContext context, CancellationToken ct = default)
    {
        var acme = await context.Tenants
            .FirstOrDefaultAsync(t => t.Slug == "acme", ct);

        if (acme is not null)
        {
            return;
        }

        acme = new Tenant("Acme Workshop", "acme");
        context.Tenants.Add(acme);
        await context.SaveChangesAsync(ct);

        var b1 = new Branch(acme.Id, "Acme Centro", "B001");
        var b2 = new Branch(acme.Id, "Acme Norte", "B002");
        context.Branches.AddRange(b1, b2);

        var owner = new User("owner@acme.test", "Acme Owner");
        var pa = new PlatformAdmin("dev@tallerpro.local", "TallerPro Dev");
        context.Users.Add(owner);
        context.PlatformAdmins.Add(pa);
        await context.SaveChangesAsync(ct);

        context.UserBranchAccesses.Add(new UserBranchAccess(owner.Id, b1.Id, acme.Id, Role.Admin));
        context.UserBranchAccesses.Add(new UserBranchAccess(owner.Id, b2.Id, acme.Id, Role.Admin));
        await context.SaveChangesAsync(ct);
    }
}
