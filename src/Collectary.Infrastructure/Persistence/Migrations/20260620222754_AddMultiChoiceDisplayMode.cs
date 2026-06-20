using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectary.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiChoiceDisplayMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayMode",
                table: "MultiChoiceFieldDefinitions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayMode",
                table: "MultiChoiceFieldDefinitions");
        }
    }
}
