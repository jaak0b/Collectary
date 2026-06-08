using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectary.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncUsersAndShares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BaseRevision",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDirty",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByUserId",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Revision",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "BaseRevision",
                table: "CollectionShares",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CollectionShares",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CollectionShares",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDirty",
                table: "CollectionShares",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByUserId",
                table: "CollectionShares",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Revision",
                table: "CollectionShares",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "CollectionShares",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql("UPDATE Users SET Revision = 1, IsDirty = 1 WHERE Revision = 0;");
            migrationBuilder.Sql("UPDATE CollectionShares SET Revision = 1, IsDirty = 1 WHERE Revision = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseRevision",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsDirty",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastModifiedByUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BaseRevision",
                table: "CollectionShares");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CollectionShares");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CollectionShares");

            migrationBuilder.DropColumn(
                name: "IsDirty",
                table: "CollectionShares");

            migrationBuilder.DropColumn(
                name: "LastModifiedByUserId",
                table: "CollectionShares");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "CollectionShares");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CollectionShares");
        }
    }
}
