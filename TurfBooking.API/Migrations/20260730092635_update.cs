using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TurfBooking.API.Migrations
{
    /// <inheritdoc />
    public partial class update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "OwnerProfiles",
                newName: "AppliedAt");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "OwnerProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "OwnerProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "OwnerProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "OwnerProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactPersonName",
                table: "OwnerProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "OwnerProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "OwnerProfiles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "OwnerProfiles");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "OwnerProfiles");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "OwnerProfiles");

            migrationBuilder.DropColumn(
                name: "City",
                table: "OwnerProfiles");

            migrationBuilder.DropColumn(
                name: "ContactPersonName",
                table: "OwnerProfiles");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "OwnerProfiles");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "OwnerProfiles");

            migrationBuilder.RenameColumn(
                name: "AppliedAt",
                table: "OwnerProfiles",
                newName: "CreatedAt");
        }
    }
}
