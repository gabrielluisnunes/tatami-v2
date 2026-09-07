using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tatami.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandStudentsAndStudentSports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM students;");

            migrationBuilder.DropIndex(
                name: "IX_students_AcademyId",
                table: "students");

            migrationBuilder.DropIndex(
                name: "IX_students_Email",
                table: "students");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "students",
                newName: "FullName");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "students",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                table: "students",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cep",
                table: "students",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "students",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyPhone",
                table: "students",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "students",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Neighborhood",
                table: "students",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentDueDay",
                table: "students",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "students",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "students",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "students",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "students",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "student_sports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sport = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Belt = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Degree = table.Column<int>(type: "integer", nullable: false),
                    BeltUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_sports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_student_sports_academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_student_sports_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_students_AcademyId_Email",
                table: "students",
                columns: new[] { "AcademyId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_students_AcademyId_IsActive",
                table: "students",
                columns: new[] { "AcademyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_students_UserId",
                table: "students",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_sports_AcademyId",
                table: "student_sports",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_student_sports_StudentId_Sport",
                table: "student_sports",
                columns: new[] { "StudentId", "Sport" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_students_users_UserId",
                table: "students",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_students_users_UserId",
                table: "students");

            migrationBuilder.DropTable(
                name: "student_sports");

            migrationBuilder.DropIndex(
                name: "IX_students_AcademyId_Email",
                table: "students");

            migrationBuilder.DropIndex(
                name: "IX_students_AcademyId_IsActive",
                table: "students");

            migrationBuilder.DropIndex(
                name: "IX_students_UserId",
                table: "students");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "students");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "students");

            migrationBuilder.DropColumn(
                name: "Cep",
                table: "students");

            migrationBuilder.DropColumn(
                name: "City",
                table: "students");

            migrationBuilder.DropColumn(
                name: "EmergencyPhone",
                table: "students");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "students");

            migrationBuilder.DropColumn(
                name: "Neighborhood",
                table: "students");

            migrationBuilder.DropColumn(
                name: "PaymentDueDay",
                table: "students");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "students");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "students");

            migrationBuilder.DropColumn(
                name: "State",
                table: "students");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "students");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "students",
                newName: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_students_AcademyId",
                table: "students",
                column: "AcademyId");

            migrationBuilder.CreateIndex(
                name: "IX_students_Email",
                table: "students",
                column: "Email");
        }
    }
}
