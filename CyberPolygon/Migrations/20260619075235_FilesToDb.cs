using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CyberPolygon.Migrations
{
    /// <inheritdoc />
    public partial class FilesToDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "SchemaFileData",
                table: "Scenarios",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchemaFileName",
                table: "Scenarios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "FileData",
                table: "ScenarioDocument",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "ScenarioDocument",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SchemaFileData",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "SchemaFileName",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "FileData",
                table: "ScenarioDocument");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "ScenarioDocument");
        }
    }
}
