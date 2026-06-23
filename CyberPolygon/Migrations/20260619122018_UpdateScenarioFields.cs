using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CyberPolygon.Migrations
{
    /// <inheritdoc />
    public partial class UpdateScenarioFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Difficulty",
                table: "Scenarios",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Scenarios",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExampleAnswer",
                table: "ScenarioQuestions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "ExampleAnswer",
                table: "ScenarioQuestions");
        }
    }
}
