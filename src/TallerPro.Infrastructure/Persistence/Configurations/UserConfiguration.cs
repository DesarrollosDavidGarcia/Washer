using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TallerPro.Domain.Auth;

namespace TallerPro.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "auth", t =>
        {
            t.HasCheckConstraint(
                "CK_Users_CreatedByExclusive",
                "[CreatedByUserId] IS NULL OR [CreatedByPlatformId] IS NULL");
            t.HasCheckConstraint(
                "CK_Users_UpdatedByExclusive",
                "[UpdatedByUserId] IS NULL OR [UpdatedByPlatformId] IS NULL");
            t.HasCheckConstraint(
                "CK_Users_EmailLowercase",
                "[Email] = LOWER([Email])");
        });

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .UseIdentityColumn();

        // R-8: CI_AI collation for case-insensitive email matching
        builder.Property(u => u.Email)
            .IsRequired()
            .HasColumnType("varchar(254)")
            .UseCollation("Latin1_General_100_CI_AI_SC_UTF8");

        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasColumnType("nvarchar(120)");

        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2(7)");

        builder.Property(u => u.UpdatedAt)
            .IsRequired()
            .HasColumnType("datetime2(7)");

        builder.Property(u => u.CreatedByUserId)
            .HasColumnType("bigint");

        builder.Property(u => u.CreatedByPlatformId)
            .HasColumnType("bigint");

        builder.Property(u => u.UpdatedByUserId)
            .HasColumnType("bigint");

        builder.Property(u => u.UpdatedByPlatformId)
            .HasColumnType("bigint");

        builder.Property(u => u.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.DeletedAt)
            .HasColumnType("datetime2(7)");

        builder.Property(u => u.RowVersion)
            .IsRowVersion();

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UQ_Users_Email");

        // Self-referential audit FK (Users created by Users) — must defer to avoid cycle issues
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(u => u.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(u => u.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Audit FKs to auth.PlatformAdmins
        builder.HasOne<PlatformAdmin>()
            .WithMany()
            .HasForeignKey(u => u.CreatedByPlatformId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PlatformAdmin>()
            .WithMany()
            .HasForeignKey(u => u.UpdatedByPlatformId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}
