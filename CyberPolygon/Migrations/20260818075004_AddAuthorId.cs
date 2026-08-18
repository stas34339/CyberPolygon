using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CyberPolygon.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorId",
                table: "Scenarios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorId",
                table: "Instructions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorId",
                table: "CyberTests",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "Instructions");

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "CyberTests");
        }
    }
}
