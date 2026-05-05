using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TallerPro.Domain.Tenants;
using TallerPro.Security;

namespace TallerPro.Infrastructure.Persistence.Configurations;

internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    private readonly ITenantContext _tenantContext;

    public BranchConfiguration(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches", "core", t =>
        {
            t.HasCheckConstraint(
                "CK_Branches_CreatedByExclusive",
                "[CreatedByUserId] IS NULL OR [CreatedByPlatformId] IS NULL");
            t.HasCheckConstraint(
                "CK_Branches_UpdatedByExclusive",
                "[UpdatedByUserId] IS NULL OR [UpdatedByPlatformId] IS NULL");
            t.HasCheckConstraint(
                "CK_Branches_CodeFormat",
                "[Code] NOT LIKE '%[^A-Z0-9-]%'");
        });

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .UseIdentityColumn();

        builder.Property(b => b.TenantId)
            .IsRequired()
            .HasColumnType("bigint");

        builder.Property(b => b.Name)
            .IsRequired()
            .HasColumnType("nvarchar(120)");

        builder.Property(b => b.Code)
            .IsRequired()
            .HasColumnType("varchar(20)");

        builder.Property(b => b.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2(7)");

        builder.Property(b => b.UpdatedAt)
            .IsRequired()
            .HasColumnType("datetime2(7)");

        builder.Property(b => b.CreatedByUserId)
            .HasColumnType("bigint");

        builder.Property(b => b.CreatedByPlatformId)
            .HasColumnType("bigint");

        builder.Property(b => b.UpdatedByUserId)
            .HasColumnType("bigint");

        builder.Property(b => b.UpdatedByPlatformId)
            .HasColumnType("bigint");

        builder.Property(b => b.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(b => b.DeletedAt)
            .HasColumnType("datetime2(7)");

        builder.Property(b => b.RowVersion)
            .IsRowVersion();

        builder.HasIndex(b => new { b.TenantId, b.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UQ_Branches_TenantId_Code");

        builder.HasIndex(b => b.TenantId)
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Branches_TenantId");

        // FK to core.Tenants
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(b => b.TenantId)
            .HasConstraintName("FK_Branches_Tenants")
            .OnDelete(DeleteBehavior.Restrict);

        // Audit FKs to auth.Users
        builder.HasOne<Domain.Auth.User>()
            .WithMany()
            .HasForeignKey(b => b.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Auth.User>()
            .WithMany()
            .HasForeignKey(b => b.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Audit FKs to auth.PlatformAdmins
        builder.HasOne<Domain.Auth.PlatformAdmin>()
            .WithMany()
            .HasForeignKey(b => b.CreatedByPlatformId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Auth.PlatformAdmin>()
            .WithMany()
            .HasForeignKey(b => b.UpdatedByPlatformId)
            .OnDelete(DeleteBehavior.Restrict);

        // Global filters: soft delete + tenant scoping
        // _tenantContext.CurrentTenantId is evaluated per-query; throws MissingTenantContextException when unresolved (D-01)
        builder.HasQueryFilter(b => !b.IsDeleted && b.TenantId == _tenantContext.CurrentTenantId);
    }
}
