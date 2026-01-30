using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PROJECT_C_.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodTranslation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FoodTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FoodId = table.Column<int>(type: "int", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                table: "FoodTranslations",
                columns: new[] { "Id", "Description", "FoodId", "LanguageCode", "Name" },
                values: new object[,]
                {
                    { 1, "Phở bò truyền thống Việt Nam", 1, "vi-VN", "Phở Bò" },
                    { 2, "Traditional Vietnamese beef noodle soup", 1, "en-US", "Beef Pho" },
                    { 3, "Bánh mì Việt Nam", 2, "vi-VN", "Bánh Mì" },
                    { 4, "Vietnamese baguette sandwich", 2, "en-US", "Vietnamese Baguette" },
                    { 5, "Cơm tấm sườn nướng", 3, "vi-VN", "Cơm Tấm" },
                    { 6, "Broken rice with grilled pork", 3, "en-US", "Broken Rice" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoodTranslations_FoodId",
                table: "FoodTranslations",
                column: "FoodId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FoodTranslations");
        }
    }
}
