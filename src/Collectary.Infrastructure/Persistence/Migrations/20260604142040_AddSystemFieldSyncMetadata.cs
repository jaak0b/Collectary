using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectary.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemFieldSyncMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BaseRevision",
                table: "SystemFields",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SystemFields",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SystemFields",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDirty",
                table: "SystemFields",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByUserId",
                table: "SystemFields",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Revision",
                table: "SystemFields",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "SystemFields",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseRevision",
                table: "SystemFields");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SystemFields");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SystemFields");

            migrationBuilder.DropColumn(
                name: "IsDirty",
                table: "SystemFields");

            migrationBuilder.DropColumn(
                name: "LastModifiedByUserId",
                table: "SystemFields");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "SystemFields");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SystemFields");
        }
    }
}
