using System;
using System.IO;
using System.Linq;
using BakeryAutomation.Models;
using BakeryAutomation.Services;
using Microsoft.EntityFrameworkCore;

namespace BakeryAutomation.Tests
{
    public sealed class ShipmentIntegrityServiceTests
    {
        [Fact]
        public void ValidateDeletion_Fails_WhenShipmentHasLinkedPayments()
        {
            var tempDirectory = CreateTempDirectory();

            try
            {
                var dbPath = Path.Combine(tempDirectory, "bakery.db");
                using (var db = CreateDatabase(dbPath))
                {
                    var shipment = CreateShipment(quantity: 10m, unitPrice: 12m);
                    db.Shipments.Add(shipment);
                    db.SaveChanges();

                    db.Payments.Add(new Payment
                    {
                        BranchId = shipment.BranchId,
                        Date = shipment.Date,
                        Amount = 50m,
                        ShipmentId = shipment.Id
                    });
                    db.SaveChanges();

                    var service = new ShipmentIntegrityService(new CalculationService());
                    var result = service.ValidateDeletion(db, shipment.Id);

                    Assert.False(result.IsValid);
                    Assert.Contains("tahsilat", result.Message, StringComparison.OrdinalIgnoreCase);
                }
            }
            finally
            {
                TryDeleteDirectory(tempDirectory);
            }
        }

        [Fact]
        public void ValidateUpdate_Fails_WhenQuantityDropsBelowProtectedReturns()
        {
            var tempDirectory = CreateTempDirectory();

            try
            {
                var dbPath = Path.Combine(tempDirectory, "bakery.db");
                using (var db = CreateDatabase(dbPath))
                {
                    var shipment = CreateShipment(quantity: 10m, unitPrice: 10m);
                    shipment.Items[0].QuantityReturned = 1m;
                    shipment.Items[0].QuantityWasted = 2m;
                    db.Shipments.Add(shipment);
                    db.SaveChanges();

                    var receipt = new ReturnReceipt
                    {
                        ReturnNo = "I-1",
                        BranchId = shipment.BranchId,
                        Date = shipment.Date,
                        Items =
                        {
                            new ReturnReceiptItem
                            {
                                ProductId = shipment.Items[0].ProductId,
                                ProductName = shipment.Items[0].ProductName,
                                UnitType = shipment.Items[0].UnitType,
                                Quantity = 3m,
                                UnitPrice = 10m,
                                SourceShipmentId = shipment.Id,
                                SourceShipmentItemId = shipment.Items[0].Id
                            }
                        }
                    };

                    db.ReturnReceipts.Add(receipt);
                    db.SaveChanges();

                    var existingShipment = db.Shipments.Include(x => x.Items).Single();
                    var proposedItem = existingShipment.Items.Single();
                    proposedItem.QuantitySent = 5m;

                    var service = new ShipmentIntegrityService(new CalculationService());
                    var result = service.ValidateUpdate(
                        db,
                        existingShipment,
                        existingShipment.Items.ToList(),
                        existingShipment.BranchId,
                        existingShipment.BatchDiscountPercent);

                    Assert.False(result.IsValid);
                    Assert.Contains("korunan", result.Message, StringComparison.OrdinalIgnoreCase);
                }
            }
            finally
            {
                TryDeleteDirectory(tempDirectory);
            }
        }

        [Fact]
        public void ValidateUpdate_Fails_WhenProjectedTotalFallsBelowLinkedCredits()
        {
            var tempDirectory = CreateTempDirectory();

            try
            {
                var dbPath = Path.Combine(tempDirectory, "bakery.db");
                using (var db = CreateDatabase(dbPath))
                {
                    var shipment = CreateShipment(quantity: 10m, unitPrice: 10m);
                    db.Shipments.Add(shipment);
                    db.SaveChanges();

                    db.ReturnReceipts.Add(new ReturnReceipt
                    {
                        ReturnNo = "I-2",
                        BranchId = shipment.BranchId,
                        Date = shipment.Date,
                        Items =
                        {
                            new ReturnReceiptItem
                            {
                                ProductId = shipment.Items[0].ProductId,
                                ProductName = shipment.Items[0].ProductName,
                                UnitType = shipment.Items[0].UnitType,
                                Quantity = 2m,
                                UnitPrice = 10m,
                                SourceShipmentId = shipment.Id,
                                SourceShipmentItemId = shipment.Items[0].Id
                            }
                        }
                    });

                    db.Payments.Add(new Payment
                    {
                        BranchId = shipment.BranchId,
                        Date = shipment.Date,
                        Amount = 50m,
                        ShipmentId = shipment.Id
                    });

                    db.SaveChanges();

                    var existingShipment = db.Shipments.Include(x => x.Items).Single();
                    var proposedItem = existingShipment.Items.Single();
                    proposedItem.QuantitySent = 6m;

                    var service = new ShipmentIntegrityService(new CalculationService());
                    var result = service.ValidateUpdate(
                        db,
                        existingShipment,
                        existingShipment.Items.ToList(),
                        existingShipment.BranchId,
                        existingShipment.BatchDiscountPercent);

                    Assert.False(result.IsValid);
                    Assert.Contains("dusurulemez", result.Message, StringComparison.OrdinalIgnoreCase);
                }
            }
            finally
            {
                TryDeleteDirectory(tempDirectory);
            }
        }

