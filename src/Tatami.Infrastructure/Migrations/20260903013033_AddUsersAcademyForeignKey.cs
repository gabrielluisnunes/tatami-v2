using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tatami.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersAcademyForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE users
                SET "AcademyId" = NULL
                WHERE "AcademyId" IS NOT NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM academies a WHERE a."Id" = users."AcademyId"
                  );
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_users_academies_AcademyId",
                table: "users",
                column: "AcademyId",
                principalTable: "academies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_academies_AcademyId",
                table: "users");
        }
    }
}
