using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CyberPolygon.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentationFileName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentationFileName",
                table: "Scenarios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentationPath",
                table: "Scenarios",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProgresses_CyberScenarioId",
                table: "UserProgresses",
                column: "CyberScenarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserProgresses_Scenarios_CyberScenarioId",
                table: "UserProgresses",
                column: "CyberScenarioId",
                principalTable: "Scenarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserProgresses_Scenarios_CyberScenarioId",
                table: "UserProgresses");

            migrationBuilder.DropIndex(
                name: "IX_UserProgresses_CyberScenarioId",
                table: "UserProgresses");

            migrationBuilder.DropColumn(
                name: "DocumentationFileName",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "DocumentationPath",
                table: "Scenarios");
        }
    }
}
