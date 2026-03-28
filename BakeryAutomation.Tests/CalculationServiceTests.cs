using System;
using System.Collections.Generic;
using System.IO;
using BakeryAutomation.Models;
using BakeryAutomation.Services;
using Microsoft.EntityFrameworkCore;

namespace BakeryAutomation.Tests
{
    public sealed class CalculationServiceTests
    {
        [Fact]
        public void BuildBranchBalanceLookup_CalculatesBalancesForMultipleBranches()
        {
            var tempDirectory = CreateTempDirectory();

            try
            {
                var dbPath = Path.Combine(tempDirectory, "bakery.db");
                using var db = CreateDatabase(dbPath);

                db.Branches.AddRange(
                    new Branch { Name = "Merkez" },
                    new Branch { Name = "Sahil" });

                var branchOneShipment = CreateShipment("F-100", 1, new DateTime(2026, 3, 20), 10m, 10m);
                var branchTwoShipment = CreateShipment("F-200", 2, new DateTime(2026, 3, 21), 6m, 10m);

                db.Shipments.AddRange(branchOneShipment, branchTwoShipment);
                db.SaveChanges();

                db.ReturnReceipts.Add(new ReturnReceipt
                {
                    ReturnNo = "I-100",
                    BranchId = 1,
                    Date = new DateTime(2026, 3, 21),
                    Items =
                    {
                        new ReturnReceiptItem
                        {
                            ProductId = branchOneShipment.Items[0].ProductId,
                            ProductName = branchOneShipment.Items[0].ProductName,
                            UnitType = branchOneShipment.Items[0].UnitType,
                            Quantity = 1m,
                            UnitPrice = 10m,
                            SourceShipmentId = branchOneShipment.Id,
                            SourceShipmentItemId = branchOneShipment.Items[0].Id
                        }
                    }
                });

                db.Payments.Add(new Payment
                {
                    BranchId = 1,
                    Date = new DateTime(2026, 3, 22),
                    Amount = 40m
                });

                db.SaveChanges();

                var service = new CalculationService();
                var result = service.BuildBranchBalanceLookup(db, new[] { 1, 2 });

                Assert.Equal(50m, result[1]);
                Assert.Equal(60m, result[2]);
            }
            finally
            {
                TryDeleteDirectory(tempDirectory);
            }
        }

        [Fact]
        public void BuildBranchBalanceLookup_RespectsUpToDate()
        {
            var tempDirectory = CreateTempDirectory();

            try
            {
                var dbPath = Path.Combine(tempDirectory, "bakery.db");
                using var db = CreateDatabase(dbPath);

                db.Branches.Add(new Branch { Name = "Merkez" });
                db.Shipments.Add(CreateShipment("F-300", 1, new DateTime(2026, 3, 20), 5m, 10m));
                db.Payments.Add(new Payment
                {
                    BranchId = 1,
                    Date = new DateTime(2026, 3, 25),
                    Amount = 15m
                });
                db.SaveChanges();

                var service = new CalculationService();
                var beforePayment = service.BuildBranchBalanceLookup(db, new[] { 1 }, new DateTime(2026, 3, 24));
                var afterPayment = service.BuildBranchBalanceLookup(db, new[] { 1 }, new DateTime(2026, 3, 25));

                Assert.Equal(50m, beforePayment[1]);
                Assert.Equal(35m, afterPayment[1]);
            }
            finally
            {
                TryDeleteDirectory(tempDirectory);
            }
        }

        [Fact]
        public void BuildBranchBalanceLookup_IncludesFreeReturnsWithoutSourceShipment()
        {
            var tempDirectory = CreateTempDirectory();

            try
            {
                var dbPath = Path.Combine(tempDirectory, "bakery.db");
                using var db = CreateDatabase(dbPath);

                db.Branches.Add(new Branch { Name = "Merkez" });
                db.Shipments.Add(CreateShipment("F-350", 1, new DateTime(2026, 3, 20), 10m, 10m));
                db.SaveChanges();

                db.ReturnReceipts.Add(new ReturnReceipt
                {
                    ReturnNo = "I-SERBEST-1",
                    BranchId = 1,
                    Date = new DateTime(2026, 3, 21),
                    Items =
                    {
                        new ReturnReceiptItem
                        {
                            ProductId = 1,
                            ProductName = "Somun Ekmek",
                            UnitType = UnitType.Piece,
                            Quantity = 2m,
                            UnitPrice = 10m
                        }
                    }
                });
                db.SaveChanges();

                var service = new CalculationService();
                var result = service.BuildBranchBalanceLookup(db, new[] { 1 });

                Assert.Equal(80m, result[1]);
            }
            finally
            {
                TryDeleteDirectory(tempDirectory);
            }
        }

