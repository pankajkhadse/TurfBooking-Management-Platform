using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TurfBooking.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBookingUniqueIndexForActiveStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_TurfId_BookingDate_StartTime",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TurfId_BookingDate_StartTime",
                table: "Bookings",
                columns: new[] { "TurfId", "BookingDate", "StartTime" },
                unique: true,
                filter: "[Status] IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_TurfId_BookingDate_StartTime",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TurfId_BookingDate_StartTime",
                table: "Bookings",
                columns: new[] { "TurfId", "BookingDate", "StartTime" },
                unique: true,
                filter: "[Status] <> 3");
        }
    }
}
