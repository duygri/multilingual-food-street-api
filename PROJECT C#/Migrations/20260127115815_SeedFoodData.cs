using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PROJECT_C_.Migrations
{
    /// <inheritdoc />
    public partial class SeedFoodData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Foods",
                columns: new[] { "Id", "Description", "Latitude", "Longitude", "Name", "Price" },
                values: new object[,]
                {
                    { 1, "Traditional Vietnamese beef noodle soup", 0.0, 0.0, "Pho Bo", 45000m },
                    { 2, "Vietnamese baguette sandwich", 0.0, 0.0, "Banh Mi", 25000m },
                    { 3, "Broken rice with grilled pork", 0.0, 0.0, "Com Tam", 50000m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Foods",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Foods",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Foods",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
