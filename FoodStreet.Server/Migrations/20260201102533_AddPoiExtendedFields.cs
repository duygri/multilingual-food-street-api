using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROJECT_C_.Migrations
{
    /// <inheritdoc />
    public partial class AddPoiExtendedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Foods",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MapLink",
                table: "Foods",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Foods",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Radius",
                table: "Foods",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "TtsScript",
                table: "Foods",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "FoodTranslations",
                keyColumn: "Id",
                keyValue: 5,
                column: "Description",
                value: "Cơm tấm sườn");

            migrationBuilder.UpdateData(
                table: "Foods",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ImageUrl", "MapLink", "Priority", "Radius", "TtsScript" },
                values: new object[] { null, null, 0, 50.0, null });

            migrationBuilder.UpdateData(
                table: "Foods",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ImageUrl", "MapLink", "Priority", "Radius", "TtsScript" },
                values: new object[] { null, null, 0, 50.0, null });

            migrationBuilder.UpdateData(
                table: "Foods",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ImageUrl", "MapLink", "Priority", "Radius", "TtsScript" },
                values: new object[] { null, null, 0, 50.0, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Foods");

            migrationBuilder.DropColumn(
                name: "MapLink",
                table: "Foods");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Foods");

            migrationBuilder.DropColumn(
                name: "Radius",
                table: "Foods");

            migrationBuilder.DropColumn(
                name: "TtsScript",
                table: "Foods");

            migrationBuilder.UpdateData(
                table: "FoodTranslations",
                keyColumn: "Id",
                keyValue: 5,
                column: "Description",
                value: "Cơm tấm sườn nướng");
        }
    }
}
