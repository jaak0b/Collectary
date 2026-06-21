using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectary.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MultiImageStoreFileNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MultiImagePictures",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerValueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiImagePictures", x => new { x.OwnerValueId, x.Key });
                    table.ForeignKey(
                        name: "FK_MultiImagePictures_MultiImageFieldValues_OwnerValueId",
                        column: x => x.OwnerValueId,
                        principalTable: "MultiImageFieldValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                WITH RECURSIVE split(Id, remainder, key) AS (
                    SELECT Id, ImageKeys || char(10), ''
                    FROM MultiImageFieldValues
                    WHERE ImageKeys IS NOT NULL AND ImageKeys <> ''
                    UNION ALL
                    SELECT Id,
                           substr(remainder, instr(remainder, char(10)) + 1),
                           substr(remainder, 1, instr(remainder, char(10)) - 1)
                    FROM split
                    WHERE remainder <> ''
                )
                INSERT INTO MultiImagePictures (OwnerValueId, Key, FileName)
                SELECT Id, key,
                       CASE WHEN instr(key, '_') > 0 AND instr(key, '_') < length(key)
                            THEN substr(key, instr(key, '_') + 1)
                            ELSE key END
                FROM split
                WHERE key <> '';
                """);

            migrationBuilder.DropColumn(
                name: "ImageKeys",
                table: "MultiImageFieldValues");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageKeys",
                table: "MultiImageFieldValues",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE MultiImageFieldValues
                SET ImageKeys = COALESCE(
                    (SELECT group_concat(Key, char(10))
                     FROM MultiImagePictures
                     WHERE MultiImagePictures.OwnerValueId = MultiImageFieldValues.Id), '');
                """);

            migrationBuilder.DropTable(
                name: "MultiImagePictures");
        }
    }
}
