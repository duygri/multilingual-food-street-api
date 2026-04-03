using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodStreet.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTranslationFallbackFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudioUrl",
                table: "LocationTranslations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GeneratedAt",
                table: "LocationTranslations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFallback",
                table: "LocationTranslations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TtsScript",
                table: "LocationTranslations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioUrl",
                table: "LocationTranslations");

            migrationBuilder.DropColumn(
                name: "GeneratedAt",
                table: "LocationTranslations");

            migrationBuilder.DropColumn(
                name: "IsFallback",
                table: "LocationTranslations");

            migrationBuilder.DropColumn(
                name: "TtsScript",
                table: "LocationTranslations");
        }
    }
}
