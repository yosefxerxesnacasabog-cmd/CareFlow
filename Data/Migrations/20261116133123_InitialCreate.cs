using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CareFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiagnosticServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DailyCapacity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosticServices", x => x.Id);
                    table.CheckConstraint("CK_DiagnosticServices_DailyCapacity", "DailyCapacity >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    BookingDate = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingServices",
                columns: table => new
                {
                    BookingId = table.Column<int>(type: "INTEGER", nullable: false),
                    DiagnosticServiceId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingServices", x => new { x.BookingId, x.DiagnosticServiceId });
                    table.ForeignKey(
                        name: "FK_BookingServices_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingServices_DiagnosticServices_DiagnosticServiceId",
                        column: x => x.DiagnosticServiceId,
                        principalTable: "DiagnosticServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "DiagnosticServices",
                columns: new[] { "Id", "DailyCapacity", "Name" },
                values: new object[,]
                {
                    { 1, 8, "Complete Blood Count" },
                    { 2, 6, "Urinalysis" },
                    { 3, 4, "Chest X-Ray" },
                    { 4, 3, "ECG" }
                });

            migrationBuilder.InsertData(
                table: "Patients",
                columns: new[] { "Id", "FullName" },
                values: new object[,]
                {
                    { 1, "Mira Sample" },
                    { 2, "Theo Example" },
                    { 3, "Lina Demo" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingDate",
                table: "Bookings",
                column: "BookingDate");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PatientId",
                table: "Bookings",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingServices_DiagnosticServiceId",
                table: "BookingServices",
                column: "DiagnosticServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingServices");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "DiagnosticServices");

            migrationBuilder.DropTable(
                name: "Patients");
        }
    }
}
