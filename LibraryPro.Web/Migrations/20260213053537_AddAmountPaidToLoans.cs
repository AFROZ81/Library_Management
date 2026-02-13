using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryPro.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAmountPaidToLoans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                table: "BookLoans",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountPaid",
                table: "BookLoans");
        }
    }
}
