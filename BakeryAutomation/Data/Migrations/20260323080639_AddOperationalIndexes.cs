using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BakeryAutomation.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_Shipments_BatchNo ON Shipments (BatchNo);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_Shipments_BranchId_Date ON Shipments (BranchId, Date);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_ReturnReceipts_BranchId_Date ON ReturnReceipts (BranchId, Date);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_ReturnReceipts_ReturnNo ON ReturnReceipts (ReturnNo);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_ReturnReceiptItems_SourceShipmentId ON ReturnReceiptItems (SourceShipmentId);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_ReturnReceiptItems_SourceShipmentItemId ON ReturnReceiptItems (SourceShipmentItemId);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_Payments_BranchId_Date ON Payments (BranchId, Date);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_Payments_ShipmentId ON Payments (ShipmentId);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Shipments_BatchNo;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Shipments_BranchId_Date;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_ReturnReceipts_BranchId_Date;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_ReturnReceipts_ReturnNo;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_ReturnReceiptItems_SourceShipmentId;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_ReturnReceiptItems_SourceShipmentItemId;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Payments_BranchId_Date;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Payments_ShipmentId;");
        }
    }
}
