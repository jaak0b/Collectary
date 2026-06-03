using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectary.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "PresetSystemFields",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "ListSystemFields",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "FieldDefinitions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FieldGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PresetId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ParentListFieldDefinitionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    DisplayMode = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultCollapsed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldGroups_ListFieldDefinitions_ParentListFieldDefinitionId",
                        column: x => x.ParentListFieldDefinitionId,
                        principalTable: "ListFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FieldGroups_Presets_PresetId",
                        column: x => x.PresetId,
                        principalTable: "Presets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FieldGroups_ParentListFieldDefinitionId",
                table: "FieldGroups",
                column: "ParentListFieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldGroups_PresetId",
                table: "FieldGroups",
                column: "PresetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FieldGroups");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "PresetSystemFields");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "ListSystemFields");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "FieldDefinitions");
        }
    }
}
