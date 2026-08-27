using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryPro.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBarcodeToBooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "Books",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "Books");
        }
    }
}
