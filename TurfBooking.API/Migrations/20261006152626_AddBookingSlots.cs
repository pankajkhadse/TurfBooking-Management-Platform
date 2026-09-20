using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TurfBooking.API.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_TurfId_BookingDate_StartTime",
                table: "Bookings");

            migrationBuilder.CreateTable(
                name: "BookingSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    TurfId = table.Column<int>(type: "int", nullable: false),
                    BookingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingSlots_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingSlots_Turfs_TurfId",
                        column: x => x.TurfId,
                        principalTable: "Turfs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TurfId",
                table: "Bookings",
                column: "TurfId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSlots_BookingId",
                table: "BookingSlots",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSlots_TurfId_BookingDate_StartTime",
                table: "BookingSlots",
                columns: new[] { "TurfId", "BookingDate", "StartTime" },
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingSlots");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TurfId",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TurfId_BookingDate_StartTime",
                table: "Bookings",
                columns: new[] { "TurfId", "BookingDate", "StartTime" },
                unique: true,
                filter: "[Status] IN (1, 2)");
        }
    }
}
