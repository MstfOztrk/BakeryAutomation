using System;
using System.Globalization;
using System.IO;
using System.Linq;
using BakeryAutomation.Models;
using BakeryAutomation.Services;
using BakeryAutomation.ViewModels;

namespace BakeryAutomation.Tests
{
    public sealed class ShipmentDocumentRegressionTests
    {
        [Fact]
        public void BuildExportBatchText_UsesCurrentEditorValues_NotPersistedBatchHeaderValues()
        {
            var tempDirectory = CreateTempDirectory();

            try
            {
                var dbPath = Path.Combine(tempDirectory, "bakery.db");
                SeedShipment(dbPath, batchDiscountPercent: 0m, notes: "Eski not", batchNo: "F-ORJ");

                using var db = CreateDatabase(dbPath);
                var ctx = new BakeryAppContext(db);
                var viewModel = new ShipmentsViewModel(ctx);

                var selectedBatch = viewModel.SelectedBatch;
                Assert.NotNull(selectedBatch);
                Assert.Equal(0m, selectedBatch!.BatchDiscountPercent);

                viewModel.BatchNo = "F-YENI";
                viewModel.Notes = "Yeni not";
                viewModel.BatchDiscountPercent = 15m;

                var exportText = viewModel.BuildExportBatchText();
                var expectedTotalText = 85m.ToString("N2", CultureInfo.CurrentCulture);

                Assert.Contains("Fis No: F-YENI", exportText);
                Assert.Contains("Not: Yeni not", exportText);
                Assert.Contains($"TOPLAM     : {expectedTotalText} TL", exportText);
                Assert.DoesNotContain("Fis No: F-ORJ", exportText);
                Assert.DoesNotContain("Not: Eski not", exportText);
            }
            finally
            {
                TryDeleteDirectory(tempDirectory);
            }
        }

        [Fact]
        public void BuildShipmentExportText_ClampsOutOfRangeDiscounts_LikeCalculationService()
        {
            var printService = new PrintService(new CalculationService());
            var shipment = new ShipmentBatch
            {
                BatchNo = "F-CLAMP",
                Date = new DateTime(2026, 3, 26),
                Notes = "Clamp test",
                BatchDiscountPercent = 150m,
                Items =
                {
                    new ShipmentItem
                    {
                        ProductId = 1,
                        ProductName = "Somun",
                        UnitType = UnitType.Piece,
                        QuantitySent = 10m,
                        UnitPrice = 10m
                    }
                }
            };

            var document = printService.BuildShipmentDocument(shipment, "Merkez");
            var exportText = printService.BuildShipmentExportText(shipment, "Merkez");

            Assert.Equal(100m, document.DiscountPercent);
            Assert.Equal(0m, document.Total);
            Assert.Contains($"Iskonto %  : {100m.ToString("N0", CultureInfo.CurrentCulture)}", exportText);
            Assert.Contains($"TOPLAM     : {0m.ToString("N2", CultureInfo.CurrentCulture)} TL", exportText);
        }

        private static void SeedShipment(string dbPath, decimal batchDiscountPercent, string notes, string batchNo)
        {
            using var db = CreateDatabase(dbPath);
            db.Branches.Add(new Branch { Name = "Merkez" });
            db.Products.Add(new Product
            {
                Name = "Somun",
                UnitType = UnitType.Piece,
                DefaultUnitPrice = 10m,
                IsActive = true
            });
            db.SaveChanges();

            db.Shipments.Add(new ShipmentBatch
            {
                BatchNo = batchNo,
                BranchId = 1,
                Date = DateTime.Today,
                Notes = notes,
                BatchDiscountPercent = batchDiscountPercent,
                Items =
                {
                    new ShipmentItem
                    {
                        ProductId = 1,
                        ProductName = "Somun",
                        UnitType = UnitType.Piece,
                        QuantitySent = 10m,
                        UnitPrice = 10m
                    }
                }
            });
            db.SaveChanges();
        }

        private static AppDbContext CreateDatabase(string dbPath)
        {
            var db = new AppDbContext(dbPath);
            new DatabaseInitializationService().Initialize(db);
            return db;
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
