using System.Collections.Generic;
using System.Linq;
using BakeryAutomation.Models;

namespace BakeryAutomation.Services
{
    public sealed class ShipmentValidationResult
    {
        private ShipmentValidationResult(bool isValid, string message)
        {
            IsValid = isValid;
            Message = message;
        }

        public bool IsValid { get; }
        public string Message { get; }

        public static ShipmentValidationResult Success() => new(true, string.Empty);
        public static ShipmentValidationResult Fail(string message) => new(false, message);
    }

    public sealed class ShipmentIntegrityService
    {
        private readonly CalculationService _calc;

        public ShipmentIntegrityService(CalculationService calc)
        {
            _calc = calc;
        }

        public ShipmentValidationResult ValidateDeletion(AppDbContext db, int shipmentId)
        {
            var linkedPayments = db.Payments.Count(x => x.ShipmentId == shipmentId);
            var linkedReturns = db.ReturnReceiptItems.Count(x => x.SourceShipmentId == shipmentId);

            if (linkedPayments == 0 && linkedReturns == 0)
            {
                return ShipmentValidationResult.Success();
            }

            var reasons = new List<string>();
            if (linkedPayments > 0)
            {
                reasons.Add($"{linkedPayments} bagli tahsilat");
            }

            if (linkedReturns > 0)
            {
                reasons.Add($"{linkedReturns} bagli iade satiri");
            }

            return ShipmentValidationResult.Fail(
                $"Bu sevkiyat silinemez. Once {string.Join(" ve ", reasons)} kaydini temizleyin.");
        }

        public ShipmentValidationResult ValidateUpdate(
            AppDbContext db,
            ShipmentBatch existingBatch,
            IReadOnlyCollection<ShipmentItem> proposedItems,
            int proposedBranchId,
            decimal proposedBatchDiscountPercent)
        {
            if (existingBatch.Id <= 0)
            {
                return ShipmentValidationResult.Success();
            }

            var linkedPayments = db.Payments.Count(x => x.ShipmentId == existingBatch.Id);
            var linkedReturns = db.ReturnReceiptItems.Count(x => x.SourceShipmentId == existingBatch.Id);

            if (existingBatch.BranchId != proposedBranchId && (linkedPayments > 0 || linkedReturns > 0))
            {
                return ShipmentValidationResult.Fail(
                    "Bagli tahsilat veya iade kaydi olan sevkiyatin subesi degistirilemez.");
            }

            foreach (var item in proposedItems)
            {
                if (item.Id <= 0)
                {
                    continue;
                }

                var separateReturnQty = _calc.ReturnedQtyFromSeparateReceipts(db, item.Id);
                var protectedQuantity = item.QuantityReturned + item.QuantityWasted + separateReturnQty;

                if (item.QuantitySent < protectedQuantity)
                {
                    return ShipmentValidationResult.Fail(
                        $"'{item.ProductName}' satirinda gonderilen miktar, bagli iadeler ve zayi ile korunan {protectedQuantity:G29} miktarindan dusuk olamaz.");
                }
            }

            var proposedItemIds = proposedItems
                .Where(x => x.Id > 0)
                .Select(x => x.Id)
                .ToHashSet();

            foreach (var existingItem in existingBatch.Items.Where(x => x.Id > 0 && !proposedItemIds.Contains(x.Id)))
            {
                var separateReturnQty = _calc.ReturnedQtyFromSeparateReceipts(db, existingItem.Id);
                if (separateReturnQty <= 0)
                {
                    continue;
                }

                return ShipmentValidationResult.Fail(
                    $"'{existingItem.ProductName}' satiri silinemez; bu satira bagli {separateReturnQty:G29} miktarinda iade kaydi var.");
            }

            var projectedBatch = new ShipmentBatch
            {
                Id = existingBatch.Id,
                BranchId = proposedBranchId,
                BatchDiscountPercent = proposedBatchDiscountPercent,
                Items = proposedItems.ToList()
            };

            var projectedTotal = _calc.ShipmentTotal(projectedBatch);
            var linkedReturnAmount = _calc.LinkedReturnAmountForShipment(db, existingBatch.Id);
            var linkedPaymentAmount = _calc.LinkedPaymentAmountForShipment(db, existingBatch.Id);

            if (projectedTotal < linkedReturnAmount + linkedPaymentAmount)
            {
                var protectedTotal = linkedReturnAmount + linkedPaymentAmount;
                return ShipmentValidationResult.Fail(
                    $"Sevkiyat toplami {projectedTotal:n2} seviyesine dusurulemez. Bagli iade ve tahsilatlar toplamda {protectedTotal:n2} tutuyor.");
            }

            return ShipmentValidationResult.Success();
        }
    }
}
