using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NarrationApp.Server.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260418170000_AddManagedLanguages")]
public partial class AddManagedLanguages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "managed_languages",
            columns: table => new
            {
                code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                native_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                flag_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                role = table.Column<int>(type: "integer", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_managed_languages", x => x.code);
            });

        migrationBuilder.CreateIndex(
            name: "ix_managed_languages_is_active",
            table: "managed_languages",
            column: "is_active");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "managed_languages");
    }
}
