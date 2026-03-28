using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using BakeryAutomation.Models;

namespace BakeryAutomation.Services
{
    public class PrintService
    {
        public sealed class ShipmentDocumentLine
        {
            public string ProductName { get; init; } = string.Empty;
            public decimal Quantity { get; init; }
            public decimal UnitPrice { get; init; }
            public decimal Total { get; init; }
        }

        public sealed class ShipmentDocumentModel
        {
            public string BatchNo { get; init; } = string.Empty;
            public DateTime Date { get; init; }
            public string BranchName { get; init; } = string.Empty;
            public string Notes { get; init; } = string.Empty;
            public decimal Subtotal { get; init; }
            public decimal DiscountPercent { get; init; }
            public decimal Total { get; init; }
            public IReadOnlyList<ShipmentDocumentLine> Lines { get; init; } = Array.Empty<ShipmentDocumentLine>();
        }

        private readonly CalculationService _calc;

        public PrintService(CalculationService calc)
        {
            _calc = calc;
        }

        public ShipmentDocumentModel BuildShipmentDocument(ShipmentBatch shipment, string branchName)
        {
            var lines = new List<ShipmentDocumentLine>();
            foreach (var item in shipment.Items)
            {
                var qty = _calc.NetSoldQty(item);
                if (qty <= 0 && item.QuantityReturned == 0 && item.QuantityWasted == 0) continue;

                var price = _calc.ItemUnitPriceAfterItemDiscount(item);
                lines.Add(new ShipmentDocumentLine
                {
                    ProductName = item.ProductName,
                    Quantity = qty,
                    UnitPrice = price,
                    Total = qty * price
                });
            }

            var subtotal = _calc.ShipmentSubtotal(shipment);
            var discountPercent = ClampPercent(shipment.BatchDiscountPercent);
            var total = _calc.ShipmentTotal(shipment);

            return new ShipmentDocumentModel
            {
                BatchNo = shipment.BatchNo,
                Date = shipment.Date,
                BranchName = branchName,
                Notes = shipment.Notes,
                Subtotal = subtotal,
                DiscountPercent = discountPercent,
                Total = total,
                Lines = lines
            };
        }

        public string BuildShipmentExportText(ShipmentBatch shipment, string branchName)
        {
            var document = BuildShipmentDocument(shipment, branchName);
            var sb = new StringBuilder();
            sb.AppendLine("--------------------------------");
            sb.AppendLine("          SEVKIYAT FISI         ");
            sb.AppendLine("--------------------------------");
            sb.AppendLine($"Fis No: {document.BatchNo}");
            sb.AppendLine($"Tarih : {document.Date:dd.MM.yyyy}");
            sb.AppendLine($"Sube  : {document.BranchName}");
            sb.AppendLine("--------------------------------");
            sb.AppendLine(string.Format("{0,-20} {1,5} {2,8}", "URUN", "ADET", "TUTAR"));
            sb.AppendLine("--------------------------------");

            foreach (var line in document.Lines)
            {
                var name = line.ProductName.Length > 20 ? line.ProductName[..20] : line.ProductName;
                sb.AppendLine(string.Format("{0,-20} {1,5} {2,8:N2}", name, line.Quantity, line.Total));
            }

            sb.AppendLine("--------------------------------");
            sb.AppendLine($"Ara Toplam : {document.Subtotal:N2}");
            sb.AppendLine($"Iskonto %  : {document.DiscountPercent:N0}");
            sb.AppendLine($"TOPLAM     : {document.Total:N2} TL");
            sb.AppendLine("--------------------------------");
            sb.AppendLine($"Not: {document.Notes}");
            sb.AppendLine("--------------------------------");
            return sb.ToString();
        }

        public void PrintShipment(ShipmentBatch shipment, string branchName)
        {
            var document = BuildShipmentDocument(shipment, branchName);
            var doc = new FlowDocument
            {
                PagePadding = new Thickness(10),
                ColumnWidth = 300,
                PageWidth = 320,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11
            };

            doc.Blocks.Add(new Paragraph(new Run("FIRIN OTOMASYON"))
            {
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            });

            doc.Blocks.Add(new Paragraph(new Run($"Tarih: {document.Date:dd.MM.yyyy HH:mm}")) { TextAlignment = TextAlignment.Left, Margin = new Thickness(0) });
            doc.Blocks.Add(new Paragraph(new Run($"Fis No: {document.BatchNo}")) { TextAlignment = TextAlignment.Left, Margin = new Thickness(0) });
            doc.Blocks.Add(new Paragraph(new Run($"Sube: {document.BranchName}"))
            {
                TextAlignment = TextAlignment.Left,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 5, 0, 5)
            });

            doc.Blocks.Add(new Paragraph(new Run(new string('-', 35))) { Margin = new Thickness(0) });

            var table = new Table { CellSpacing = 0 };
            table.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1.3, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1.5, GridUnitType.Star) });

            var rowGroup = new TableRowGroup();
            var headerRow = new TableRow();
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Urun")) { FontWeight = FontWeights.Bold }));
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Ad")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center }));
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Fyt")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right }));
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Top")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right }));
            rowGroup.Rows.Add(headerRow);

            foreach (var line in document.Lines)
            {
                var row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(line.ProductName))));
                row.Cells.Add(new TableCell(new Paragraph(new Run(line.Quantity.ToString("G29"))) { TextAlignment = TextAlignment.Center }));
                row.Cells.Add(new TableCell(new Paragraph(new Run(line.UnitPrice.ToString("0.00"))) { TextAlignment = TextAlignment.Right }));
                row.Cells.Add(new TableCell(new Paragraph(new Run(line.Total.ToString("0.00"))) { TextAlignment = TextAlignment.Right }));
                rowGroup.Rows.Add(row);
            }

            table.RowGroups.Add(rowGroup);
            doc.Blocks.Add(table);
            doc.Blocks.Add(new Paragraph(new Run(new string('-', 35))) { Margin = new Thickness(0) });

            if (document.DiscountPercent > 0)
            {
                var discountAmount = document.Subtotal - document.Total;
                doc.Blocks.Add(new Paragraph(new Run($"Ara Toplam: {document.Subtotal:N2}")) { TextAlignment = TextAlignment.Right, Margin = new Thickness(0) });
                doc.Blocks.Add(new Paragraph(new Run($"Iskonto (%{document.DiscountPercent:0.##}): -{discountAmount:N2}")) { TextAlignment = TextAlignment.Right, Margin = new Thickness(0) });
            }

            doc.Blocks.Add(new Paragraph(new Run($"GENEL TOPLAM: {document.Total:N2} TL"))
            {
                TextAlignment = TextAlignment.Right,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 5, 0, 0)
            });

            doc.Blocks.Add(new Paragraph(new Run(" ")) { LineHeight = 20 });
            doc.Blocks.Add(new Paragraph(new Run("....................")) { TextAlignment = TextAlignment.Right });
            doc.Blocks.Add(new Paragraph(new Run("Teslim Alan")) { TextAlignment = TextAlignment.Right });

            var dlg = new PrintDialog();
            if (dlg.ShowDialog() == true)
            {
                dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, $"Fis_{document.BatchNo}");
            }
        }

        private static decimal ClampPercent(decimal value)
        {
            if (value < 0m) return 0m;
            if (value > 100m) return 100m;
            return value;
        }
    }
}
