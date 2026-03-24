using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FoodStreet.Server.Migrations
{
    /// <inheritdoc />
    public partial class AudioFixFoodToPlace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AudioFiles_Foods_FoodId",
                table: "AudioFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayLogs_Foods_FoodId",
                table: "PlayLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_TourItems_Foods_FoodId",
                table: "TourItems");

            migrationBuilder.DropTable(
                name: "FoodTranslations");

            migrationBuilder.DropTable(
                name: "Foods");

            migrationBuilder.DropIndex(
                name: "IX_AudioFiles_FoodId",
                table: "AudioFiles");

            migrationBuilder.DropColumn(
                name: "FoodId",
                table: "AudioFiles");

            migrationBuilder.RenameColumn(
                name: "FoodId",
                table: "TourItems",
                newName: "LocationId");

            migrationBuilder.RenameIndex(
                name: "IX_TourItems_FoodId",
                table: "TourItems",
                newName: "IX_TourItems_LocationId");

            migrationBuilder.RenameColumn(
                name: "FoodId",
                table: "PlayLogs",
                newName: "LocationId");

            migrationBuilder.RenameIndex(
                name: "IX_PlayLogs_FoodId",
                table: "PlayLogs",
                newName: "IX_PlayLogs_LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayLogs_Locations_LocationId",
                table: "PlayLogs",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TourItems_Locations_LocationId",
                table: "TourItems",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayLogs_Locations_LocationId",
                table: "PlayLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_TourItems_Locations_LocationId",
                table: "TourItems");

            migrationBuilder.RenameColumn(
                name: "LocationId",
                table: "TourItems",
                newName: "FoodId");

            migrationBuilder.RenameIndex(
                name: "IX_TourItems_LocationId",
                table: "TourItems",
                newName: "IX_TourItems_FoodId");

            migrationBuilder.RenameColumn(
                name: "LocationId",
                table: "PlayLogs",
                newName: "FoodId");

            migrationBuilder.RenameIndex(
                name: "IX_PlayLogs_LocationId",
                table: "PlayLogs",
                newName: "IX_PlayLogs_FoodId");

            migrationBuilder.AddColumn<int>(
                name: "FoodId",
                table: "AudioFiles",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Foods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CategoryId = table.Column<int>(type: "integer", nullable: true),
                    LocationId = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Foods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Foods_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Foods_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FoodTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FoodId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    LanguageCode = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodTranslations_Foods_FoodId",
                        column: x => x.FoodId,
                        principalTable: "Foods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Foods",
                columns: new[] { "Id", "CategoryId", "Description", "ImageUrl", "LocationId", "Name", "Price" },
                values: new object[,]
                {
                    { 1, null, "Traditional Vietnamese beef noodle soup", null, 1, "Pho Bo", 45000m },
                    { 2, null, "Vietnamese baguette sandwich", null, 2, "Banh Mi", 25000m },
                    { 3, null, "Broken rice with grilled pork", null, 3, "Com Tam", 50000m }
                });

            migrationBuilder.InsertData(
                table: "FoodTranslations",
                columns: new[] { "Id", "Description", "FoodId", "LanguageCode", "Name" },
                values: new object[,]
                {
                    { 1, "Phở bò truyền thống Việt Nam", 1, "vi-VN", "Phở Bò" },
                    { 2, "Traditional Vietnamese beef noodle soup", 1, "en-US", "Beef Pho" },
                    { 3, "Bánh mì Việt Nam", 2, "vi-VN", "Bánh Mì" },
                    { 4, "Vietnamese baguette sandwich", 2, "en-US", "Vietnamese Baguette" },
                    { 5, "Cơm tấm sườn", 3, "vi-VN", "Cơm Tấm" },
                    { 6, "Broken rice with grilled pork", 3, "en-US", "Broken Rice" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AudioFiles_FoodId",
                table: "AudioFiles",
                column: "FoodId");

            migrationBuilder.CreateIndex(
                name: "IX_Foods_CategoryId",
                table: "Foods",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Foods_LocationId",
                table: "Foods",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodTranslations_FoodId",
                table: "FoodTranslations",
                column: "FoodId");

            migrationBuilder.AddForeignKey(
                name: "FK_AudioFiles_Foods_FoodId",
                table: "AudioFiles",
                column: "FoodId",
                principalTable: "Foods",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayLogs_Foods_FoodId",
                table: "PlayLogs",
                column: "FoodId",
                principalTable: "Foods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TourItems_Foods_FoodId",
                table: "TourItems",
                column: "FoodId",
                principalTable: "Foods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
