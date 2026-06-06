using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectary.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConfigurableFieldTypeSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Max",
                table: "DateFieldDefinitions");

            migrationBuilder.DropColumn(
                name: "Min",
                table: "DateFieldDefinitions");

            migrationBuilder.AddColumn<bool>(
                name: "ThreeState",
                table: "BoolFieldDefinitions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThreeState",
                table: "BoolFieldDefinitions");

            migrationBuilder.AddColumn<DateTime>(
                name: "Max",
                table: "DateFieldDefinitions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Min",
                table: "DateFieldDefinitions",
                type: "TEXT",
                nullable: true);
        }
    }
}
