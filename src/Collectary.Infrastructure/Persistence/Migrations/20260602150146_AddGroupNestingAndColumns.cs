using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectary.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupNestingAndColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentGroupId",
                table: "FieldGroups",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PrefixColumnHeaders",
                table: "FieldGroups",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowInList",
                table: "FieldGroups",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldGroups_ParentGroupId",
                table: "FieldGroups",
                column: "ParentGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_FieldGroups_FieldGroups_ParentGroupId",
                table: "FieldGroups",
                column: "ParentGroupId",
                principalTable: "FieldGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FieldGroups_FieldGroups_ParentGroupId",
                table: "FieldGroups");

            migrationBuilder.DropIndex(
                name: "IX_FieldGroups_ParentGroupId",
                table: "FieldGroups");

            migrationBuilder.DropColumn(
                name: "ParentGroupId",
                table: "FieldGroups");

            migrationBuilder.DropColumn(
                name: "PrefixColumnHeaders",
                table: "FieldGroups");

            migrationBuilder.DropColumn(
                name: "ShowInList",
                table: "FieldGroups");
        }
    }
}
