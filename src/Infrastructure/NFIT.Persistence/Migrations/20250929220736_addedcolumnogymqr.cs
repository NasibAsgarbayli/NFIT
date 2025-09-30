using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFIT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addedcolumnogymqr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "GymQRCodes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOneTime",
                table: "GymQRCodes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UsedAt",
                table: "GymQRCodes",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "GymQRCodes");

            migrationBuilder.DropColumn(
                name: "IsOneTime",
                table: "GymQRCodes");

            migrationBuilder.DropColumn(
                name: "UsedAt",
                table: "GymQRCodes");
        }
    }
}