        [Fact]
        public void ShipmentSubtotal_UsesNetSoldQty_AndClampsDiscounts()
        {
            var batch = new ShipmentBatch
            {
                Items = new List<ShipmentItem>
                {
                    new()
                    {
                        ProductId = 1,
                        ProductName = "Acma",
                        UnitType = UnitType.Piece,
                        QuantitySent = 10m,
                        QuantityReturned = 3m,
                        QuantityWasted = 2m,
                        UnitPrice = 12m,
                        ItemDiscountPercent = 25m
                    },
                    new()
                    {
                        ProductId = 2,
                        ProductName = "Pogaca",
                        UnitType = UnitType.Piece,
                        QuantitySent = 4m,
                        QuantityReturned = 5m,
                        UnitPrice = 100m,
                        ItemDiscountPercent = -10m
                    },
                    new()
                    {
                        ProductId = 3,
                        ProductName = "Simit",
                        UnitType = UnitType.Piece,
                        QuantitySent = 2m,
                        UnitPrice = 50m,
                        ItemDiscountPercent = 150m
                    }
                }
            };

            var service = new CalculationService();
            var subtotal = service.ShipmentSubtotal(batch);

            Assert.Equal(45m, subtotal);
        }

        [Fact]
        public void ShipmentTotal_ClampsBatchDiscountRange()
        {
            var service = new CalculationService();

            var batchWithNegativeDiscount = CreateShipment(
                "F-CLAMP-NEG",
                1,
                new DateTime(2026, 3, 23),
                5m,
                10m);
            batchWithNegativeDiscount.BatchDiscountPercent = -20m;

            var batchWithExcessiveDiscount = CreateShipment(
                "F-CLAMP-HIGH",
                1,
                new DateTime(2026, 3, 23),
                5m,
                10m);
            batchWithExcessiveDiscount.BatchDiscountPercent = 150m;

            Assert.Equal(50m, service.ShipmentTotal(batchWithNegativeDiscount));
            Assert.Equal(0m, service.ShipmentTotal(batchWithExcessiveDiscount));
        }

        [Fact]
        public void LinkedPaymentAmountForShipment_SumsPaymentsOnSqlite_AndSupportsExclusion()
        {
            var tempDirectory = CreateTempDirectory();

            try
            {
                var dbPath = Path.Combine(tempDirectory, "bakery.db");
                using var db = CreateDatabase(dbPath);

                db.Branches.Add(new Branch { Name = "Merkez" });

                var shipment = CreateShipment("F-400", 1, new DateTime(2026, 3, 24), 8m, 10m);
                db.Shipments.Add(shipment);
                db.SaveChanges();

                db.Payments.AddRange(
                    new Payment
                    {
                        BranchId = 1,
                        ShipmentId = shipment.Id,
                        Date = new DateTime(2026, 3, 24),
                        Amount = 25m
                    },
                    new Payment
                    {
                        BranchId = 1,
                        ShipmentId = shipment.Id,
                        Date = new DateTime(2026, 3, 24),
                        Amount = 15m
                    },
                    new Payment
                    {
                        BranchId = 1,
                        Date = new DateTime(2026, 3, 24),
                        Amount = 99m
                    });
                db.SaveChanges();

                var paymentToExclude = db.Payments.Single(x => x.Amount == 15m && x.ShipmentId == shipment.Id);
                var service = new CalculationService();

                var total = service.LinkedPaymentAmountForShipment(db, shipment.Id);
                var excluded = service.LinkedPaymentAmountForShipment(db, shipment.Id, paymentToExclude.Id);

                Assert.Equal(40m, total);
                Assert.Equal(25m, excluded);
            }
            finally
            {
                TryDeleteDirectory(tempDirectory);
            }
        }

