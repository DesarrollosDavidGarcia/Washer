using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TallerPro.Domain.Auth;
using TallerPro.Domain.Tenants;
using TallerPro.Security;

namespace TallerPro.Infrastructure.Persistence.Configurations;

internal sealed class UserBranchAccessConfiguration : IEntityTypeConfiguration<UserBranchAccess>
{
    private readonly ITenantContext _tenantContext;

    public UserBranchAccessConfiguration(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public void Configure(EntityTypeBuilder<UserBranchAccess> builder)
    {
        builder.ToTable("UserBranchAccesses", "auth", t =>
        {
            t.HasCheckConstraint(
                "CK_UBA_CreatedByExclusive",
                "[CreatedByUserId] IS NULL OR [CreatedByPlatformId] IS NULL");
            t.HasCheckConstraint(
                "CK_UBA_UpdatedByExclusive",
                "[UpdatedByUserId] IS NULL OR [UpdatedByPlatformId] IS NULL");
            t.HasCheckConstraint(
                "CK_UBA_RoleValid",
                "[Role] IN (0, 1, 2)");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .UseIdentityColumn();

        builder.Property(a => a.UserId)
            .IsRequired()
            .HasColumnType("bigint");

        builder.Property(a => a.BranchId)
            .IsRequired()
            .HasColumnType("bigint");

        builder.Property(a => a.TenantId)
            .IsRequired()
            .HasColumnType("bigint");

        builder.Property(a => a.Role)
            .IsRequired()
            .HasColumnType("tinyint");

        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2(7)");

        builder.Property(a => a.UpdatedAt)
            .IsRequired()
            .HasColumnType("datetime2(7)");

        builder.Property(a => a.CreatedByUserId)
            .HasColumnType("bigint");

        builder.Property(a => a.CreatedByPlatformId)
            .HasColumnType("bigint");

        builder.Property(a => a.UpdatedByUserId)
            .HasColumnType("bigint");

        builder.Property(a => a.UpdatedByPlatformId)
            .HasColumnType("bigint");

        builder.Property(a => a.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.DeletedAt)
            .HasColumnType("datetime2(7)");

        builder.Property(a => a.RowVersion)
            .IsRowVersion();

        builder.HasIndex(a => new { a.UserId, a.BranchId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UQ_UBA_User_Branch");

        builder.HasIndex(a => a.TenantId)
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_UBA_TenantId");

        builder.HasIndex(a => a.UserId)
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_UBA_UserId");

        builder.HasIndex(a => a.BranchId)
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_UBA_BranchId");

        // FK to auth.Users
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .HasConstraintName("FK_UBA_Users")
            .OnDelete(DeleteBehavior.Restrict);

        // FK to core.Branches
        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(a => a.BranchId)
            .HasConstraintName("FK_UBA_Branches")
            .OnDelete(DeleteBehavior.Restrict);

        // FK to core.Tenants (denormalized — D-03)
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .HasConstraintName("FK_UBA_Tenants")
            .OnDelete(DeleteBehavior.Restrict);

        // Audit FKs to auth.Users
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Audit FKs to auth.PlatformAdmins
        builder.HasOne<PlatformAdmin>()
            .WithMany()
            .HasForeignKey(a => a.CreatedByPlatformId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PlatformAdmin>()
            .WithMany()
            .HasForeignKey(a => a.UpdatedByPlatformId)
            .OnDelete(DeleteBehavior.Restrict);

        // Global filters: soft delete + tenant scoping (D-03)
        // _tenantContext.CurrentTenantId is evaluated per-query; throws MissingTenantContextException when unresolved (D-01)
        builder.HasQueryFilter(a => !a.IsDeleted && a.TenantId == _tenantContext.CurrentTenantId);
    }
}
