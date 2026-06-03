using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectary.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImageFieldColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "ImageFieldValues",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayHeight",
                table: "ImageFieldDefinitions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 200);

            migrationBuilder.AddColumn<int>(
                name: "DisplayWidth",
                table: "ImageFieldDefinitions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 200);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                table: "ImageFieldValues");

            migrationBuilder.DropColumn(
                name: "DisplayHeight",
                table: "ImageFieldDefinitions");

            migrationBuilder.DropColumn(
                name: "DisplayWidth",
                table: "ImageFieldDefinitions");
        }
    }
}
