using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NarrationApp.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriesAndPoiSourceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "category_id",
                table: "pois",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "pois",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tts_script",
                table: "pois",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    icon = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pois_category_id",
                table: "pois",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_categories_display_order_name",
                table: "categories",
                columns: new[] { "display_order", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_categories_slug",
                table: "categories",
                column: "slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_pois_categories_category_id",
                table: "pois",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pois_categories_category_id",
                table: "pois");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropIndex(
                name: "ix_pois_category_id",
                table: "pois");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "pois");

            migrationBuilder.DropColumn(
                name: "description",
                table: "pois");

            migrationBuilder.DropColumn(
                name: "tts_script",
                table: "pois");
        }
    }
}
