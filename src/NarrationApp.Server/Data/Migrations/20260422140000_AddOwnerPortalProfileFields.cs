using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NarrationApp.Server.Data.Migrations;

public partial class AddOwnerPortalProfileFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "created_at_utc",
            table: "app_users",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "CURRENT_TIMESTAMP");

        migrationBuilder.AddColumn<DateTime>(
            name: "last_login_at_utc",
            table: "app_users",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "managed_area",
            table: "app_users",
            type: "character varying(250)",
            maxLength: 250,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "phone",
            table: "app_users",
            type: "character varying(30)",
            maxLength: 30,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "created_at_utc",
            table: "app_users");

        migrationBuilder.DropColumn(
            name: "last_login_at_utc",
            table: "app_users");

        migrationBuilder.DropColumn(
            name: "managed_area",
            table: "app_users");

        migrationBuilder.DropColumn(
            name: "phone",
            table: "app_users");
    }
}
