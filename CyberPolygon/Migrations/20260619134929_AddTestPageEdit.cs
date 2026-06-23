using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CyberPolygon.Migrations
{
    /// <inheritdoc />
    public partial class AddTestPageEdit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowedTeamIds",
                table: "CyberTests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllowedUserIds",
                table: "CyberTests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "CyberTests",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedTeamIds",
                table: "CyberTests");

            migrationBuilder.DropColumn(
                name: "AllowedUserIds",
                table: "CyberTests");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "CyberTests");
        }
    }
}
