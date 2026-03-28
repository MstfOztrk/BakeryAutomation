using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BakeryAutomation.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVatRateFromProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
