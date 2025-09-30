using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFIT.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainerImagesRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                table: "Trainers");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Trainers",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TrainerId",
                table: "Images",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trainers_UserId",
                table: "Trainers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_TrainerId",
                table: "Images",
                column: "TrainerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Images_Trainers_TrainerId",
                table: "Images",
                column: "TrainerId",
                principalTable: "Trainers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Trainers_AspNetUsers_UserId",
                table: "Trainers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Images_Trainers_TrainerId",
                table: "Images");

            migrationBuilder.DropForeignKey(
                name: "FK_Trainers_AspNetUsers_UserId",
                table: "Trainers");

            migrationBuilder.DropIndex(
                name: "IX_Trainers_UserId",
                table: "Trainers");

            migrationBuilder.DropIndex(
                name: "IX_Images_TrainerId",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Trainers");

            migrationBuilder.DropColumn(
                name: "TrainerId",
                table: "Images");

            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                table: "Trainers",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");
        }
    }
}
