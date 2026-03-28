using System;
using System.Collections.Generic;
using System.Linq;
using BakeryAutomation.Models;
using Microsoft.EntityFrameworkCore;

namespace BakeryAutomation.Services
{
    public sealed class CalculationService
    {
        public decimal ResolveUnitPrice(AppDbContext db, int productId, int branchId)
        {
            var overridePrice = db.BranchPriceOverrides
                .FirstOrDefault(x => x.BranchId == branchId && x.ProductId == productId);
            if (overridePrice != null && overridePrice.UnitPrice > 0) return overridePrice.UnitPrice;

            var product = db.Products.FirstOrDefault(p => p.Id == productId);
            return product?.DefaultUnitPrice ?? 0m;
        }

        public decimal NetSoldQty(ShipmentItem item)
        {
            var net = item.QuantitySent - item.QuantityReturned - item.QuantityWasted;
            return net < 0 ? 0 : net;
        }

        public decimal ItemUnitPriceAfterItemDiscount(ShipmentItem item)
        {
            var disc = ClampPercent(item.ItemDiscountPercent);
            return item.UnitPrice * (1m - disc / 100m);
        }

        public decimal ItemUnitPriceAfterAllDiscounts(ShipmentBatch batch, ShipmentItem item)
        {
            var itemPrice = ItemUnitPriceAfterItemDiscount(item);
            var batchDiscount = ClampPercent(batch.BatchDiscountPercent);
            return itemPrice * (1m - batchDiscount / 100m);
        }

        public decimal ShipmentSubtotal(ShipmentBatch batch)
        {
            decimal total = 0m;
            for (int i = 0; i < batch.Items.Count; i++)
            {
                var it = batch.Items[i];
                total += NetSoldQty(it) * ItemUnitPriceAfterItemDiscount(it);
            }
            return total;
        }

        public decimal ShipmentTotal(ShipmentBatch batch)
        {
            var subtotal = ShipmentSubtotal(batch);
            var disc = ClampPercent(batch.BatchDiscountPercent);
            return subtotal * (1m - disc / 100m);
        }

        public decimal ReturnLineTotal(ReturnReceiptItem item) => item.Quantity * item.UnitPrice;

        public decimal ReturnTotal(ReturnReceipt receipt)
        {
            decimal total = 0m;
            for (int i = 0; i < receipt.Items.Count; i++)
            {
                total += ReturnLineTotal(receipt.Items[i]);
            }

            return total;
        }

        public decimal LinkedReturnAmountForShipment(AppDbContext db, int shipmentId)
        {
            return db.ReturnReceiptItems
                .Where(x => x.SourceShipmentId == shipmentId)
                .AsEnumerable()
                .Sum(x => x.Quantity * x.UnitPrice);
        }

        public decimal LinkedPaymentAmountForShipment(AppDbContext db, int shipmentId, int? excludePaymentId = null)
        {
            var query = db.Payments
                .AsNoTracking()
                .Where(x => x.ShipmentId == shipmentId);

            if (excludePaymentId.HasValue)
            {
                query = query.Where(x => x.Id != excludePaymentId.Value);
            }

            return query
                .AsEnumerable()
                .Sum(x => x.Amount);
        }

        public decimal ReturnedQtyFromSeparateReceipts(AppDbContext db, int shipmentItemId, int? excludeReturnItemId = null)
        {
            var query = db.ReturnReceiptItems.Where(x => x.SourceShipmentItemId == shipmentItemId);

            if (excludeReturnItemId.HasValue)
            {
                query = query.Where(x => x.Id != excludeReturnItemId.Value);
            }

            return query
                .AsEnumerable()
                .Sum(x => x.Quantity);
        }

        public decimal AvailableReturnQty(AppDbContext db, ShipmentItem shipmentItem, int? excludeReturnItemId = null)
        {
            var availableBeforeSeparateReturns = shipmentItem.QuantitySent - shipmentItem.QuantityReturned - shipmentItem.QuantityWasted;
            if (availableBeforeSeparateReturns <= 0) return 0m;

            var separateReturns = ReturnedQtyFromSeparateReceipts(db, shipmentItem.Id, excludeReturnItemId);
            var available = availableBeforeSeparateReturns - separateReturns;
            return available < 0 ? 0 : available;
        }

        public decimal BranchBalance(AppDbContext db, int branchId, DateTime? upTo = null)
        {
            return BuildBranchBalanceLookup(db, new[] { branchId }, upTo)
                .TryGetValue(branchId, out var balance)
                ? balance
                : 0m;
        }

        public Dictionary<int, decimal> BuildBranchBalanceLookup(
            AppDbContext db,
            IEnumerable<int> branchIds,
            DateTime? upTo = null)
        {
            var normalizedBranchIds = branchIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            var result = new Dictionary<int, decimal>();
            if (normalizedBranchIds.Count == 0)
            {
                return result;
            }

            var end = upTo?.Date ?? DateTime.MaxValue.Date;
            foreach (var branchId in normalizedBranchIds)
            {
                result[branchId] = 0m;
            }

            var shipments = db.Shipments
                .AsNoTracking()
                .Include(s => s.Items)
                .Where(s => normalizedBranchIds.Contains(s.BranchId) && s.Date <= end)
                .ToList();

            foreach (var shipment in shipments)
            {
                result[shipment.BranchId] += ShipmentTotal(shipment);
            }

            var returns = db.ReturnReceipts
                .AsNoTracking()
                .Include(r => r.Items)
                .Where(r => normalizedBranchIds.Contains(r.BranchId) && r.Date <= end)
                .ToList();

            foreach (var receipt in returns)
            {
                result[receipt.BranchId] -= ReturnTotal(receipt);
            }

            var payments = db.Payments
                .AsNoTracking()
                .Where(p => normalizedBranchIds.Contains(p.BranchId) && p.Date <= end)
                .ToList();

            foreach (var payment in payments)
            {
                result[payment.BranchId] -= payment.Amount;
            }

            return result;
        }

        private static decimal ClampPercent(decimal v)
        {
            if (v < 0) return 0;
            if (v > 100) return 100;
            return v;
        }
    }
}
