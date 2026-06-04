using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectary.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountsOwnershipAndSyncMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BaseRevision",
                table: "Presets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Presets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Presets",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDirty",
                table: "Presets",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByUserId",
                table: "Presets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Presets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Revision",
                table: "Presets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Presets",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "BaseRevision",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDirty",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByUserId",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Revision",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "CollectionShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PresetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SharedWithUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GrantedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Permission = table.Column<int>(type: "INTEGER", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionShares", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserCredentials",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Hash = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Salt = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Iterations = table.Column<int>(type: "INTEGER", nullable: false),
                    Algorithm = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCredentials", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionShares_PresetId_SharedWithUserId",
                table: "CollectionShares",
                columns: new[] { "PresetId", "SharedWithUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionShares");

            migrationBuilder.DropTable(
                name: "UserCredentials");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropColumn(
                name: "BaseRevision",
                table: "Presets");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Presets");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Presets");

            migrationBuilder.DropColumn(
                name: "IsDirty",
                table: "Presets");

            migrationBuilder.DropColumn(
                name: "LastModifiedByUserId",
                table: "Presets");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Presets");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "Presets");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Presets");

            migrationBuilder.DropColumn(
                name: "BaseRevision",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "IsDirty",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "LastModifiedByUserId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "Items");
        }
    }
}
