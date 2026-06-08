using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CyberPolygon.Migrations
{
    /// <inheritdoc />
    public partial class AddScenarioTimer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "UserProgresses");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletedAt",
                table: "UserProgresses",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "UserProgresses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "UserProgresses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TargetEndTime",
                table: "UserProgresses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TimeSpent",
                table: "UserProgresses",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationInMinutes",
                table: "Scenarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "UserProgresses");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "UserProgresses");

            migrationBuilder.DropColumn(
                name: "TargetEndTime",
                table: "UserProgresses");

            migrationBuilder.DropColumn(
                name: "TimeSpent",
                table: "UserProgresses");

            migrationBuilder.DropColumn(
                name: "DurationInMinutes",
                table: "Scenarios");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletedAt",
                table: "UserProgresses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "UserProgresses",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
