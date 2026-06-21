using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectary.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DateWithTimeAndRemoveTimeField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM FieldValues WHERE FieldDefinitionId IN (SELECT Id FROM TimeFieldDefinitions);");
            migrationBuilder.Sql(
                "DELETE FROM PresetSharedFields WHERE SharedFieldId IN (SELECT SharedFieldId FROM FieldDefinitions WHERE SharedFieldId IS NOT NULL AND Id IN (SELECT Id FROM TimeFieldDefinitions));");
            migrationBuilder.Sql(
                "DELETE FROM ListSharedFields WHERE SharedFieldId IN (SELECT SharedFieldId FROM FieldDefinitions WHERE SharedFieldId IS NOT NULL AND Id IN (SELECT Id FROM TimeFieldDefinitions));");
            migrationBuilder.Sql(
                "DELETE FROM SharedFields WHERE Id IN (SELECT SharedFieldId FROM FieldDefinitions WHERE SharedFieldId IS NOT NULL AND Id IN (SELECT Id FROM TimeFieldDefinitions));");
            migrationBuilder.Sql(
                "DELETE FROM FieldDefinitions WHERE Id IN (SELECT Id FROM TimeFieldDefinitions);");

            migrationBuilder.DropTable(
                name: "TimeFieldDefinitions");

            migrationBuilder.DropTable(
                name: "TimeFieldValues");

            migrationBuilder.AddColumn<bool>(
                name: "WithTime",
                table: "DateFieldDefinitions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WithTime",
                table: "DateFieldDefinitions");

            migrationBuilder.CreateTable(
                name: "TimeFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShowInList = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeFieldDefinitions_FieldDefinitions_Id",
                        column: x => x.Id,
                        principalTable: "FieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimeFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeFieldValues_FieldValues_Id",
                        column: x => x.Id,
                        principalTable: "FieldValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
