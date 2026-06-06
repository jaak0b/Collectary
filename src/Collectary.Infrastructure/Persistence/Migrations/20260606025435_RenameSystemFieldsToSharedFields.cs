using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectary.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameSystemFieldsToSharedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(name: "SystemFields", newName: "SharedFields");
            migrationBuilder.RenameTable(name: "PresetSystemFields", newName: "PresetSharedFields");
            migrationBuilder.RenameTable(name: "ListSystemFields", newName: "ListSharedFields");

            migrationBuilder.RenameColumn(name: "SystemFieldId", table: "FieldDefinitions", newName: "SharedFieldId");
            migrationBuilder.RenameColumn(name: "SystemFieldId", table: "PresetSharedFields", newName: "SharedFieldId");
            migrationBuilder.RenameColumn(name: "SystemFieldId", table: "ListSharedFields", newName: "SharedFieldId");

            migrationBuilder.RenameIndex(name: "IX_FieldDefinitions_SystemFieldId", table: "FieldDefinitions", newName: "IX_FieldDefinitions_SharedFieldId");
            migrationBuilder.RenameIndex(name: "IX_PresetSystemFields_SystemFieldId", table: "PresetSharedFields", newName: "IX_PresetSharedFields_SharedFieldId");
            migrationBuilder.RenameIndex(name: "IX_ListSystemFields_SystemFieldId", table: "ListSharedFields", newName: "IX_ListSharedFields_SharedFieldId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(name: "IX_ListSharedFields_SharedFieldId", table: "ListSharedFields", newName: "IX_ListSystemFields_SystemFieldId");
            migrationBuilder.RenameIndex(name: "IX_PresetSharedFields_SharedFieldId", table: "PresetSharedFields", newName: "IX_PresetSystemFields_SystemFieldId");
            migrationBuilder.RenameIndex(name: "IX_FieldDefinitions_SharedFieldId", table: "FieldDefinitions", newName: "IX_FieldDefinitions_SystemFieldId");

            migrationBuilder.RenameColumn(name: "SharedFieldId", table: "ListSharedFields", newName: "SystemFieldId");
            migrationBuilder.RenameColumn(name: "SharedFieldId", table: "PresetSharedFields", newName: "SystemFieldId");
            migrationBuilder.RenameColumn(name: "SharedFieldId", table: "FieldDefinitions", newName: "SystemFieldId");

            migrationBuilder.RenameTable(name: "ListSharedFields", newName: "ListSystemFields");
            migrationBuilder.RenameTable(name: "PresetSharedFields", newName: "PresetSystemFields");
            migrationBuilder.RenameTable(name: "SharedFields", newName: "SystemFields");
        }
    }
}
