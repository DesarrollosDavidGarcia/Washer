using Microsoft.EntityFrameworkCore;
using TallerPro.Domain.Auth;
using TallerPro.Domain.Tenants;
using TallerPro.Infrastructure.Persistence.Configurations;
using TallerPro.Security;

namespace TallerPro.Infrastructure.Persistence;

/// <summary>Primary EF Core DbContext for TallerPro.</summary>
public sealed class TallerProDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;

    public TallerProDbContext(
        DbContextOptions<TallerProDbContext> options,
        ITenantContext tenantContext,
        ICurrentUser currentUser) : base(options)
    {
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<User> Users => Set<User>();
    public DbSet<PlatformAdmin> PlatformAdmins => Set<PlatformAdmin>();
    public DbSet<UserBranchAccess> UserBranchAccesses => Set<UserBranchAccess>();

    internal ITenantContext TenantContext => _tenantContext;
    internal ICurrentUser CurrentUser => _currentUser;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("core");

        modelBuilder.ApplyConfiguration(new TenantConfiguration());
        modelBuilder.ApplyConfiguration(new BranchConfiguration(_tenantContext));
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new PlatformAdminConfiguration());
        modelBuilder.ApplyConfiguration(new UserBranchAccessConfiguration(_tenantContext));
    }
}
