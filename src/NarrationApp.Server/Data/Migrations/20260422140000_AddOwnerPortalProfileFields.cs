using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NarrationApp.Server.Data.Migrations;

public partial class AddOwnerPortalProfileFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "last_login_at_utc",
            table: "app_users",
            type: "timestamp with time zone",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "last_login_at_utc",
            table: "app_users");
    }
}
