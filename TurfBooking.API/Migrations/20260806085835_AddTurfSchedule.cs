using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TurfBooking.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTurfSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TurfAmenities",
                table: "TurfAmenities");

            migrationBuilder.DropColumn(
                name: "IsClosed",
                table: "TurfSchedules");

            migrationBuilder.RenameColumn(
                name: "OpeningTime",
                table: "TurfSchedules",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "ClosingTime",
                table: "TurfSchedules",
                newName: "EndTime");

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "TurfSchedules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "TurfAmenities",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "Amenity",
                table: "TurfAmenities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TurfAmenities",
                table: "TurfAmenities",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_TurfAmenities_TurfId_Amenity",
                table: "TurfAmenities",
                columns: new[] { "TurfId", "Amenity" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TurfAmenities",
                table: "TurfAmenities");

            migrationBuilder.DropIndex(
                name: "IX_TurfAmenities_TurfId_Amenity",
                table: "TurfAmenities");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "TurfSchedules");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "TurfAmenities");

            migrationBuilder.DropColumn(
                name: "Amenity",
                table: "TurfAmenities");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "TurfSchedules",
                newName: "OpeningTime");

            migrationBuilder.RenameColumn(
                name: "EndTime",
                table: "TurfSchedules",
                newName: "ClosingTime");

            migrationBuilder.AddColumn<bool>(
                name: "IsClosed",
                table: "TurfSchedules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TurfAmenities",
                table: "TurfAmenities",
                columns: new[] { "TurfId", "AmenityId" });
        }
    }
}
