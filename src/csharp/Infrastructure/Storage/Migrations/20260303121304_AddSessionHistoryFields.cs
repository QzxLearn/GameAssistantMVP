using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameAssistant.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionHistoryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CardType",
                table: "game_sessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Confidence",
                table: "game_sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsReviewed",
                table: "game_sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OcrResult",
                table: "game_sessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "game_sessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScreenshotPath",
                table: "game_sessions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CardType",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "IsReviewed",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "OcrResult",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "ScreenshotPath",
                table: "game_sessions");
        }
    }
}
