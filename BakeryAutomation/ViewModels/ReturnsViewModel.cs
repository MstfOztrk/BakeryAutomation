using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BakeryAutomation.Models;
using BakeryAutomation.Services;
using Microsoft.EntityFrameworkCore;

namespace BakeryAutomation.ViewModels
{
    public sealed class ReturnReceiptRow
    {
        public ReturnReceipt Receipt { get; set; } = new();
        public string Display { get; set; } = "";
    }

    public sealed class SourceShipmentRow
    {
        public int Id { get; set; }
        public string Display { get; set; } = "";
    }

    public sealed class SourceShipmentItemRow
    {
        public int ShipmentId { get; set; }
        public int ShipmentItemId { get; set; }
        public string Display { get; set; } = "";
        public decimal AvailableQuantity { get; set; }
        public decimal EffectiveUnitPrice { get; set; }
        public ShipmentItem Item { get; set; } = new();
    }

    public sealed class ReturnsViewModel : ObservableObject
    {
        private readonly BakeryAppContext _ctx;
        private readonly int? _initialBranchId;
        private readonly bool _startInFreeReturnMode;

        public ObservableCollection<Branch> Branches { get; } = new();
        public ObservableCollection<Product> Products { get; } = new();
        public ObservableCollection<ReturnReceiptRow> Receipts { get; } = new();
        public ObservableCollection<ReturnReceiptItem> Items { get; } = new();
        public ObservableCollection<SourceShipmentRow> SourceShipments { get; } = new();
        public ObservableCollection<SourceShipmentItemRow> SourceShipmentItems { get; } = new();

        private ReturnReceiptRow? _selectedRow;
        public ReturnReceiptRow? SelectedRow
        {
            get => _selectedRow;
            set
            {
                if (!Set(ref _selectedRow, value)) return;
                SelectedReceipt = value?.Receipt;
            }
        }

        private ReturnReceipt? _selectedReceipt;
        public ReturnReceipt? SelectedReceipt
        {
            get => _selectedReceipt;
            set
            {
                if (!Set(ref _selectedReceipt, value)) return;
                LoadReceiptIntoForm();
                RaiseReceiptStateProperties();
            }
        }