        [Fact]
        public void ValidateUpdate_Fails_WhenRemovingItemWithLinkedSeparateReturn()
        {
            var tempDirectory = CreateTempDirectory();

            try
            {
                var dbPath = Path.Combine(tempDirectory, "bakery.db");
                using (var db = CreateDatabase(dbPath))
                {
                    var shipment = CreateShipment(quantity: 10m, unitPrice: 10m);
                    db.Shipments.Add(shipment);
                    db.SaveChanges();

                    db.ReturnReceipts.Add(new ReturnReceipt
                    {
                        ReturnNo = "I-REMOVE-1",
                        BranchId = shipment.BranchId,
                        Date = shipment.Date,
                        Items =
                        {
                            new ReturnReceiptItem
                            {
                                ProductId = shipment.Items[0].ProductId,
                                ProductName = shipment.Items[0].ProductName,
                                UnitType = shipment.Items[0].UnitType,
                                Quantity = 1m,
                                UnitPrice = 10m,
                                SourceShipmentId = shipment.Id,
                                SourceShipmentItemId = shipment.Items[0].Id
                            }
                        }
                    });
                    db.SaveChanges();

                    var existingShipment = db.Shipments.Include(x => x.Items).Single();
                    var service = new ShipmentIntegrityService(new CalculationService());
                    var result = service.ValidateUpdate(
                        db,
                        existingShipment,
                        Array.Empty<ShipmentItem>(),
                        existingShipment.BranchId,
                        existingShipment.BatchDiscountPercent);

                    Assert.False(result.IsValid);
                    Assert.Contains("silinemez", result.Message, StringComparison.OrdinalIgnoreCase);
                }
            }
            finally
            {
                TryDeleteDirectory(tempDirectory);
            }
        }

        [Fact]
        public void ValidateUpdate_Fails_WhenBranchChangesWithLinkedPayment()
        {
            var tempDirectory = CreateTempDirectory();

            try
            {
                var dbPath = Path.Combine(tempDirectory, "bakery.db");
                using (var db = CreateDatabase(dbPath))
                {
                    var shipment = CreateShipment(quantity: 10m, unitPrice: 10m);
                    db.Shipments.Add(shipment);
                    db.SaveChanges();

                    db.Payments.Add(new Payment
                    {
                        BranchId = shipment.BranchId,
                        Date = shipment.Date,
                        Amount = 10m,
                        ShipmentId = shipment.Id
                    });
                    db.SaveChanges();

                    var existingShipment = db.Shipments.Include(x => x.Items).Single();
                    var service = new ShipmentIntegrityService(new CalculationService());
                    var result = service.ValidateUpdate(
                        db,
                        existingShipment,
                        existingShipment.Items.ToList(),
                        proposedBranchId: shipment.BranchId + 1,
                        existingShipment.BatchDiscountPercent);

                    Assert.False(result.IsValid);
                    Assert.Contains("subesi degistirilemez", result.Message, StringComparison.OrdinalIgnoreCase);
                }
            }
            finally
            {
                TryDeleteDirectory(tempDirectory);
            }
        }

        private static AppDbContext CreateDatabase(string dbPath)
        {
            var db = new AppDbContext(dbPath);
            new DatabaseInitializationService().Initialize(db);
            return db;
        }

        private static ShipmentBatch CreateShipment(decimal quantity, decimal unitPrice)
        {
            return new ShipmentBatch
            {
                BatchNo = "F-1",
                BranchId = 1,
                Date = DateTime.Today,
                Items =
                {
                    new ShipmentItem
                    {
                        ProductId = 1,
                        ProductName = "Somun Ekmek",
                        UnitType = UnitType.Piece,
                        QuantitySent = quantity,
                        UnitPrice = unitPrice
                    }
                }
            };
        }

        private static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), $"BakeryAutomationTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            return path;
        }

        private static void TryDeleteDirectory(string path)
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    if (!Directory.Exists(path))
                    {
                        return;
                    }

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Directory.Delete(path, true);
                    return;
                }
                catch
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
        }
    }
}
