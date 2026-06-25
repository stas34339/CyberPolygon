using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CyberPolygon.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToAnswers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "UserAnswerProgresses",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAnswerProgresses_UserId",
                table: "UserAnswerProgresses",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAnswerProgresses_AspNetUsers_UserId",
                table: "UserAnswerProgresses",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAnswerProgresses_AspNetUsers_UserId",
                table: "UserAnswerProgresses");

            migrationBuilder.DropIndex(
                name: "IX_UserAnswerProgresses_UserId",
                table: "UserAnswerProgresses");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserAnswerProgresses");
        }
    }
}