        [Fact]
        public void LinkedReturnAmountForShipment_SumsLinkedReturnsAcrossReceipts_OnSqlite()
        {
            var tempDirectory = CreateTempDirectory();

            try
            {
                var dbPath = Path.Combine(tempDirectory, "bakery.db");
                using var db = CreateDatabase(dbPath);

                db.Branches.Add(new Branch { Name = "Merkez" });

                var shipment = CreateShipment("F-410", 1, new DateTime(2026, 3, 24), 8m, 10m);
                db.Shipments.Add(shipment);
                db.SaveChanges();

                db.ReturnReceipts.AddRange(
                    new ReturnReceipt
                    {
                        ReturnNo = "I-410-1",
                        BranchId = 1,
                        Date = new DateTime(2026, 3, 24),
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
                    },
                    new ReturnReceipt
                    {
                        ReturnNo = "I-410-2",
                        BranchId = 1,
                        Date = new DateTime(2026, 3, 25),
                        Items =
                        {
                            new ReturnReceiptItem
                            {
                                ProductId = shipment.Items[0].ProductId,
                                ProductName = shipment.Items[0].ProductName,
                                UnitType = shipment.Items[0].UnitType,
                                Quantity = 1m,
                                UnitPrice = 8m,
                                SourceShipmentId = shipment.Id,
                                SourceShipmentItemId = shipment.Items[0].Id
                            }
                        }
                    },
                    new ReturnReceipt
                    {
                        ReturnNo = "I-410-SERBEST",
                        BranchId = 1,
                        Date = new DateTime(2026, 3, 25),
                        Items =
                        {
                            new ReturnReceiptItem
                            {
                                ProductId = 99,
                                ProductName = "Serbest",
                                UnitType = UnitType.Piece,
                                Quantity = 20m,
                                UnitPrice = 20m
                            }
                        }
                    });
                db.SaveChanges();

                var service = new CalculationService();
                var total = service.LinkedReturnAmountForShipment(db, shipment.Id);

                Assert.Equal(28m, total);
            }
            finally
            {
                TryDeleteDirectory(tempDirectory);
            }
        }

        [Fact]
        public void AvailableReturnQty_SubtractsSeparateReturns_AndSupportsExclusion()
        {
            var tempDirectory = CreateTempDirectory();

            try
            {
                var dbPath = Path.Combine(tempDirectory, "bakery.db");
                using var db = CreateDatabase(dbPath);

                db.Branches.Add(new Branch { Name = "Merkez" });

                var shipment = CreateShipment("F-420", 1, new DateTime(2026, 3, 24), 10m, 10m);
                shipment.Items[0].QuantityReturned = 1m;
                shipment.Items[0].QuantityWasted = 2m;
                db.Shipments.Add(shipment);
                db.SaveChanges();

                db.ReturnReceipts.AddRange(
                    new ReturnReceipt
                    {
                        ReturnNo = "I-420-1",
                        BranchId = 1,
                        Date = new DateTime(2026, 3, 25),
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
                    },
                    new ReturnReceipt
                    {
                        ReturnNo = "I-420-2",
                        BranchId = 1,
                        Date = new DateTime(2026, 3, 25),
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

                var shipmentItem = db.Shipments
                    .Include(x => x.Items)
                    .Single(x => x.Id == shipment.Id)
                    .Items
                    .Single();
                var returnItemToExclude = db.ReturnReceiptItems.Single(x => x.Quantity == 1m);
                var service = new CalculationService();

                var totalAvailable = service.AvailableReturnQty(db, shipmentItem);
                var availableWithExclusion = service.AvailableReturnQty(db, shipmentItem, returnItemToExclude.Id);

                Assert.Equal(3m, totalAvailable);
                Assert.Equal(4m, availableWithExclusion);
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

        private static ShipmentBatch CreateShipment(string batchNo, int branchId, DateTime date, decimal quantity, decimal unitPrice)
        {
            return new ShipmentBatch
            {
                BatchNo = batchNo,
                BranchId = branchId,
                Date = date,
                Items = new List<ShipmentItem>
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
