using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TallerPro.Domain.Auth;

namespace TallerPro.Infrastructure.Persistence.Configurations;

internal sealed class PlatformAdminConfiguration : IEntityTypeConfiguration<PlatformAdmin>
{
    public void Configure(EntityTypeBuilder<PlatformAdmin> builder)
    {
        builder.ToTable("PlatformAdmins", "auth", t =>
        {
            t.HasCheckConstraint(
                "CK_PlatformAdmins_CreatedByExclusive",
                "[CreatedByUserId] IS NULL OR [CreatedByPlatformId] IS NULL");
            t.HasCheckConstraint(
                "CK_PlatformAdmins_UpdatedByExclusive",
                "[UpdatedByUserId] IS NULL OR [UpdatedByPlatformId] IS NULL");
            t.HasCheckConstraint(
                "CK_PlatformAdmins_EmailLowercase",
                "[Email] = LOWER([Email])");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .UseIdentityColumn();

        // R-8: CI_AI collation for case-insensitive email matching
        builder.Property(p => p.Email)
            .IsRequired()
            .HasColumnType("varchar(254)")
            .UseCollation("Latin1_General_100_CI_AI_SC_UTF8");

        builder.Property(p => p.DisplayName)
            .IsRequired()
            .HasColumnType("nvarchar(120)");

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2(7)");

        builder.Property(p => p.UpdatedAt)
            .IsRequired()
            .HasColumnType("datetime2(7)");

        builder.Property(p => p.CreatedByUserId)
            .HasColumnType("bigint");

        builder.Property(p => p.CreatedByPlatformId)
            .HasColumnType("bigint");

        builder.Property(p => p.UpdatedByUserId)
            .HasColumnType("bigint");

        builder.Property(p => p.UpdatedByPlatformId)
            .HasColumnType("bigint");

        builder.Property(p => p.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.DeletedAt)
            .HasColumnType("datetime2(7)");

        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        builder.HasIndex(p => p.Email)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UQ_PlatformAdmins_Email");

        // Audit FKs to auth.Users
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referential audit FK (PlatformAdmins created by PlatformAdmins)
        builder.HasOne<PlatformAdmin>()
            .WithMany()
            .HasForeignKey(p => p.CreatedByPlatformId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PlatformAdmin>()
            .WithMany()
            .HasForeignKey(p => p.UpdatedByPlatformId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
