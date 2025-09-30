using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFIT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addedcolumorder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConsumedByMembershipId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsumedForMembershipAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsumedByMembershipId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ConsumedForMembershipAt",
                table: "Orders");
        }
    }
}
