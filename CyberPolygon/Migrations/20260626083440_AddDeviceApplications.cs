using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CyberPolygon.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScenarioDeviceId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    Vulnerability = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceApplications_ScenarioDevices_ScenarioDeviceId",
                        column: x => x.ScenarioDeviceId,
                        principalTable: "ScenarioDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProgresses_TeamId",
                table: "UserProgresses",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceApplications_ScenarioDeviceId",
                table: "DeviceApplications",
                column: "ScenarioDeviceId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserProgresses_UserTeams_TeamId",
                table: "UserProgresses",
                column: "TeamId",
                principalTable: "UserTeams",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserProgresses_UserTeams_TeamId",
                table: "UserProgresses");

            migrationBuilder.DropTable(
                name: "DeviceApplications");

            migrationBuilder.DropIndex(
                name: "IX_UserProgresses_TeamId",
                table: "UserProgresses");
        }
    }
}
