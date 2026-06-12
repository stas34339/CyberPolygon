using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CyberPolygon.Migrations
{
    /// <inheritdoc />
    public partial class _3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AnsweredAt",
                table: "UserTestAnswers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "SelectedAnswer",
                table: "UserTestAnswers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnsweredAt",
                table: "UserTestAnswers");

            migrationBuilder.DropColumn(
                name: "SelectedAnswer",
                table: "UserTestAnswers");
        }
    }
}
