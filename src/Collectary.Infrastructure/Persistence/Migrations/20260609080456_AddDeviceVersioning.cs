using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectary.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Lamport",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByDeviceId",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<long>(
                name: "Lamport",
                table: "SharedFields",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByDeviceId",
                table: "SharedFields",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<long>(
                name: "Lamport",
                table: "Presets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByDeviceId",
                table: "Presets",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<long>(
                name: "Lamport",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByDeviceId",
                table: "Items",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<long>(
                name: "Lamport",
                table: "CollectionShares",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedByDeviceId",
                table: "CollectionShares",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "SyncState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxObservedLamport = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncState", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tombstones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tombstones", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncState");

            migrationBuilder.DropTable(
                name: "Tombstones");

            migrationBuilder.DropColumn(
                name: "Lamport",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastModifiedByDeviceId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Lamport",
                table: "SharedFields");

            migrationBuilder.DropColumn(
                name: "LastModifiedByDeviceId",
                table: "SharedFields");

            migrationBuilder.DropColumn(
                name: "Lamport",
                table: "Presets");

            migrationBuilder.DropColumn(
                name: "LastModifiedByDeviceId",
                table: "Presets");

            migrationBuilder.DropColumn(
                name: "Lamport",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "LastModifiedByDeviceId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Lamport",
                table: "CollectionShares");

            migrationBuilder.DropColumn(
                name: "LastModifiedByDeviceId",
                table: "CollectionShares");
        }
    }
}
