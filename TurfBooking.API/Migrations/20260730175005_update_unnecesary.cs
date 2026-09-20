using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TurfBooking.API.Migrations
{
    /// <inheritdoc />
    public partial class update_unnecesary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "TurfImages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                table: "TurfImages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "TurfImages");

            migrationBuilder.DropColumn(
                name: "IsPrimary",
                table: "TurfImages");
        }
    }
}
