using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Estapar.Parking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefineParkingSessionOperationalIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_LicensePlate_Status",
                table: "ParkingSessions");

            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_SectorCode_EntryTimeUtc",
                table: "ParkingSessions");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_RevenueBySectorAndExitTime",
                table: "ParkingSessions",
                columns: new[] { "SectorCode", "ExitTimeUtc" },
                filter: "[ExitTimeUtc] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_RevenueBySectorAndExitTime",
                table: "ParkingSessions");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_LicensePlate_Status",
                table: "ParkingSessions",
                columns: new[] { "LicensePlate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_SectorCode_EntryTimeUtc",
                table: "ParkingSessions",
                columns: new[] { "SectorCode", "EntryTimeUtc" });
        }
    }
}
