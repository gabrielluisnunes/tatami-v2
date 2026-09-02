using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tatami.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandAcademyAndOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyPrice",
                table: "academies",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "academies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Sport",
                table: "academies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionStatus",
                table: "academies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_academies_OwnerId",
                table: "academies",
                column: "OwnerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_academies_OwnerId",
                table: "academies");

            migrationBuilder.DropColumn(
                name: "MonthlyPrice",
                table: "academies");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "academies");

            migrationBuilder.DropColumn(
                name: "Sport",
                table: "academies");

            migrationBuilder.DropColumn(
                name: "SubscriptionStatus",
                table: "academies");
        }
    }
}
