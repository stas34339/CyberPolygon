using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CyberPolygon.Migrations
{
    /// <inheritdoc />
    public partial class FixQuestionsTablePostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE TABLE IF NOT EXISTS \"AspNetUserPasskeys\" (\"Id\" text PRIMARY KEY);");

            migrationBuilder.DropForeignKey(
                name: "FK_ScenarioQuestion_Scenarios_CyberScenarioId",
                table: "ScenarioQuestion");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScenarioQuestion",
                table: "ScenarioQuestion");

            migrationBuilder.RenameTable(
                name: "ScenarioQuestion",
                newName: "ScenarioQuestions");

            migrationBuilder.RenameIndex(
                name: "IX_ScenarioQuestion_CyberScenarioId",
                table: "ScenarioQuestions",
                newName: "IX_ScenarioQuestions_CyberScenarioId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScenarioQuestions",
                table: "ScenarioQuestions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScenarioQuestions_Scenarios_CyberScenarioId",
                table: "ScenarioQuestions",
                column: "CyberScenarioId",
                principalTable: "Scenarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScenarioQuestions_Scenarios_CyberScenarioId",
                table: "ScenarioQuestions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScenarioQuestions",
                table: "ScenarioQuestions");

            migrationBuilder.RenameTable(
                name: "ScenarioQuestions",
                newName: "ScenarioQuestion");

            migrationBuilder.RenameIndex(
                name: "IX_ScenarioQuestions_CyberScenarioId",
                table: "ScenarioQuestion",
                newName: "IX_ScenarioQuestion_CyberScenarioId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScenarioQuestion",
                table: "ScenarioQuestion",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScenarioQuestion_Scenarios_CyberScenarioId",
                table: "ScenarioQuestion",
                column: "CyberScenarioId",
                principalTable: "Scenarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
