using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NarrationApp.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerRegistrationFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "full_name",
                table: "app_users",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "full_name",
                table: "app_users");
        }
    }
}