        private ReturnReceiptItem? _selectedItem;
        public ReturnReceiptItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (!Set(ref _selectedItem, value)) return;
                Raise(nameof(HasSelectedItem));
                Raise(nameof(SelectedItemStatusText));
                Raise(nameof(RemoveItemButtonText));
                RemoveItemCommand.RaiseCanExecuteChanged();
            }
        }

        private string _returnNo = "";
        public string ReturnNo
        {
            get => _returnNo;
            set
            {
                if (!Set(ref _returnNo, value)) return;
                Raise(nameof(FormModeTitle));
            }
        }

        private DateTime _date = DateTime.Today;
        public DateTime Date { get => _date; set => Set(ref _date, value); }

        private int _branchId;
        public int BranchId
        {
            get => _branchId;
            set
            {
                if (!Set(ref _branchId, value)) return;
                RefreshSourceShipments();
            }
        }

        private string _notes = "";
        public string Notes { get => _notes; set => Set(ref _notes, value); }

        private DateTime _filterDate = DateTime.Today;
        public DateTime FilterDate
        {
            get => _filterDate;
            set
            {
                if (!Set(ref _filterDate, value)) return;
                Reload();
            }
        }

        private int _selectedSourceShipmentId;
        public int SelectedSourceShipmentId
        {
            get => _selectedSourceShipmentId;
            set
            {
                if (!Set(ref _selectedSourceShipmentId, value)) return;
                RefreshSourceShipmentItems();
                Raise(nameof(IsManualSourceMode));
                Raise(nameof(IsSourceBoundMode));
                Raise(nameof(HasSelectedSourceShipment));
            }
        }

        private int _selectedSourceShipmentItemId;
        public int SelectedSourceShipmentItemId
        {
            get => _selectedSourceShipmentItemId;
            set
            {
                if (!Set(ref _selectedSourceShipmentItemId, value)) return;
                ApplySourceShipmentItem();
                Raise(nameof(IsManualSourceMode));
                Raise(nameof(IsSourceBoundMode));
            }
        }

        private int _newProductId;
        public int NewProductId
        {
            get => _newProductId;
            set
            {
                if (!Set(ref _newProductId, value)) return;
                if (!IsManualSourceMode) return;

                var product = Products.FirstOrDefault(p => p.Id == value);
                if (product != null)
                {
                    NewUnitTypeDisplay = product.UnitTypeDisplay;
                }
            }
        }

        private decimal _newQuantity;
        public decimal NewQuantity { get => _newQuantity; set => Set(ref _newQuantity, value); }

        private decimal _newUnitPrice;
        public decimal NewUnitPrice { get => _newUnitPrice; set => Set(ref _newUnitPrice, value); }

        private string _newUnitTypeDisplay = "";
        public string NewUnitTypeDisplay { get => _newUnitTypeDisplay; set => Set(ref _newUnitTypeDisplay, value); }

        private decimal _selectedSourceAvailableQuantity;
        public decimal SelectedSourceAvailableQuantity
        {
            get => _selectedSourceAvailableQuantity;
            set => Set(ref _selectedSourceAvailableQuantity, value);
        }

        private decimal _receiptTotal;
        public decimal ReceiptTotal { get => _receiptTotal; set => Set(ref _receiptTotal, value); }

        private bool _isFreeReturnMode;
        public bool IsFreeReturnMode
        {
            get => _isFreeReturnMode;
            private set
            {
                if (!Set(ref _isFreeReturnMode, value)) return;
                Raise(nameof(IsManualSourceMode));
                Raise(nameof(IsSourceBoundMode));
                Raise(nameof(ReturnModeText));
                Raise(nameof(ReturnModeHint));
                Raise(nameof(ProductSelectorLabel));
                Raise(nameof(AddItemButtonText));
                Raise(nameof(SourceAvailabilityText));
            }
        }

        public bool IsManualSourceMode => IsFreeReturnMode;
        public bool IsSourceBoundMode => !IsFreeReturnMode;
        public bool HasSelectedSourceShipment => SelectedSourceShipmentId > 0;
        public bool HasSelectedItem => SelectedItem != null;
        public string FormModeTitle
        {
            get
            {
                if (SelectedReceipt?.Id > 0)
                {
                    var normalizedReturnNo = (ReturnNo ?? string.Empty).Trim();
                    return $"Duzenlenen Iade: {(normalizedReturnNo.Length > 0 ? normalizedReturnNo : SelectedReceipt.ReturnNo)}";
                }

                return "Yeni Iade Fisi";
            }
        }
        public string FormModeHint => SelectedReceipt?.Id > 0
            ? "Secili iade fisine yeni satir ekleyebilir, notu guncelleyebilir ve ayni fis uzerinden devam edebilirsiniz."
            : "Yeni iade fisindesiniz. Ilk satirdan sonra ayni fis acik kalir ve ayni kayit uzerinden devam edersiniz.";
        public string ReturnModeText => IsFreeReturnMode ? "Serbest iade" : "Fise bagli iade";
        public string ReturnModeHint => IsFreeReturnMode
            ? "Bu iade herhangi bir fise baglanmaz; dogrudan cari bakiyesinden duser."
            : "Kaynak sevkiyat secip musait miktar kadar iade dusun.";
        public string ProductSelectorLabel => IsFreeReturnMode ? "Urun" : "Kaynak urun";
        public string AddItemButtonText => IsFreeReturnMode ? "Serbest Iade Ekle" : "Iade Satiri Ekle";
        public string RemoveItemButtonText => SelectedItem == null
            ? "Satir Secip Sil"
            : $"Secili Satiri Sil: {SelectedItem.ProductName}";
        public string SelectedItemStatusText => SelectedItem == null
            ? "Yanlis eklenen iade satirini silmek icin once ortadaki tabloda satiri secin."
            : $"Secili satir: {SelectedItem.ProductName} | Miktar {SelectedItem.Quantity:G29}";
        public string SourceAvailabilityText => IsFreeReturnMode
            ? "Serbest iadede urun, miktar ve fiyat el ile girilir."
            : SelectedSourceShipmentItemId > 0
                ? $"Musait Iade: {SelectedSourceAvailableQuantity:G29}"
                : "Kaynak sevkiyat satiri secin.";

        public RelayCommand NewReceiptCommand { get; }
        public RelayCommand SaveReceiptCommand { get; }
        public RelayCommand DeleteReceiptCommand { get; }
        public RelayCommand RefreshCommand { get; }
        public RelayCommand PreviousDayCommand { get; }
        public RelayCommand NextDayCommand { get; }
        public RelayCommand AddItemCommand { get; }
        public RelayCommand RemoveItemCommand { get; }
        public RelayCommand SourceBoundModeCommand { get; }
        public RelayCommand FreeReturnModeCommand { get; }

        public ReturnsViewModel(BakeryAppContext ctx, int? initialBranchId = null, bool startInFreeReturnMode = false)
        {
            _ctx = ctx;
            _initialBranchId = initialBranchId;
            _startInFreeReturnMode = startInFreeReturnMode;

            NewReceiptCommand = new RelayCommand(_ => NewReceipt());
            SaveReceiptCommand = new RelayCommand(_ => SaveReceipt(), _ => SelectedReceipt != null);
            DeleteReceiptCommand = new RelayCommand(_ => DeleteReceipt(), _ => SelectedReceipt?.Id > 0);
            RefreshCommand = new RelayCommand(_ => Reload());
            PreviousDayCommand = new RelayCommand(_ => FilterDate = FilterDate.AddDays(-1));
            NextDayCommand = new RelayCommand(_ => FilterDate = FilterDate.AddDays(1));
            AddItemCommand = new RelayCommand(_ => AddItem(), _ => SelectedReceipt != null);
            RemoveItemCommand = new RelayCommand(_ => RemoveItem(), _ => SelectedItem != null);
            SourceBoundModeCommand = new RelayCommand(_ => ApplyReturnMode(false));
            FreeReturnModeCommand = new RelayCommand(_ => ApplyReturnMode(true));

            Reload();
            NewReceipt();
        }

        private void Reload(
            int? preferredReceiptId = null,
            bool preserveAddItemContext = false,
            bool? preferredFreeReturnMode = null,
            int? preferredSourceShipmentId = null,
            int? preferredSourceShipmentItemId = null)
        {
            Branches.Clear();
            Products.Clear();
            Receipts.Clear();

            var currentBranchId = SelectedReceipt?.BranchId ?? BranchId;

            var branches = _ctx.Db.Branches
                .AsNoTracking()
                .Where(b => b.IsActive || b.Id == currentBranchId)
                .OrderBy(b => b.Name)
                .ToList();

            foreach (var branch in branches)
            {
                Branches.Add(branch);
            }

            var products = _ctx.Db.Products
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToList();

            foreach (var product in products)
            {
                Products.Add(product);
            }

            var branchLookup = branches.ToDictionary(x => x.Id, x => x.Name);
            var receipts = _ctx.Db.ReturnReceipts
                .AsNoTracking()
                .Include(r => r.Items)
                .Where(r => r.Date == FilterDate.Date)
                .OrderByDescending(r => r.Id)
                .ToList();

            foreach (var receipt in receipts)
            {
                Receipts.Add(new ReturnReceiptRow
                {
                    Receipt = receipt,
                    Display = $"{receipt.Date:yyyy-MM-dd} | {ResolveBranchName(branchLookup, receipt.BranchId)} | {receipt.ReturnNo}"
                });
            }

            var targetReceiptId = preferredReceiptId ?? (SelectedReceipt?.Id > 0 ? SelectedReceipt.Id : (int?)null);
            if (targetReceiptId.HasValue)
            {
                var matchingRow = Receipts.FirstOrDefault(x => x.Receipt.Id == targetReceiptId.Value);
                if (matchingRow != null)
                {
                    SelectedRow = matchingRow;
                }
                else if (SelectedReceipt == null || SelectedReceipt.Id > 0)
                {
                    SelectedRow = Receipts.FirstOrDefault();
                }
            }
            else if (SelectedReceipt == null || SelectedReceipt.Id > 0)
            {
                SelectedRow = Receipts.FirstOrDefault();
            }

            if (preserveAddItemContext)
            {
                RestoreAddItemContext(
                    preferredFreeReturnMode ?? IsFreeReturnMode,
                    preferredSourceShipmentId,
                    preferredSourceShipmentItemId);
            }
        }

        private static string ResolveBranchName(IReadOnlyDictionary<int, string> lookup, int branchId)
        {
            return lookup.TryGetValue(branchId, out var name) ? name : "(Silinmis)";
        }

        private void NewReceipt()
        {
            SelectedRow = null;
            ApplyReturnMode(_startInFreeReturnMode);
            SelectedReceipt = new ReturnReceipt
            {
                Date = FilterDate.Date,
                BranchId = ResolveDefaultBranchId(),
                Notes = "",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        private void LoadReceiptIntoForm()
        {
            Items.Clear();
            SelectedItem = null;
            ResetAddItemPanel();

            if (SelectedReceipt == null)
            {
                ApplyReturnMode(_startInFreeReturnMode, clearAddItemPanel: false);
                ReturnNo = "";
                Date = FilterDate.Date;
                BranchId = ResolveDefaultBranchId();
                Notes = "";
                ReceiptTotal = 0m;
                return;
            }

            ApplyReturnMode(
                isFreeReturnMode: !SelectedReceipt.Items.Any(x => x.SourceShipmentItemId.HasValue && x.SourceShipmentItemId.Value > 0),
                clearAddItemPanel: false);
            ReturnNo = SelectedReceipt.ReturnNo;
            Date = SelectedReceipt.Date;
            BranchId = SelectedReceipt.BranchId;
            Notes = SelectedReceipt.Notes;

            var shipmentLookup = LoadSourceShipmentLookup(SelectedReceipt.Items);
            foreach (var item in SelectedReceipt.Items)
            {
                item.SourceShipmentDisplay = ResolveSourceShipmentDisplay(shipmentLookup, item.SourceShipmentId);
                Items.Add(item);
            }

            ReceiptTotal = Items.Sum(x => x.TotalLinePrice);
            RefreshSourceShipments();
        }

        private void RefreshSourceShipments()
        {
            SourceShipments.Clear();
            SourceShipmentItems.Clear();

            if (BranchId <= 0) return;

            var shipments = _ctx.Db.Shipments
                .AsNoTracking()
                .Where(s => s.BranchId == BranchId)
                .OrderByDescending(s => s.Date)
                .ThenByDescending(s => s.Id)
                .Select(s => new { s.Id, s.Date, s.BatchNo })
                .ToList();

            foreach (var shipment in shipments)
            {
                SourceShipments.Add(new SourceShipmentRow
                {
                    Id = shipment.Id,
                    Display = $"{shipment.Date:yyyy-MM-dd} | {shipment.BatchNo}"
                });
            }

            if (!SourceShipments.Any(x => x.Id == SelectedSourceShipmentId))
            {
                SelectedSourceShipmentId = 0;
            }
            else
            {
                RefreshSourceShipmentItems();
            }
        }

        private void RefreshSourceShipmentItems()
        {
            SourceShipmentItems.Clear();
            SelectedSourceShipmentItemId = 0;
            SelectedSourceAvailableQuantity = 0m;

            if (SelectedSourceShipmentId <= 0) return;

            var shipment = _ctx.Db.Shipments
                .AsNoTracking()
                .Include(s => s.Items)
                .FirstOrDefault(s => s.Id == SelectedSourceShipmentId);

            if (shipment == null || shipment.BranchId != BranchId) return;

            foreach (var item in shipment.Items.OrderBy(x => x.ProductName))
            {
                var available = _ctx.Calc.AvailableReturnQty(_ctx.Db, item);
                if (available <= 0) continue;

                var effectiveUnitPrice = _ctx.Calc.ItemUnitPriceAfterAllDiscounts(shipment, item);
                SourceShipmentItems.Add(new SourceShipmentItemRow
                {
                    ShipmentId = shipment.Id,
                    ShipmentItemId = item.Id,
                    AvailableQuantity = available,
                    EffectiveUnitPrice = effectiveUnitPrice,
                    Item = item,
                    Display = $"{item.ProductName} | Musait Iade: {available:G29} | {effectiveUnitPrice:n2}"
                });
            }
        }

        private void ApplySourceShipmentItem()
        {
            SelectedSourceAvailableQuantity = 0m;

            if (SelectedSourceShipmentItemId <= 0)
            {
                if (SelectedSourceShipmentId > 0 && !IsFreeReturnMode)
                {
                    NewProductId = 0;
                    NewUnitPrice = 0m;
                    NewUnitTypeDisplay = "";
                }

                return;
            }

            var sourceItem = SourceShipmentItems.FirstOrDefault(x => x.ShipmentItemId == SelectedSourceShipmentItemId);
            if (sourceItem == null) return;

            NewProductId = sourceItem.Item.ProductId;
            NewUnitPrice = sourceItem.EffectiveUnitPrice;
            NewUnitTypeDisplay = sourceItem.Item.UnitTypeDisplay;
            SelectedSourceAvailableQuantity = sourceItem.AvailableQuantity;
        }

        private int ResolveDefaultBranchId()
        {
            if (BranchId > 0 && Branches.Any(x => x.Id == BranchId))
            {
                return BranchId;
            }

            if (_initialBranchId.HasValue && Branches.Any(x => x.Id == _initialBranchId.Value))
            {
                return _initialBranchId.Value;
            }

            return Branches.FirstOrDefault()?.Id ?? 0;
        }

        private void ApplyReturnMode(bool isFreeReturnMode, bool clearAddItemPanel = true)
        {
            IsFreeReturnMode = isFreeReturnMode;

            if (clearAddItemPanel)
            {
                ResetAddItemPanel();
            }
        }

        private void ResetAddItemPanel()
        {
            SelectedSourceShipmentId = 0;
            SelectedSourceShipmentItemId = 0;
            NewProductId = 0;
            NewQuantity = 0m;
            NewUnitPrice = 0m;
            NewUnitTypeDisplay = "";
            SelectedSourceAvailableQuantity = 0m;
            Raise(nameof(HasSelectedSourceShipment));
        }

        private void SaveReceipt()
        {
            var preferredFreeReturnMode = IsFreeReturnMode;
            var preferredSourceShipmentId = SelectedSourceShipmentId > 0 ? SelectedSourceShipmentId : (int?)null;
            var preferredSourceShipmentItemId = SelectedSourceShipmentItemId > 0 ? SelectedSourceShipmentItemId : (int?)null;
            var persisted = PersistReceipt(requireItems: true);
            if (persisted == null) return;

            FilterDate = persisted.Date.Date;
            Reload(
                persisted.Id,
                preserveAddItemContext: true,
                preferredFreeReturnMode: preferredFreeReturnMode,
                preferredSourceShipmentId: preferredSourceShipmentId,
                preferredSourceShipmentItemId: preferredSourceShipmentItemId);
        }

        private ReturnReceipt? PersistReceipt(bool requireItems = false)
        {
            if (SelectedReceipt == null) return null;

            if (BranchId <= 0)
            {
                FailCommand("Iade icin sube secin.", _ctx.Loc["Confirm"]);
            }

            if (requireItems && Items.Count == 0)
            {
                FailCommand("Kaydetmeden once en az bir iade satiri ekleyin.", _ctx.Loc["Confirm"]);
            }

            var normalizedReturnNo = (ReturnNo ?? string.Empty).Trim();
            if (!ValidateReturnNumber(normalizedReturnNo))
            {
                return null;
            }

            ReturnReceipt? entity;
            if (SelectedReceipt.Id <= 0)
            {
                entity = new ReturnReceipt
                {
                    ReturnNo = normalizedReturnNo,
                    Date = Date.Date,
                    BranchId = BranchId,
                    Notes = (Notes ?? "").Trim(),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _ctx.Db.ReturnReceipts.Add(entity);
                _ctx.Save();

                if (string.IsNullOrWhiteSpace(entity.ReturnNo))
                {
                    entity.ReturnNo = $"I-{entity.Date:yyyyMMdd}-{entity.Id:0000}";
                }
            }
            else
            {
                entity = _ctx.Db.ReturnReceipts
                    .Include(r => r.Items)
                    .FirstOrDefault(r => r.Id == SelectedReceipt.Id);

                if (entity == null)
                {
                    FailCommand("Iade fisi bulunamadi.", _ctx.Loc["Confirm"]);
                }

                entity.ReturnNo = string.IsNullOrWhiteSpace(normalizedReturnNo) ? entity.ReturnNo : normalizedReturnNo;
                entity.Date = Date.Date;
                entity.BranchId = BranchId;
                entity.Notes = (Notes ?? "").Trim();
                entity.UpdatedAt = DateTime.Now;
            }

            _ctx.Save();
            return entity;
        }

        private void DeleteReceipt()
        {
            if (SelectedReceipt == null || SelectedReceipt.Id <= 0) return;

            if (!ConfirmCommand("Iade fisini silmek istiyor musunuz?", _ctx.Loc["Confirm"]))
            {
                return;
            }

            var entity = _ctx.Db.ReturnReceipts.FirstOrDefault(x => x.Id == SelectedReceipt.Id);
            if (entity != null)
            {
                _ctx.Db.ReturnReceipts.Remove(entity);
                _ctx.Save();
            }

            Reload();
            NewReceipt();
        }

        private void AddItem()
        {
            if (SelectedReceipt == null) return;

            if (NewQuantity <= 0)
            {
                FailCommand("Iade miktari sifirdan buyuk olmalidir.", _ctx.Loc["Confirm"]);
            }

            var preferredFreeReturnMode = IsFreeReturnMode;
            var preferredSourceShipmentId = SelectedSourceShipmentId > 0 ? SelectedSourceShipmentId : (int?)null;
            var preferredSourceShipmentItemId = SelectedSourceShipmentItemId > 0 ? SelectedSourceShipmentItemId : (int?)null;
            var receipt = PersistReceipt();
            if (receipt == null) return;

            var item = CreateReturnItem(receipt.Id);
            if (item == null) return;

            _ctx.Db.ReturnReceiptItems.Add(item);
            _ctx.Save();

            FilterDate = receipt.Date.Date;
            Reload(
                receipt.Id,
                preserveAddItemContext: true,
                preferredFreeReturnMode: preferredFreeReturnMode,
                preferredSourceShipmentId: preferredSourceShipmentId,
                preferredSourceShipmentItemId: preferredSourceShipmentItemId);
        }

        private ReturnReceiptItem? CreateReturnItem(int receiptId)
        {
            if (IsSourceBoundMode)
            {
                if (SelectedSourceShipmentId <= 0 || SelectedSourceShipmentItemId <= 0)
                {
                    FailCommand("Fise bagli iade icin once kaynak sevkiyat satiri secin.", _ctx.Loc["Confirm"]);
                }

                var shipment = _ctx.Db.Shipments
                    .AsNoTracking()
                    .Include(s => s.Items)
                    .FirstOrDefault(s => s.Id == SelectedSourceShipmentId);

                var sourceItem = shipment?.Items.FirstOrDefault(x => x.Id == SelectedSourceShipmentItemId);
                if (shipment == null || sourceItem == null)
                {
                    FailCommand("Kaynak sevkiyat satiri bulunamadi.", _ctx.Loc["Confirm"]);
                }

                if (shipment.BranchId != BranchId)
                {
                    FailCommand("Iade ve kaynak sevkiyat ayni subede olmalidir.", _ctx.Loc["Confirm"]);
                }

                var availableQuantity = _ctx.Calc.AvailableReturnQty(_ctx.Db, sourceItem);
                if (NewQuantity > availableQuantity)
                {
                    FailCommand($"Bu satir icin en fazla {availableQuantity:G29} iade alinabilir.", _ctx.Loc["Confirm"]);
                }

                return new ReturnReceiptItem
                {
                    ReturnReceiptId = receiptId,
                    ProductId = sourceItem.ProductId,
                    ProductName = sourceItem.ProductName,
                    UnitType = sourceItem.UnitType,
                    Quantity = NewQuantity,
                    UnitPrice = _ctx.Calc.ItemUnitPriceAfterAllDiscounts(shipment, sourceItem),
                    SourceShipmentId = shipment.Id,
                    SourceShipmentItemId = sourceItem.Id
                };
            }

            var product = _ctx.Db.Products
                .AsNoTracking()
                .FirstOrDefault(x => x.Id == NewProductId && x.IsActive);

            if (product == null)
            {
                FailCommand("Serbest iade icin urun secin.", _ctx.Loc["Confirm"]);
            }

            if (NewUnitPrice <= 0)
            {
                FailCommand("Serbest iadede birim fiyat sifirdan buyuk olmalidir.", _ctx.Loc["Confirm"]);
            }

            return new ReturnReceiptItem
            {
                ReturnReceiptId = receiptId,
                ProductId = product.Id,
                ProductName = product.Name,
                UnitType = product.UnitType,
                Quantity = NewQuantity,
                UnitPrice = NewUnitPrice
            };
        }

        private void RemoveItem()
        {
            if (SelectedItem == null) return;

            if (!ConfirmCommand("Iade satirini silmek istiyor musunuz?", _ctx.Loc["Confirm"]))
            {
                return;
            }

            var preferredFreeReturnMode = IsFreeReturnMode;
            var preferredSourceShipmentId = SelectedSourceShipmentId > 0 ? SelectedSourceShipmentId : (int?)null;
            var preferredSourceShipmentItemId = SelectedSourceShipmentItemId > 0 ? SelectedSourceShipmentItemId : (int?)null;
            if (SelectedItem.Id > 0)
            {
                var entity = _ctx.Db.ReturnReceiptItems.FirstOrDefault(x => x.Id == SelectedItem.Id);
                if (entity != null)
                {
                    _ctx.Db.ReturnReceiptItems.Remove(entity);
                    _ctx.Save();
                }
            }

            var receiptId = SelectedReceipt?.Id ?? 0;
            Reload(
                receiptId > 0 ? receiptId : null,
                preserveAddItemContext: receiptId > 0,
                preferredFreeReturnMode: preferredFreeReturnMode,
                preferredSourceShipmentId: preferredSourceShipmentId,
                preferredSourceShipmentItemId: preferredSourceShipmentItemId);
        }

        private void RestoreAddItemContext(
            bool preferredFreeReturnMode,
            int? preferredSourceShipmentId,
            int? preferredSourceShipmentItemId)
        {
            ApplyReturnMode(preferredFreeReturnMode, clearAddItemPanel: false);

            if (preferredFreeReturnMode)
            {
                SelectedSourceShipmentId = 0;
                SelectedSourceShipmentItemId = 0;
                SelectedSourceAvailableQuantity = 0m;
                return;
            }

            if (preferredSourceShipmentId.HasValue && SourceShipments.Any(x => x.Id == preferredSourceShipmentId.Value))
            {
                SelectedSourceShipmentId = preferredSourceShipmentId.Value;

                if (preferredSourceShipmentItemId.HasValue && SourceShipmentItems.Any(x => x.ShipmentItemId == preferredSourceShipmentItemId.Value))
                {
                    SelectedSourceShipmentItemId = preferredSourceShipmentItemId.Value;
                }
            }
        }

        private void RaiseReceiptStateProperties()
        {
            Raise(nameof(FormModeTitle));
            Raise(nameof(FormModeHint));
            Raise(nameof(HasSelectedItem));
            Raise(nameof(SelectedItemStatusText));
            Raise(nameof(RemoveItemButtonText));
            SaveReceiptCommand.RaiseCanExecuteChanged();
            DeleteReceiptCommand.RaiseCanExecuteChanged();
            AddItemCommand.RaiseCanExecuteChanged();
            RemoveItemCommand.RaiseCanExecuteChanged();
        }

        private bool ValidateReturnNumber(string normalizedReturnNo)
        {
            if (string.IsNullOrWhiteSpace(normalizedReturnNo))
            {
                return true;
            }

            var currentReceiptId = SelectedReceipt?.Id ?? 0;
            var duplicateExists = _ctx.Db.ReturnReceipts
                .AsNoTracking()
                .Select(x => new { x.Id, x.ReturnNo })
                .AsEnumerable()
                .Any(x =>
                    x.Id != currentReceiptId &&
                    string.Equals((x.ReturnNo ?? string.Empty).Trim(), normalizedReturnNo, StringComparison.CurrentCultureIgnoreCase));

            if (!duplicateExists)
            {
                return true;
            }

            FailCommand("Ayni numarayla baska bir iade fisi var.", _ctx.Loc["Confirm"]);
            return false;
        }

        private Dictionary<int, string> LoadSourceShipmentLookup(IEnumerable<ReturnReceiptItem> items)
        {
            var sourceShipmentIds = items
                .Where(x => x.SourceShipmentId.HasValue && x.SourceShipmentId.Value > 0)
                .Select(x => x.SourceShipmentId!.Value)
                .Distinct()
                .ToList();

            if (sourceShipmentIds.Count == 0)
            {
                return new Dictionary<int, string>();
            }

            return _ctx.Db.Shipments
                .AsNoTracking()
                .Where(x => sourceShipmentIds.Contains(x.Id))
                .Select(x => new { x.Id, x.BatchNo })
                .ToDictionary(x => x.Id, x => x.BatchNo);
        }

        private static string ResolveSourceShipmentDisplay(IReadOnlyDictionary<int, string> lookup, int? sourceShipmentId)
        {
            if (!sourceShipmentId.HasValue || sourceShipmentId.Value <= 0)
            {
                return string.Empty;
            }

            return lookup.TryGetValue(sourceShipmentId.Value, out var batchNo)
                ? batchNo
                : "(Silinmis Fis)";
        }
    }
}
