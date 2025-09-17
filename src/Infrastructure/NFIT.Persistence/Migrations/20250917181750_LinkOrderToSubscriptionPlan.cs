using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFIT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkOrderToSubscriptionPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Memberships_Gyms_GymId",
                table: "Memberships");

            migrationBuilder.DropIndex(
                name: "IX_Memberships_GymId",
                table: "Memberships");

            migrationBuilder.DropColumn(
                name: "GymId",
                table: "Memberships");

            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionPlanId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SubscriptionPlanId",
                table: "Orders",
                column: "SubscriptionPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_SubscriptionPlans_SubscriptionPlanId",
                table: "Orders",
                column: "SubscriptionPlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_SubscriptionPlans_SubscriptionPlanId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_SubscriptionPlanId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlanId",
                table: "Orders");

            migrationBuilder.AddColumn<Guid>(
                name: "GymId",
                table: "Memberships",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_GymId",
                table: "Memberships",
                column: "GymId");

            migrationBuilder.AddForeignKey(
                name: "FK_Memberships_Gyms_GymId",
                table: "Memberships",
                column: "GymId",
                principalTable: "Gyms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
