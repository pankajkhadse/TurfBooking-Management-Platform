using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TurfBooking.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTurfSportE : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TurfSports_Sports_SportId",
                table: "TurfSports");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TurfSports",
                table: "TurfSports");

            migrationBuilder.AlterColumn<int>(
                name: "SportId",
                table: "TurfSports",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "TurfSports",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "SportType",
                table: "TurfSports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TurfSports",
                table: "TurfSports",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_TurfSports_TurfId",
                table: "TurfSports",
                column: "TurfId");

            migrationBuilder.AddForeignKey(
                name: "FK_TurfSports_Sports_SportId",
                table: "TurfSports",
                column: "SportId",
                principalTable: "Sports",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TurfSports_Sports_SportId",
                table: "TurfSports");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TurfSports",
                table: "TurfSports");

            migrationBuilder.DropIndex(
                name: "IX_TurfSports_TurfId",
                table: "TurfSports");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "TurfSports");

            migrationBuilder.DropColumn(
                name: "SportType",
                table: "TurfSports");

            migrationBuilder.AlterColumn<int>(
                name: "SportId",
                table: "TurfSports",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TurfSports",
                table: "TurfSports",
                columns: new[] { "TurfId", "SportId" });

            migrationBuilder.AddForeignKey(
                name: "FK_TurfSports_Sports_SportId",
                table: "TurfSports",
                column: "SportId",
                principalTable: "Sports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
