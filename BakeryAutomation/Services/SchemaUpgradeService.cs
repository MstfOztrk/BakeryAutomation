using Microsoft.EntityFrameworkCore;

namespace BakeryAutomation.Services
{
    public sealed class SchemaUpgradeService
    {
        private readonly AppDbContext _db;

        public SchemaUpgradeService(AppDbContext db)
        {
            _db = db;
        }

        public void ApplyLegacyUpgrades()
        {
            _db.Database.ExecuteSqlRaw(
                """
                CREATE TABLE IF NOT EXISTS ReturnReceipts (
                    Id INTEGER NOT NULL CONSTRAINT PK_ReturnReceipts PRIMARY KEY AUTOINCREMENT,
                    ReturnNo TEXT NOT NULL,
                    Date TEXT NOT NULL,
                    BranchId INTEGER NOT NULL,
                    Notes TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );
                """);

            _db.Database.ExecuteSqlRaw(
                """
                CREATE TABLE IF NOT EXISTS ReturnReceiptItems (
                    Id INTEGER NOT NULL CONSTRAINT PK_ReturnReceiptItems PRIMARY KEY AUTOINCREMENT,
                    ReturnReceiptId INTEGER NOT NULL,
                    ProductId INTEGER NOT NULL,
                    ProductName TEXT NOT NULL,
                    UnitType INTEGER NOT NULL,
                    Quantity TEXT NOT NULL,
                    UnitPrice TEXT NOT NULL,
                    SourceShipmentId INTEGER NULL,
                    SourceShipmentItemId INTEGER NULL,
                    CONSTRAINT FK_ReturnReceiptItems_ReturnReceipts_ReturnReceiptId
                        FOREIGN KEY (ReturnReceiptId) REFERENCES ReturnReceipts (Id) ON DELETE CASCADE
                );
                """);

            _db.Database.ExecuteSqlRaw(
                "CREATE INDEX IF NOT EXISTS IX_ReturnReceipts_BranchId_Date ON ReturnReceipts (BranchId, Date);");
            _db.Database.ExecuteSqlRaw(
                "CREATE INDEX IF NOT EXISTS IX_ReturnReceiptItems_ReturnReceiptId ON ReturnReceiptItems (ReturnReceiptId);");
            _db.Database.ExecuteSqlRaw(
                "CREATE INDEX IF NOT EXISTS IX_ReturnReceiptItems_SourceShipmentId ON ReturnReceiptItems (SourceShipmentId);");
            _db.Database.ExecuteSqlRaw(
                "CREATE INDEX IF NOT EXISTS IX_ReturnReceiptItems_SourceShipmentItemId ON ReturnReceiptItems (SourceShipmentItemId);");
        }
    }
}
