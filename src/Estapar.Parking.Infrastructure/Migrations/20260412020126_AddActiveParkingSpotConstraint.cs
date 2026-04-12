using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Estapar.Parking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveParkingSpotConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_ParkingSpotId",
                table: "ParkingSessions");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_ActiveParkingSpot",
                table: "ParkingSessions",
                column: "ParkingSpotId",
                unique: true,
                filter: "[Status] = 1 AND [ParkingSpotId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_ActiveParkingSpot",
                table: "ParkingSessions");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_ParkingSpotId",
                table: "ParkingSessions",
                column: "ParkingSpotId");
        }
    }
}