using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CyberPolygon.Migrations
{
    /// <inheritdoc />
    public partial class AddVmIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DomainLogin",
                table: "ScenarioDevices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DomainPassword",
                table: "ScenarioDevices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VmName",
                table: "ScenarioDevices",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DomainLogin",
                table: "ScenarioDevices");

            migrationBuilder.DropColumn(
                name: "DomainPassword",
                table: "ScenarioDevices");

            migrationBuilder.DropColumn(
                name: "VmName",
                table: "ScenarioDevices");
        }
    }
}
