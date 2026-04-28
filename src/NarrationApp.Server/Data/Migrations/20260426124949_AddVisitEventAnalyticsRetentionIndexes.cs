using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NarrationApp.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitEventAnalyticsRetentionIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_visit_events_created_at",
                table: "visit_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_visit_events_device_id_created_at",
                table: "visit_events",
                columns: new[] { "device_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_visit_events_event_type_created_at",
                table: "visit_events",
                columns: new[] { "event_type", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_visit_events_poi_id_event_type_created_at",
                table: "visit_events",
                columns: new[] { "poi_id", "event_type", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_visit_events_created_at",
                table: "visit_events");

            migrationBuilder.DropIndex(
                name: "ix_visit_events_device_id_created_at",
                table: "visit_events");

            migrationBuilder.DropIndex(
                name: "ix_visit_events_event_type_created_at",
                table: "visit_events");

            migrationBuilder.DropIndex(
                name: "ix_visit_events_poi_id_event_type_created_at",
                table: "visit_events");
        }
    }
}
