using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1707 // EF generates class names from migration names that start with digits

namespace TallerPro.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _0001_CoreDataModel : Migration
{
    private static readonly string[] BranchesTenantIdCode = ["TenantId", "Code"];
    private static readonly string[] UbaUserBranch = ["UserId", "BranchId"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "core");

        migrationBuilder.EnsureSchema(
            name: "auth");

        migrationBuilder.CreateTable(
            name: "Branches",
            schema: "core",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                TenantId = table.Column<long>(type: "bigint", nullable: false),
                Name = table.Column<string>(type: "nvarchar(120)", nullable: false),
                Code = table.Column<string>(type: "varchar(20)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                CreatedByPlatformId = table.Column<long>(type: "bigint", nullable: true),
                UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                UpdatedByPlatformId = table.Column<long>(type: "bigint", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Branches", x => x.Id);
                table.CheckConstraint("CK_Branches_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9-]%'");
                table.CheckConstraint("CK_Branches_CreatedByExclusive", "[CreatedByUserId] IS NULL OR [CreatedByPlatformId] IS NULL");
                table.CheckConstraint("CK_Branches_UpdatedByExclusive", "[UpdatedByUserId] IS NULL OR [UpdatedByPlatformId] IS NULL");
            });

        migrationBuilder.CreateTable(
            name: "PlatformAdmins",
            schema: "auth",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Email = table.Column<string>(type: "varchar(254)", nullable: false, collation: "Latin1_General_100_CI_AI_SC_UTF8"),
                DisplayName = table.Column<string>(type: "nvarchar(120)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                CreatedByPlatformId = table.Column<long>(type: "bigint", nullable: true),
                UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                UpdatedByPlatformId = table.Column<long>(type: "bigint", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PlatformAdmins", x => x.Id);
                table.CheckConstraint("CK_PlatformAdmins_CreatedByExclusive", "[CreatedByUserId] IS NULL OR [CreatedByPlatformId] IS NULL");
                table.CheckConstraint("CK_PlatformAdmins_EmailLowercase", "[Email] = LOWER([Email])");
                table.CheckConstraint("CK_PlatformAdmins_UpdatedByExclusive", "[UpdatedByUserId] IS NULL OR [UpdatedByPlatformId] IS NULL");
                table.ForeignKey(
                    name: "FK_PlatformAdmins_PlatformAdmins_CreatedByPlatformId",
                    column: x => x.CreatedByPlatformId,
                    principalSchema: "auth",
                    principalTable: "PlatformAdmins",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PlatformAdmins_PlatformAdmins_UpdatedByPlatformId",
                    column: x => x.UpdatedByPlatformId,
                    principalSchema: "auth",
                    principalTable: "PlatformAdmins",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            schema: "auth",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Email = table.Column<string>(type: "varchar(254)", nullable: false, collation: "Latin1_General_100_CI_AI_SC_UTF8"),
                DisplayName = table.Column<string>(type: "nvarchar(120)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                CreatedByPlatformId = table.Column<long>(type: "bigint", nullable: true),
                UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                UpdatedByPlatformId = table.Column<long>(type: "bigint", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
                table.CheckConstraint("CK_Users_CreatedByExclusive", "[CreatedByUserId] IS NULL OR [CreatedByPlatformId] IS NULL");
                table.CheckConstraint("CK_Users_EmailLowercase", "[Email] = LOWER([Email])");
                table.CheckConstraint("CK_Users_UpdatedByExclusive", "[UpdatedByUserId] IS NULL OR [UpdatedByPlatformId] IS NULL");
                table.ForeignKey(
                    name: "FK_Users_PlatformAdmins_CreatedByPlatformId",
                    column: x => x.CreatedByPlatformId,
                    principalSchema: "auth",
                    principalTable: "PlatformAdmins",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Users_PlatformAdmins_UpdatedByPlatformId",
                    column: x => x.UpdatedByPlatformId,
                    principalSchema: "auth",
                    principalTable: "PlatformAdmins",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Users_Users_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalSchema: "auth",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Users_Users_UpdatedByUserId",
                    column: x => x.UpdatedByUserId,
                    principalSchema: "auth",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Tenants",
            schema: "core",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(120)", nullable: false),
                Slug = table.Column<string>(type: "varchar(40)", nullable: false),
                Status = table.Column<byte>(type: "tinyint", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                CreatedByPlatformId = table.Column<long>(type: "bigint", nullable: true),
                UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                UpdatedByPlatformId = table.Column<long>(type: "bigint", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tenants", x => x.Id);
                table.CheckConstraint("CK_Tenants_CreatedByExclusive", "[CreatedByUserId] IS NULL OR [CreatedByPlatformId] IS NULL");
                table.CheckConstraint("CK_Tenants_SlugLowercase", "[Slug] = LOWER([Slug])");
                table.CheckConstraint("CK_Tenants_UpdatedByExclusive", "[UpdatedByUserId] IS NULL OR [UpdatedByPlatformId] IS NULL");
                table.ForeignKey(
                    name: "FK_Tenants_PlatformAdmins_CreatedByPlatformId",
                    column: x => x.CreatedByPlatformId,
                    principalSchema: "auth",
                    principalTable: "PlatformAdmins",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Tenants_PlatformAdmins_UpdatedByPlatformId",
                    column: x => x.UpdatedByPlatformId,
                    principalSchema: "auth",
                    principalTable: "PlatformAdmins",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Tenants_Users_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalSchema: "auth",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Tenants_Users_UpdatedByUserId",
                    column: x => x.UpdatedByUserId,
                    principalSchema: "auth",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "UserBranchAccesses",
            schema: "auth",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<long>(type: "bigint", nullable: false),
                BranchId = table.Column<long>(type: "bigint", nullable: false),
                TenantId = table.Column<long>(type: "bigint", nullable: false),
                Role = table.Column<byte>(type: "tinyint", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                CreatedByPlatformId = table.Column<long>(type: "bigint", nullable: true),
                UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                UpdatedByPlatformId = table.Column<long>(type: "bigint", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserBranchAccesses", x => x.Id);
                table.CheckConstraint("CK_UBA_CreatedByExclusive", "[CreatedByUserId] IS NULL OR [CreatedByPlatformId] IS NULL");
                table.CheckConstraint("CK_UBA_RoleValid", "[Role] IN (0, 1, 2)");
                table.CheckConstraint("CK_UBA_UpdatedByExclusive", "[UpdatedByUserId] IS NULL OR [UpdatedByPlatformId] IS NULL");
                table.ForeignKey(
                    name: "FK_UBA_Branches",
                    column: x => x.BranchId,
                    principalSchema: "core",
                    principalTable: "Branches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_UBA_Tenants",
                    column: x => x.TenantId,
                    principalSchema: "core",
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_UBA_Users",
                    column: x => x.UserId,
                    principalSchema: "auth",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_UserBranchAccesses_PlatformAdmins_CreatedByPlatformId",
                    column: x => x.CreatedByPlatformId,
                    principalSchema: "auth",
                    principalTable: "PlatformAdmins",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_UserBranchAccesses_PlatformAdmins_UpdatedByPlatformId",
                    column: x => x.UpdatedByPlatformId,
                    principalSchema: "auth",
                    principalTable: "PlatformAdmins",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_UserBranchAccesses_Users_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalSchema: "auth",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_UserBranchAccesses_Users_UpdatedByUserId",
                    column: x => x.UpdatedByUserId,
                    principalSchema: "auth",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Branches_CreatedByPlatformId",
            schema: "core",
            table: "Branches",
            column: "CreatedByPlatformId");

        migrationBuilder.CreateIndex(
            name: "IX_Branches_CreatedByUserId",
            schema: "core",
            table: "Branches",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_Branches_TenantId",
            schema: "core",
            table: "Branches",
            column: "TenantId",
            filter: "[IsDeleted] = 0");

        migrationBuilder.CreateIndex(
            name: "IX_Branches_UpdatedByPlatformId",
            schema: "core",
            table: "Branches",
            column: "UpdatedByPlatformId");

        migrationBuilder.CreateIndex(
            name: "IX_Branches_UpdatedByUserId",
            schema: "core",
            table: "Branches",
            column: "UpdatedByUserId");

        migrationBuilder.CreateIndex(
            name: "UQ_Branches_TenantId_Code",
            schema: "core",
            table: "Branches",
            columns: BranchesTenantIdCode,
            unique: true,
            filter: "[IsDeleted] = 0");

        migrationBuilder.CreateIndex(
            name: "IX_PlatformAdmins_CreatedByPlatformId",
            schema: "auth",
            table: "PlatformAdmins",
            column: "CreatedByPlatformId");

        migrationBuilder.CreateIndex(
            name: "IX_PlatformAdmins_CreatedByUserId",
            schema: "auth",
            table: "PlatformAdmins",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_PlatformAdmins_UpdatedByPlatformId",
            schema: "auth",
            table: "PlatformAdmins",
            column: "UpdatedByPlatformId");

        migrationBuilder.CreateIndex(
            name: "IX_PlatformAdmins_UpdatedByUserId",
            schema: "auth",
            table: "PlatformAdmins",
            column: "UpdatedByUserId");

        migrationBuilder.CreateIndex(
            name: "UQ_PlatformAdmins_Email",
            schema: "auth",
            table: "PlatformAdmins",
            column: "Email",
            unique: true,
            filter: "[IsDeleted] = 0");

        migrationBuilder.CreateIndex(
            name: "IX_Tenants_CreatedByPlatformId",
            schema: "core",
            table: "Tenants",
            column: "CreatedByPlatformId");

        migrationBuilder.CreateIndex(
            name: "IX_Tenants_CreatedByUserId",
            schema: "core",
            table: "Tenants",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_Tenants_Status",
            schema: "core",
            table: "Tenants",
            column: "Status",
            filter: "[IsDeleted] = 0");

        migrationBuilder.CreateIndex(
            name: "IX_Tenants_UpdatedByPlatformId",
            schema: "core",
            table: "Tenants",
            column: "UpdatedByPlatformId");

        migrationBuilder.CreateIndex(
            name: "IX_Tenants_UpdatedByUserId",
            schema: "core",
            table: "Tenants",
            column: "UpdatedByUserId");

        migrationBuilder.CreateIndex(
            name: "UQ_Tenants_Slug",
            schema: "core",
            table: "Tenants",
            column: "Slug",
            unique: true,
            filter: "[IsDeleted] = 0");

        migrationBuilder.CreateIndex(
            name: "IX_UBA_BranchId",
            schema: "auth",
            table: "UserBranchAccesses",
            column: "BranchId",
            filter: "[IsDeleted] = 0");

        migrationBuilder.CreateIndex(
            name: "IX_UBA_TenantId",
            schema: "auth",
            table: "UserBranchAccesses",
            column: "TenantId",
            filter: "[IsDeleted] = 0");

        migrationBuilder.CreateIndex(
            name: "IX_UBA_UserId",
            schema: "auth",
            table: "UserBranchAccesses",
            column: "UserId",
            filter: "[IsDeleted] = 0");

        migrationBuilder.CreateIndex(
            name: "IX_UserBranchAccesses_CreatedByPlatformId",
            schema: "auth",
            table: "UserBranchAccesses",
            column: "CreatedByPlatformId");

        migrationBuilder.CreateIndex(
            name: "IX_UserBranchAccesses_CreatedByUserId",
            schema: "auth",
            table: "UserBranchAccesses",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_UserBranchAccesses_UpdatedByPlatformId",
            schema: "auth",
            table: "UserBranchAccesses",
            column: "UpdatedByPlatformId");

        migrationBuilder.CreateIndex(
            name: "IX_UserBranchAccesses_UpdatedByUserId",
            schema: "auth",
            table: "UserBranchAccesses",
            column: "UpdatedByUserId");

        migrationBuilder.CreateIndex(
            name: "UQ_UBA_User_Branch",
            schema: "auth",
            table: "UserBranchAccesses",
            columns: UbaUserBranch,
            unique: true,
            filter: "[IsDeleted] = 0");

        migrationBuilder.CreateIndex(
            name: "IX_Users_CreatedByPlatformId",
            schema: "auth",
            table: "Users",
            column: "CreatedByPlatformId");

        migrationBuilder.CreateIndex(
            name: "IX_Users_CreatedByUserId",
            schema: "auth",
            table: "Users",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_Users_UpdatedByPlatformId",
            schema: "auth",
            table: "Users",
            column: "UpdatedByPlatformId");

        migrationBuilder.CreateIndex(
            name: "IX_Users_UpdatedByUserId",
            schema: "auth",
            table: "Users",
            column: "UpdatedByUserId");

        migrationBuilder.CreateIndex(
            name: "UQ_Users_Email",
            schema: "auth",
            table: "Users",
            column: "Email",
            unique: true,
            filter: "[IsDeleted] = 0");

        migrationBuilder.AddForeignKey(
            name: "FK_Branches_PlatformAdmins_CreatedByPlatformId",
            schema: "core",
            table: "Branches",
            column: "CreatedByPlatformId",
            principalSchema: "auth",
            principalTable: "PlatformAdmins",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Branches_PlatformAdmins_UpdatedByPlatformId",
            schema: "core",
            table: "Branches",
            column: "UpdatedByPlatformId",
            principalSchema: "auth",
            principalTable: "PlatformAdmins",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Branches_Tenants",
            schema: "core",
            table: "Branches",
            column: "TenantId",
            principalSchema: "core",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Branches_Users_CreatedByUserId",
            schema: "core",
            table: "Branches",
            column: "CreatedByUserId",
            principalSchema: "auth",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Branches_Users_UpdatedByUserId",
            schema: "core",
            table: "Branches",
            column: "UpdatedByUserId",
            principalSchema: "auth",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_PlatformAdmins_Users_CreatedByUserId",
            schema: "auth",
            table: "PlatformAdmins",
            column: "CreatedByUserId",
            principalSchema: "auth",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_PlatformAdmins_Users_UpdatedByUserId",
            schema: "auth",
            table: "PlatformAdmins",
            column: "UpdatedByUserId",
            principalSchema: "auth",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        // D-03 defense-in-depth: trigger enforces TenantId == Branch.TenantId on UserBranchAccesses.
        // EF Core does not generate triggers automatically; must be added manually.
        migrationBuilder.Sql(@"
            CREATE TRIGGER [auth].[TR_UBA_TenantConsistency]
            ON [auth].[UserBranchAccesses]
            AFTER INSERT, UPDATE
            AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (
                    SELECT 1
                    FROM inserted i
                    INNER JOIN [core].[Branches] b ON b.Id = i.BranchId
                    WHERE i.TenantId <> b.TenantId
                )
                BEGIN
                    THROW 50001, 'UserBranchAccess.TenantId must equal Branch.TenantId', 1;
                END
            END;
        ");

        // R-1: seed foundational tenant via SQL so IDENTITY assigns Id; canonical key is Slug.
        // FundationalTenantGuard identifies this tenant by Slug='tallerpro-platform', never by Id.
        migrationBuilder.Sql(@"
            INSERT INTO [core].[Tenants] (Name, Slug, Status, CreatedAt, UpdatedAt, IsDeleted)
            VALUES ('TallerPro Platform', 'tallerpro-platform', 0, SYSUTCDATETIME(), SYSUTCDATETIME(), 0);
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS [auth].[TR_UBA_TenantConsistency];");

        migrationBuilder.Sql("DELETE FROM [core].[Tenants] WHERE Slug = 'tallerpro-platform';");

        migrationBuilder.DropForeignKey(
            name: "FK_Users_PlatformAdmins_CreatedByPlatformId",
            schema: "auth",
            table: "Users");

        migrationBuilder.DropForeignKey(
            name: "FK_Users_PlatformAdmins_UpdatedByPlatformId",
            schema: "auth",
            table: "Users");

        migrationBuilder.DropTable(
            name: "UserBranchAccesses",
            schema: "auth");

        migrationBuilder.DropTable(
            name: "Branches",
            schema: "core");

        migrationBuilder.DropTable(
            name: "Tenants",
            schema: "core");

        migrationBuilder.DropTable(
            name: "PlatformAdmins",
            schema: "auth");

        migrationBuilder.DropTable(
            name: "Users",
            schema: "auth");
    }
}

#pragma warning restore CA1707
