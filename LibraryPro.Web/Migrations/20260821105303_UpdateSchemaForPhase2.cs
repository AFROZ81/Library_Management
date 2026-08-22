using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryPro.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchemaForPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastRenewalDate",
                table: "BookLoans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RenewalCount",
                table: "BookLoans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BookReservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookId = table.Column<int>(type: "int", nullable: false),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    ReservationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    QueuePosition = table.Column<int>(type: "int", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookReservations_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookReservations_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LibrarySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DailyFineRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DefaultLoanPeriodDays = table.Column<int>(type: "int", nullable: false),
                    MaxBooksPerMember = table.Column<int>(type: "int", nullable: false),
                    MaxRenewalAttempts = table.Column<int>(type: "int", nullable: false),
                    GracePeriodDays = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibrarySettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookReservations_BookId",
                table: "BookReservations",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_BookReservations_MemberId",
                table: "BookReservations",
                column: "MemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookReservations");

            migrationBuilder.DropTable(
                name: "LibrarySettings");

            migrationBuilder.DropColumn(
                name: "LastRenewalDate",
                table: "BookLoans");

            migrationBuilder.DropColumn(
                name: "RenewalCount",
                table: "BookLoans");
        }
    }
}
