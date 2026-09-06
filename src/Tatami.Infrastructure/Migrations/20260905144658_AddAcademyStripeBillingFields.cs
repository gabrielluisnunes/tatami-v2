using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tatami.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAcademyStripeBillingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Plan",
                table: "academies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "academies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSubscriptionId",
                table: "academies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndsAt",
                table: "academies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_academies_StripeSubscriptionId",
                table: "academies",
                column: "StripeSubscriptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_academies_StripeSubscriptionId",
                table: "academies");

            migrationBuilder.DropColumn(
                name: "Plan",
                table: "academies");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "academies");

            migrationBuilder.DropColumn(
                name: "StripeSubscriptionId",
                table: "academies");

            migrationBuilder.DropColumn(
                name: "TrialEndsAt",
                table: "academies");
        }
    }
}
