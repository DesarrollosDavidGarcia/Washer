using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TallerPro.Domain.Tenants;

namespace TallerPro.Infrastructure.Persistence.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants", "core", t =>
        {
            t.HasCheckConstraint(
                "CK_Tenants_CreatedByExclusive",
                "[CreatedByUserId] IS NULL OR [CreatedByPlatformId] IS NULL");
            t.HasCheckConstraint(
                "CK_Tenants_UpdatedByExclusive",
                "[UpdatedByUserId] IS NULL OR [UpdatedByPlatformId] IS NULL");
            t.HasCheckConstraint(
                "CK_Tenants_SlugLowercase",
                "[Slug] = LOWER([Slug])");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .UseIdentityColumn();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasColumnType("nvarchar(120)");

        builder.Property(t => t.Slug)
            .IsRequired()
            .HasColumnType("varchar(40)");

        builder.Property(t => t.Status)
            .IsRequired()
            .HasColumnType("tinyint");

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2(7)");

        builder.Property(t => t.UpdatedAt)
            .IsRequired()
            .HasColumnType("datetime2(7)");

        builder.Property(t => t.CreatedByUserId)
            .HasColumnType("bigint");

        builder.Property(t => t.CreatedByPlatformId)
            .HasColumnType("bigint");

        builder.Property(t => t.UpdatedByUserId)
            .HasColumnType("bigint");

        builder.Property(t => t.UpdatedByPlatformId)
            .HasColumnType("bigint");

        builder.Property(t => t.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.DeletedAt)
            .HasColumnType("datetime2(7)");

        builder.Property(t => t.RowVersion)
            .IsRowVersion();

        builder.HasIndex(t => t.Slug)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UQ_Tenants_Slug");

        builder.HasIndex(t => t.Status)
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Tenants_Status");

        // Audit FK to auth.Users
        builder.HasOne<Domain.Auth.User>()
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Auth.User>()
            .WithMany()
            .HasForeignKey(t => t.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Audit FK to auth.PlatformAdmins
        builder.HasOne<Domain.Auth.PlatformAdmin>()
            .WithMany()
            .HasForeignKey(t => t.CreatedByPlatformId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Auth.PlatformAdmin>()
            .WithMany()
            .HasForeignKey(t => t.UpdatedByPlatformId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
