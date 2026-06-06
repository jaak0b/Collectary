using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collectary.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemovePasswordsAndEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserCredentials");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserCredentials",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Algorithm = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Iterations = table.Column<int>(type: "INTEGER", nullable: false),
                    Salt = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCredentials", x => x.UserId);
                });
        }
    }
}
