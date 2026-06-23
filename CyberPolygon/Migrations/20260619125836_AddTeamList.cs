using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CyberPolygon.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowedTeamIds",
                table: "Scenarios",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedTeamIds",
                table: "Scenarios");
        }
    }
}
