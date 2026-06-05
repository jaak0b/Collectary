using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectary.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFileAttachmentFieldType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileAttachmentFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileAttachmentFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileAttachmentFieldDefinitions_FieldDefinitions_Id",
                        column: x => x.Id,
                        principalTable: "FieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileAttachmentFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileAttachmentFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileAttachmentFieldValues_FieldValues_Id",
                        column: x => x.Id,
                        principalTable: "FieldValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileAttachmentEntries",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerValueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileAttachmentEntries", x => new { x.OwnerValueId, x.Key });
                    table.ForeignKey(
                        name: "FK_FileAttachmentEntries_FileAttachmentFieldValues_OwnerValueId",
                        column: x => x.OwnerValueId,
                        principalTable: "FileAttachmentFieldValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileAttachmentEntries");

            migrationBuilder.DropTable(
                name: "FileAttachmentFieldDefinitions");

            migrationBuilder.DropTable(
                name: "FileAttachmentFieldValues");
        }
    }
}
