using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CyberPolygon.Migrations
{
    /// <inheritdoc />
    public partial class AddInst : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVisible",
                table: "Instructions");

            migrationBuilder.RenameColumn(
                name: "Icon",
                table: "Instructions",
                newName: "IconName");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Instructions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Instructions");

            migrationBuilder.RenameColumn(
                name: "IconName",
                table: "Instructions",
                newName: "Icon");

            migrationBuilder.AddColumn<bool>(
                name: "IsVisible",
                table: "Instructions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
