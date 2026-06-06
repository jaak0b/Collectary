using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectary.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProgressAndSliderFieldTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            DeleteFieldsOfType(migrationBuilder, "ProgressFieldDefinitions");
            DeleteFieldsOfType(migrationBuilder, "SliderFieldDefinitions");

            migrationBuilder.DropTable(
                name: "ProgressFieldDefinitions");

            migrationBuilder.DropTable(
                name: "ProgressFieldValues");

            migrationBuilder.DropTable(
                name: "SliderFieldDefinitions");

            migrationBuilder.DropTable(
                name: "SliderFieldValues");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProgressFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShowInList = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgressFieldDefinitions_FieldDefinitions_Id",
                        column: x => x.Id,
                        principalTable: "FieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgressFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Have = table.Column<int>(type: "INTEGER", nullable: true),
                    Total = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgressFieldValues_FieldValues_Id",
                        column: x => x.Id,
                        principalTable: "FieldValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SliderFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShowInList = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SliderFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SliderFieldDefinitions_FieldDefinitions_Id",
                        column: x => x.Id,
                        principalTable: "FieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SliderFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SliderFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SliderFieldValues_FieldValues_Id",
                        column: x => x.Id,
                        principalTable: "FieldValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        private void DeleteFieldsOfType(MigrationBuilder migrationBuilder, string subtypeTable)
        {
            migrationBuilder.Sql(
                $"DELETE FROM FieldValues WHERE FieldDefinitionId IN (SELECT Id FROM {subtypeTable});");
            migrationBuilder.Sql(
                $"DELETE FROM PresetSharedFields WHERE SharedFieldId IN (SELECT SharedFieldId FROM FieldDefinitions WHERE SharedFieldId IS NOT NULL AND Id IN (SELECT Id FROM {subtypeTable}));");
            migrationBuilder.Sql(
                $"DELETE FROM ListSharedFields WHERE SharedFieldId IN (SELECT SharedFieldId FROM FieldDefinitions WHERE SharedFieldId IS NOT NULL AND Id IN (SELECT Id FROM {subtypeTable}));");
            migrationBuilder.Sql(
                $"DELETE FROM SharedFields WHERE Id IN (SELECT SharedFieldId FROM FieldDefinitions WHERE SharedFieldId IS NOT NULL AND Id IN (SELECT Id FROM {subtypeTable}));");
            migrationBuilder.Sql(
                $"DELETE FROM FieldDefinitions WHERE Id IN (SELECT Id FROM {subtypeTable});");
        }
    }
}
