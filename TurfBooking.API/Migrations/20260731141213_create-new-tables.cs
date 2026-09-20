using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TurfBooking.API.Migrations
{
    /// <inheritdoc />
    public partial class createnewtables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosingTime",
                table: "Turfs");

            migrationBuilder.DropColumn(
                name: "OpeningTime",
                table: "Turfs");

            migrationBuilder.AddColumn<string>(
                name: "ContactNumber",
                table: "Turfs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleMapLink",
                table: "Turfs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "TurfImages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "TurfImages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "TurfImages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadedAt",
                table: "TurfImages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactNumber",
                table: "Turfs");

            migrationBuilder.DropColumn(
                name: "GoogleMapLink",
                table: "Turfs");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "TurfImages");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "TurfImages");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "TurfImages");

            migrationBuilder.DropColumn(
                name: "UploadedAt",
                table: "TurfImages");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ClosingTime",
                table: "Turfs",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "OpeningTime",
                table: "Turfs",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));
        }
    }
}
