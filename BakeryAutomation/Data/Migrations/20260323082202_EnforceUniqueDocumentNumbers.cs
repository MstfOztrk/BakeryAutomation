using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BakeryAutomation.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueDocumentNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shipments_BatchNo",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_ReturnReceipts_ReturnNo",
                table: "ReturnReceipts");

            migrationBuilder.CreateIndex(
                name: "UX_Shipments_BatchNo",
                table: "Shipments",
                column: "BatchNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ReturnReceipts_ReturnNo",
                table: "ReturnReceipts",
                column: "ReturnNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Shipments_BatchNo",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "UX_ReturnReceipts_ReturnNo",
                table: "ReturnReceipts");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_BatchNo",
                table: "Shipments",
                column: "BatchNo");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnReceipts_ReturnNo",
                table: "ReturnReceipts",
                column: "ReturnNo");
        }
    }
}
