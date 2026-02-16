using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FoodStreet.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Foods");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Foods");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Foods");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Foods");

            migrationBuilder.DropColumn(
                name: "MapLink",
                table: "Foods");

            migrationBuilder.DropColumn(
                name: "OwnerId",
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

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "Foods",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "AudioFiles",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Radius = table.Column<double>(type: "double precision", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    MapLink = table.Column<string>(type: "text", nullable: true),
                    TtsScript = table.Column<string>(type: "text", nullable: true),
                    OwnerId = table.Column<string>(type: "text", nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CategoryId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Locations_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LocationTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LocationId = table.Column<int>(type: "integer", nullable: false),
                    LanguageCode = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationTranslations_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Foods",
                keyColumn: "Id",
                keyValue: 1,
                column: "LocationId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Foods",
                keyColumn: "Id",
                keyValue: 2,
                column: "LocationId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Foods",
                keyColumn: "Id",
                keyValue: 3,
                column: "LocationId",
                value: 3);

            migrationBuilder.InsertData(
                table: "Locations",
                columns: new[] { "Id", "Address", "ApprovedAt", "CategoryId", "Description", "ImageUrl", "IsApproved", "Latitude", "Longitude", "MapLink", "Name", "OwnerId", "Priority", "Radius", "TtsScript" },
                values: new object[,]
                {
                    { 1, "Đường Vĩnh Khánh, Q.4, TP.HCM", null, null, "Quán phở bò truyền thống", null, true, 10.776889000000001, 106.700806, null, "Quán Phở Bò Vĩnh Khánh", null, 0, 50.0, null },
                    { 2, "Đường Vĩnh Khánh, Q.4, TP.HCM", null, null, "Bánh mì nóng giòn", null, true, 10.762622, 106.660172, null, "Tiệm Bánh Mì Sài Gòn", null, 0, 50.0, null },
                    { 3, "Đường Vĩnh Khánh, Q.4, TP.HCM", null, null, "Cơm tấm sườn nướng", null, true, 10.792375, 106.691689, null, "Quán Cơm Tấm Bụi", null, 0, 50.0, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Foods_LocationId",
                table: "Foods",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AudioFiles_LocationId",
                table: "AudioFiles",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_CategoryId",
                table: "Locations",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationTranslations_LocationId",
                table: "LocationTranslations",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_AudioFiles_Locations_LocationId",
                table: "AudioFiles",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Foods_Locations_LocationId",
                table: "Foods",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AudioFiles_Locations_LocationId",
                table: "AudioFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Foods_Locations_LocationId",
                table: "Foods");

            migrationBuilder.DropTable(
                name: "LocationTranslations");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_Foods_LocationId",
                table: "Foods");

            migrationBuilder.DropIndex(
                name: "IX_AudioFiles_LocationId",
                table: "AudioFiles");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Foods");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "AudioFiles");

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

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Foods",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Foods",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "MapLink",
                table: "Foods",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Foods",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Foods",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Radius",
                table: "Foods",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "TtsScript",
                table: "Foods",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Foods",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApprovedAt", "IsApproved", "Latitude", "Longitude", "MapLink", "OwnerId", "Priority", "Radius", "TtsScript" },
                values: new object[] { null, false, 10.776889000000001, 106.700806, null, null, 0, 50.0, null });

            migrationBuilder.UpdateData(
                table: "Foods",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ApprovedAt", "IsApproved", "Latitude", "Longitude", "MapLink", "OwnerId", "Priority", "Radius", "TtsScript" },
                values: new object[] { null, false, 10.762622, 106.660172, null, null, 0, 50.0, null });

            migrationBuilder.UpdateData(
                table: "Foods",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ApprovedAt", "IsApproved", "Latitude", "Longitude", "MapLink", "OwnerId", "Priority", "Radius", "TtsScript" },
                values: new object[] { null, false, 10.792375, 106.691689, null, null, 0, 50.0, null });
        }
    }
}
