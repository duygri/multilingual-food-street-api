using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodStreet.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Foods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Foods",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Foods",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApprovedAt", "IsApproved" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "Foods",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ApprovedAt", "IsApproved" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "Foods",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ApprovedAt", "IsApproved" },
                values: new object[] { null, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Foods");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Foods");
        }
    }
}
