using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectary.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SystemFieldId",
                table: "FieldDefinitions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SystemFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemFields", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ListSystemFields",
                columns: table => new
                {
                    ListFieldDefinitionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SystemFieldId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListSystemFields", x => new { x.ListFieldDefinitionId, x.SystemFieldId });
                    table.ForeignKey(
                        name: "FK_ListSystemFields_ListFieldDefinitions_ListFieldDefinitionId",
                        column: x => x.ListFieldDefinitionId,
                        principalTable: "ListFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ListSystemFields_SystemFields_SystemFieldId",
                        column: x => x.SystemFieldId,
                        principalTable: "SystemFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PresetSystemFields",
                columns: table => new
                {
                    PresetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SystemFieldId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresetSystemFields", x => new { x.PresetId, x.SystemFieldId });
                    table.ForeignKey(
                        name: "FK_PresetSystemFields_Presets_PresetId",
                        column: x => x.PresetId,
                        principalTable: "Presets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PresetSystemFields_SystemFields_SystemFieldId",
                        column: x => x.SystemFieldId,
                        principalTable: "SystemFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FieldDefinitions_SystemFieldId",
                table: "FieldDefinitions",
                column: "SystemFieldId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListSystemFields_SystemFieldId",
                table: "ListSystemFields",
                column: "SystemFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_PresetSystemFields_SystemFieldId",
                table: "PresetSystemFields",
                column: "SystemFieldId");

            migrationBuilder.AddForeignKey(
                name: "FK_FieldDefinitions_SystemFields_SystemFieldId",
                table: "FieldDefinitions",
                column: "SystemFieldId",
                principalTable: "SystemFields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FieldDefinitions_SystemFields_SystemFieldId",
                table: "FieldDefinitions");

            migrationBuilder.DropTable(
                name: "ListSystemFields");

            migrationBuilder.DropTable(
                name: "PresetSystemFields");

            migrationBuilder.DropTable(
                name: "SystemFields");

            migrationBuilder.DropIndex(
                name: "IX_FieldDefinitions_SystemFieldId",
                table: "FieldDefinitions");

            migrationBuilder.DropColumn(
                name: "SystemFieldId",
                table: "FieldDefinitions");
        }
    }
}
