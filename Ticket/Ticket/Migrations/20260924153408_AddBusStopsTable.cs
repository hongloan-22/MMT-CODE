using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticket.Migrations
{
    /// <inheritdoc />
    public partial class AddBusStopsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusRoutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RouteName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TotalDistanceKm = table.Column<double>(type: "float", nullable: false),
                    EstimatedDuration = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusRoutes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusStops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusStops", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RouteStops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RouteId = table.Column<int>(type: "int", nullable: false),
                    StopId = table.Column<int>(type: "int", nullable: false),
                    StopOrder = table.Column<int>(type: "int", nullable: false),
                    DistanceFromStartKm = table.Column<double>(type: "float", nullable: false),
                    TravelTimeFromStartMinutes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteStops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteStops_BusRoutes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "BusRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteStops_BusStops_StopId",
                        column: x => x.StopId,
                        principalTable: "BusStops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: "usr-adm-001",
                column: "updated_at",
                value: new DateTime(2026, 9, 24, 15, 34, 8, 252, DateTimeKind.Utc).AddTicks(3043));

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: "usr-cus-001",
                column: "updated_at",
                value: new DateTime(2026, 9, 24, 15, 34, 8, 252, DateTimeKind.Utc).AddTicks(3068));

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: "usr-drv-001",
                column: "updated_at",
                value: new DateTime(2026, 9, 24, 15, 34, 8, 252, DateTimeKind.Utc).AddTicks(3065));

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: "usr-opr-001",
                column: "updated_at",
                value: new DateTime(2026, 9, 24, 15, 34, 8, 252, DateTimeKind.Utc).AddTicks(3061));

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_RouteId",
                table: "RouteStops",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_StopId",
                table: "RouteStops",
                column: "StopId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteStops");

            migrationBuilder.DropTable(
                name: "BusRoutes");

            migrationBuilder.DropTable(
                name: "BusStops");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: "usr-adm-001",
                column: "updated_at",
                value: new DateTime(2026, 9, 23, 19, 49, 16, 736, DateTimeKind.Utc).AddTicks(7946));

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: "usr-cus-001",
                column: "updated_at",
                value: new DateTime(2026, 9, 23, 19, 49, 16, 736, DateTimeKind.Utc).AddTicks(7995));

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: "usr-drv-001",
                column: "updated_at",
                value: new DateTime(2026, 9, 23, 19, 49, 16, 736, DateTimeKind.Utc).AddTicks(7988));

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: "usr-opr-001",
                column: "updated_at",
                value: new DateTime(2026, 9, 23, 19, 49, 16, 736, DateTimeKind.Utc).AddTicks(7980));
        }
    }
}
