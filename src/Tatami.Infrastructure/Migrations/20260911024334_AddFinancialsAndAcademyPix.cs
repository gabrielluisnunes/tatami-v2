using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tatami.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialsAndAcademyPix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PixKey",
                table: "academies",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PixKeyType",
                table: "academies",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "financials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReferenceMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financials", x => x.Id);
                    table.CheckConstraint("CK_financials_Amount", "\"Amount\" > 0");
                    table.CheckConstraint("CK_financials_ReferenceMonth", "EXTRACT(DAY FROM \"ReferenceMonth\") = 1");
                    table.CheckConstraint("CK_financials_Status", "\"Status\" IN ('pending', 'paid', 'overdue', 'aguardando_confirmacao')");
                    table.ForeignKey(
                        name: "FK_financials_academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_financials_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_financials_AcademyId_Status_DueDate",
                table: "financials",
                columns: new[] { "AcademyId", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_financials_StudentId_DueDate",
                table: "financials",
                columns: new[] { "StudentId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_financials_StudentId_ReferenceMonth",
                table: "financials",
                columns: new[] { "StudentId", "ReferenceMonth" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "financials");

            migrationBuilder.DropColumn(
                name: "PixKey",
                table: "academies");

            migrationBuilder.DropColumn(
                name: "PixKeyType",
                table: "academies");
        }
    }
}
