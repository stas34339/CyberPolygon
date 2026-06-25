using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CyberPolygon.Migrations
{
    /// <inheritdoc />
    public partial class AddScenarioConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScenarioConnection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CyberScenarioId = table.Column<int>(type: "integer", nullable: false),
                    FromDeviceId = table.Column<int>(type: "integer", nullable: false),
                    ToDeviceId = table.Column<int>(type: "integer", nullable: false),
                    LinkType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioConnection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScenarioConnection_ScenarioDevices_FromDeviceId",
                        column: x => x.FromDeviceId,
                        principalTable: "ScenarioDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScenarioConnection_ScenarioDevices_ToDeviceId",
                        column: x => x.ToDeviceId,
                        principalTable: "ScenarioDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScenarioConnection_Scenarios_CyberScenarioId",
                        column: x => x.CyberScenarioId,
                        principalTable: "Scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioConnection_CyberScenarioId",
                table: "ScenarioConnection",
                column: "CyberScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioConnection_FromDeviceId",
                table: "ScenarioConnection",
                column: "FromDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioConnection_ToDeviceId",
                table: "ScenarioConnection",
                column: "ToDeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScenarioConnection");
        }
    }
}
