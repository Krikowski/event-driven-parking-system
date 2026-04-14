using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Estapar.Parking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleEventIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PayloadSnapshot",
                table: "VehicleEvents",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "VehicleEvents",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleEvents_IdempotencyKey",
                table: "VehicleEvents",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleEvents_IdempotencyKey",
                table: "VehicleEvents");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "VehicleEvents");

            migrationBuilder.AlterColumn<string>(
                name: "PayloadSnapshot",
                table: "VehicleEvents",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
