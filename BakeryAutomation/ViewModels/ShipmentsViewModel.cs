using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using BakeryAutomation.Models;
using BakeryAutomation.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace BakeryAutomation.ViewModels
{
    public sealed class ShipmentBatchRow
    {
        public ShipmentBatch Batch { get; set; } = new();
        public string Display { get; set; } = "";
    }

    public sealed class ShipmentsViewModel : ObservableObject
    {
        private readonly BakeryAppContext _ctx;
        private readonly DispatcherTimer _itemAddFeedbackTimer;
        private readonly DispatcherTimer _saveBatchFeedbackTimer;

        public ObservableCollection<Branch> Branches { get; } = new();
        public ObservableCollection<Product> Products { get; } = new();
        public ObservableCollection<ShipmentBatchRow> Batches { get; } = new();
        public ObservableCollection<ShipmentItem> Items { get; } = new();

        private ShipmentBatchRow? _selectedRow;
        public ShipmentBatchRow? SelectedRow
        {
            get => _selectedRow;
            set
            {
                if (!Set(ref _selectedRow, value)) return;
                SelectedBatch = value?.Batch;
            }
        }

        private ShipmentBatch? _selectedBatch;
        public ShipmentBatch? SelectedBatch
        {
            get => _selectedBatch;
            set
            {
                if (!Set(ref _selectedBatch, value)) return;
                LoadBatchIntoForm();
                RaiseBatchModeProperties();
            }
        }

        private string _batchNo = "";
        public string BatchNo
        {
            get => _batchNo;
            set
            {
                if (!Set(ref _batchNo, value)) return;
                Raise(nameof(BatchStatusText));
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
                FillUnitPrice();
            }
        }

        private decimal _batchDiscountPercent;
        public decimal BatchDiscountPercent { get => _batchDiscountPercent; set => Set(ref _batchDiscountPercent, value); }

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

        private ShipmentItem? _selectedItem;
        public ShipmentItem? SelectedItem
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

        private int _newProductId;
        public int NewProductId
        {
            get => _newProductId;
            set
            {
                if (!Set(ref _newProductId, value)) return;
                FillUnitPrice();
            }
        }

        private decimal _newQtySent;
        public decimal NewQtySent { get => _newQtySent; set => Set(ref _newQtySent, value); }

        private decimal _newUnitPrice;
        public decimal NewUnitPrice { get => _newUnitPrice; set => Set(ref _newUnitPrice, value); }

        private decimal _newItemDiscountPercent;
        public decimal NewItemDiscountPercent { get => _newItemDiscountPercent; set => Set(ref _newItemDiscountPercent, value); }

        private decimal _subtotal;
        public decimal Subtotal { get => _subtotal; set => Set(ref _subtotal, value); }

        private decimal _total;
        public decimal Total { get => _total; set => Set(ref _total, value); }

        private decimal _totalGross;
        public decimal TotalGross { get => _totalGross; set => Set(ref _totalGross, value); }

        private decimal _totalDiscount;
        public decimal TotalDiscount { get => _totalDiscount; set => Set(ref _totalDiscount, value); }

        private string _itemAddFeedback = "";
        public string ItemAddFeedback
        {
            get => _itemAddFeedback;
            set
            {
                if (!Set(ref _itemAddFeedback, value)) return;
                Raise(nameof(HasItemAddFeedback));
            }
        }

        public bool HasItemAddFeedback => !string.IsNullOrWhiteSpace(ItemAddFeedback);

        private bool _isItemAddSuccessActive;
        public bool IsItemAddSuccessActive
        {
            get => _isItemAddSuccessActive;
            set
            {
                if (!Set(ref _isItemAddSuccessActive, value)) return;
                Raise(nameof(AddItemButtonText));
            }
        }

        private string _saveBatchFeedback = "";
        public string SaveBatchFeedback
        {
            get => _saveBatchFeedback;
            set
            {
                if (!Set(ref _saveBatchFeedback, value)) return;
                Raise(nameof(HasSaveBatchFeedback));
            }
        }

        public bool HasSaveBatchFeedback => !string.IsNullOrWhiteSpace(SaveBatchFeedback);

        private bool _isSaveBatchSuccessActive;
        public bool IsSaveBatchSuccessActive
        {
            get => _isSaveBatchSuccessActive;
            set
            {
                if (!Set(ref _isSaveBatchSuccessActive, value)) return;
                Raise(nameof(SaveBatchButtonText));
            }
        }

        private bool _lastSaveWasUpdate;

        public bool IsExistingBatch => SelectedBatch?.Id > 0;
        public bool HasSelectedItem => SelectedItem != null;
        public string BatchEditorTitle => IsExistingBatch ? "Fisi Duzenle" : "Yeni Fis";
        public string BatchEditorHint => IsExistingBatch
            ? "Secili fiste sube, iskonto ve notu degistirip Fisi Guncelle diyebilirsiniz."
            : "Yeni sevkiyat taslak acilir. Ilk kayitta fis olusur.";
        public string SaveBatchButtonText => IsSaveBatchSuccessActive
            ? (_lastSaveWasUpdate ? "Guncellendi" : "Kaydedildi")
            : (IsExistingBatch ? "Fisi Guncelle" : "Fisi Kaydet");
        public string AddItemButtonText => IsItemAddSuccessActive ? "Eklendi" : "Urun Ekle";
        public string RemoveItemButtonText => SelectedItem == null
            ? "Satir Secip Sil"
            : $"Secili Urunu Sil: {SelectedItem.ProductName}";
        public string BatchStatusText => IsExistingBatch
            ? $"Secili fis: {BatchNo}"
            : "Bu fis henuz kaydedilmedi.";
        public string SelectedItemStatusText => SelectedItem == null
            ? "Yanlis eklenen urunu silmek icin once ortadaki tabloda satiri secin."
            : $"Secili satir: {SelectedItem.ProductName} | Miktar {SelectedItem.QuantitySent:G}";

        public RelayCommand NewBatchCommand { get; }
        public RelayCommand SaveBatchCommand { get; }
        public RelayCommand DeleteBatchCommand { get; }
        public RelayCommand RefreshCommand { get; }
        public RelayCommand PrintCommand { get; }
        public RelayCommand ExportBatchCommand { get; }
        public RelayCommand AddItemCommand { get; }
        public RelayCommand RemoveItemCommand { get; }
        public RelayCommand RecalculateCommand { get; }
        public RelayCommand PreviousDayCommand { get; }
        public RelayCommand NextDayCommand { get; }

        public ShipmentsViewModel(BakeryAppContext ctx)
        {
            _ctx = ctx;
            _itemAddFeedbackTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2.5)
            };
            _itemAddFeedbackTimer.Tick += (_, _) => ClearItemAddFeedback();
            _saveBatchFeedbackTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2.5)
            };
            _saveBatchFeedbackTimer.Tick += (_, _) => ClearSaveBatchFeedback();

            NewBatchCommand = new RelayCommand(_ => NewBatch());
            SaveBatchCommand = new RelayCommand(_ => SaveBatch(), _ => SelectedBatch != null);
            DeleteBatchCommand = new RelayCommand(_ => DeleteBatch(), _ => SelectedBatch?.Id > 0);
            RefreshCommand = new RelayCommand(_ => Reload());
            PrintCommand = new RelayCommand(_ => PrintBatch(), _ => SelectedBatch?.Id > 0);
            ExportBatchCommand = new RelayCommand(_ => ExportBatch(), _ => SelectedBatch?.Id > 0);
            AddItemCommand = new RelayCommand(_ => AddItem(), _ => SelectedBatch != null);
            RemoveItemCommand = new RelayCommand(_ => RemoveItem(), _ => SelectedItem != null);
            RecalculateCommand = new RelayCommand(_ => Recalculate());
            PreviousDayCommand = new RelayCommand(_ => FilterDate = FilterDate.AddDays(-1));
            NextDayCommand = new RelayCommand(_ => FilterDate = FilterDate.AddDays(1));

            Reload();
            if (!Batches.Any())
            {
                NewBatch();
            }
        }

        private void Reload(int? preferredBatchId = null)
        {
            Branches.Clear();
            Products.Clear();
            Batches.Clear();

            var branches = _ctx.Db.Branches
                .AsNoTracking()
                .OrderBy(b => b.Name)
                .ToList();

            foreach (var branch in branches) Branches.Add(branch);

            var products = _ctx.Db.Products
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToList();

            foreach (var product in products) Products.Add(product);

            var branchLookup = branches.ToDictionary(x => x.Id, x => x.Name);
            var batches = _ctx.Db.Shipments
                .Include(s => s.Items)
                .Where(s => s.Date == FilterDate.Date)
                .OrderByDescending(s => s.Id)
                .ToList();

            foreach (var batch in batches)
            {
                var branchName = branchLookup.TryGetValue(batch.BranchId, out var name) ? name : "(Silinmis)";
                Batches.Add(new ShipmentBatchRow
                {
                    Batch = batch,
                    Display = $"{batch.Date:yyyy-MM-dd} | {branchName} | {batch.BatchNo}"
                });
            }

            var targetBatchId = preferredBatchId ?? (SelectedBatch?.Id > 0 ? SelectedBatch.Id : (int?)null);
            if (targetBatchId.HasValue)
            {
                var matchingRow = Batches.FirstOrDefault(x => x.Batch.Id == targetBatchId.Value);
                if (matchingRow != null)
                {
                    SelectedRow = matchingRow;
                    return;
                }
            }

            if (SelectedBatch == null || SelectedBatch.Id > 0)
            {
                SelectedRow = Batches.FirstOrDefault();
            }
        }

        private void NewBatch()
        {
            ClearItemAddFeedback();
            ClearSaveBatchFeedback();
            SelectedRow = null;
            SelectedBatch = new ShipmentBatch
            {
                Date = FilterDate.Date,
                BranchId = Branches.FirstOrDefault()?.Id ?? 0,
                BatchDiscountPercent = 0m,
                Notes = "",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        private void LoadBatchIntoForm()
        {
            ClearItemAddFeedback();
            ClearSaveBatchFeedback();
            Items.Clear();
            SelectedItem = null;
            ClearAddItemPanel();

            if (SelectedBatch == null)
            {
                BatchNo = "";
                Date = FilterDate.Date;
                BranchId = Branches.FirstOrDefault()?.Id ?? 0;
                Notes = "";
                BatchDiscountPercent = 0m;
                Subtotal = 0m;
                Total = 0m;
                TotalGross = 0m;
                TotalDiscount = 0m;
                RaiseBatchModeProperties();
                return;
            }

            BatchNo = SelectedBatch.BatchNo;
            Date = SelectedBatch.Date;
            BranchId = SelectedBatch.BranchId;
            Notes = SelectedBatch.Notes;
            BatchDiscountPercent = SelectedBatch.BatchDiscountPercent;

            foreach (var item in SelectedBatch.Items)
            {
                item.PropertyChanged -= Item_PropertyChanged;
                item.PropertyChanged += Item_PropertyChanged;
                Items.Add(item);
            }

            Recalculate();
            RaiseBatchModeProperties();
        }

        private void ClearAddItemPanel()
        {
            NewProductId = 0;
            NewQtySent = 0m;
            NewUnitPrice = 0m;
            NewItemDiscountPercent = 0m;
        }

        private void Item_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is ShipmentItem item)
            {
                NormalizeEditableItem(item);
            }

            Recalculate();
        }

        private ShipmentBatch? PersistBatch(bool requireItems = false, bool validateCreditLimit = true)
        {
            if (SelectedBatch == null) return null;

            if (BranchId <= 0)
            {
                FailCommand("Sevkiyat icin sube secin.", _ctx.Loc["Confirm"]);
            }

            if (BatchDiscountPercent < 0 || BatchDiscountPercent > 100)
            {
                FailCommand("Genel iskonto 0-100 arasinda olmalidir.", _ctx.Loc["Confirm"]);
            }

            if (requireItems && Items.Count == 0)
            {
                FailCommand("Kaydetmeden once en az bir urun ekleyin.", _ctx.Loc["Confirm"]);
            }

            if (!ValidateItems()) return null;
            if (!ValidateBatchNumber()) return null;

            ShipmentBatch? entity;
            if (SelectedBatch.Id <= 0)
            {
                var proposedBatch = BuildWorkingBatch();
                if (validateCreditLimit && !ConfirmCreditLimitForBatch(proposedBatch))
                {
                    return null;
                }

                entity = new ShipmentBatch
                {
                    BatchNo = "",
                    Date = Date.Date,
                    BranchId = BranchId,
                    Notes = (Notes ?? "").Trim(),
                    BatchDiscountPercent = BatchDiscountPercent,
                    Items = Items.ToList(),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _ctx.Db.Shipments.Add(entity);
                _ctx.Save();

                entity.BatchNo = string.IsNullOrWhiteSpace(BatchNo)
                    ? $"F-{entity.Date:yyyyMMdd}-{entity.Id:0000}"
                    : BatchNo.Trim();
            }
            else
            {
                entity = _ctx.Db.Shipments
                    .Include(s => s.Items)
                    .FirstOrDefault(s => s.Id == SelectedBatch.Id);

                if (entity == null)
                {
                    FailCommand("Sevkiyat fisi bulunamadi.", _ctx.Loc["Confirm"]);
                }

                var validation = _ctx.ShipmentIntegrity.ValidateUpdate(
                    _ctx.Db,
                    entity,
                    Items.ToList(),
                    BranchId,
                    BatchDiscountPercent);

                if (!validation.IsValid)
                {
                    FailCommand(validation.Message, _ctx.Loc["Confirm"]);
                }

                var proposedBatch = BuildWorkingBatch();
                if (validateCreditLimit && !ConfirmCreditLimitForBatch(proposedBatch, entity))
                {
                    return null;
                }

                entity.BatchNo = string.IsNullOrWhiteSpace(BatchNo) ? entity.BatchNo : BatchNo.Trim();
                entity.Date = Date.Date;
                entity.BranchId = BranchId;
                entity.Notes = (Notes ?? "").Trim();
                entity.BatchDiscountPercent = BatchDiscountPercent;
                entity.Items = Items.ToList();
                entity.UpdatedAt = DateTime.Now;
            }

            _ctx.Save();
            return entity;
        }

        private void SaveBatch()
        {
            var wasExistingBatch = SelectedBatch?.Id > 0;
            var persisted = PersistBatch(requireItems: true);
            if (persisted == null) return;

            FilterDate = persisted.Date.Date;
            Reload(persisted.Id);
            ShowSaveBatchFeedback(
                wasExistingBatch ? "Fis guncellendi." : "Fis kaydedildi.",
                wasExistingBatch);
        }

        private void DeleteBatch()
        {
            if (SelectedBatch == null || SelectedBatch.Id <= 0) return;

            var validation = _ctx.ShipmentIntegrity.ValidateDeletion(_ctx.Db, SelectedBatch.Id);
            if (!validation.IsValid)
            {
                FailCommand(validation.Message, _ctx.Loc["Confirm"]);
            }

            if (!ConfirmCommand(_ctx.Loc["ConfirmDeleteShipment"], _ctx.Loc["Confirm"]))
            {
                return;
            }

            var entity = _ctx.Db.Shipments.FirstOrDefault(x => x.Id == SelectedBatch.Id);
            if (entity != null)
            {
                _ctx.Db.Shipments.Remove(entity);
                _ctx.Save();
            }

            Reload();
            NewBatch();
        }

        private void PrintBatch()
        {
            if (SelectedBatch == null || SelectedBatch.Id <= 0) return;

            var batch = BuildCurrentBatchSnapshot();
            var branchName = Branches.FirstOrDefault(b => b.Id == batch.BranchId)?.Name ?? "";
            _ctx.Print.PrintShipment(batch, branchName);
        }

        private void ExportBatch()
        {
            if (SelectedBatch == null || SelectedBatch.Id <= 0) return;

            var dialog = new SaveFileDialog
            {
                FileName = $"Fis_{SelectedBatch.BatchNo}.txt",
                DefaultExt = ".txt",
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
            {
                CancelCommand();
                return;
            }

            File.WriteAllText(dialog.FileName, BuildExportBatchText());
        }

        private void FillUnitPrice()
        {
            if (NewProductId <= 0 || BranchId <= 0)
            {
                if (NewProductId <= 0) NewUnitPrice = 0m;
                return;
            }

            NewUnitPrice = _ctx.Calc.ResolveUnitPrice(_ctx.Db, NewProductId, BranchId);
        }

        private void AddItem()
        {
            if (SelectedBatch == null) return;

            if (NewProductId <= 0 || NewQtySent <= 0)
            {
                FailCommand("Urun ve miktar girin.", _ctx.Loc["Confirm"]);
            }

            if (NewUnitPrice < 0)
            {
                FailCommand("Birim fiyat negatif olamaz.", _ctx.Loc["Confirm"]);
            }

            if (NewItemDiscountPercent < 0 || NewItemDiscountPercent > 100)
            {
                FailCommand("Satir iskontosu 0-100 arasinda olmalidir.", _ctx.Loc["Confirm"]);
            }

            var product = _ctx.Db.Products.FirstOrDefault(p => p.Id == NewProductId && p.IsActive);
            if (product == null)
            {
                FailCommand("Secilen urun bulunamadi ya da pasif.", _ctx.Loc["Confirm"]);
            }

            var unitPrice = NewUnitPrice > 0 ? NewUnitPrice : _ctx.Calc.ResolveUnitPrice(_ctx.Db, NewProductId, BranchId);
            var proposedItems = Items.Select(CloneShipmentItem).ToList();
            proposedItems.Add(new ShipmentItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitType = product.UnitType,
                QuantitySent = NewQtySent,
                UnitPrice = unitPrice,
                ItemDiscountPercent = NewItemDiscountPercent,
                QuantityReturned = 0m,
                QuantityWasted = 0m
            });

            var existingBatch = SelectedBatch.Id > 0
                ? _ctx.Db.Shipments
                    .Include(x => x.Items)
                    .FirstOrDefault(x => x.Id == SelectedBatch.Id)
                : null;

            if (!ConfirmCreditLimitForBatch(BuildWorkingBatch(proposedItems), existingBatch))
            {
                return;
            }

            var batch = PersistBatch(validateCreditLimit: false);
            if (batch == null) return;

            batch.Items.Add(new ShipmentItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitType = product.UnitType,
                QuantitySent = NewQtySent,
                UnitPrice = unitPrice,
                ItemDiscountPercent = NewItemDiscountPercent,
                QuantityReturned = 0m,
                QuantityWasted = 0m
            });

            _ctx.Save();
            FilterDate = batch.Date.Date;
            Reload(batch.Id);
            ShowItemAddFeedback($"{product.Name} fise eklendi.");
        }

        private void RemoveItem()
        {
            if (SelectedItem == null || SelectedBatch == null) return;

            if (!ConfirmCommand(_ctx.Loc["ConfirmDeleteRow"], _ctx.Loc["Confirm"]))
            {
                return;
            }

            if (SelectedBatch.Id > 0 && SelectedItem.Id > 0)
            {
                var entityBatch = _ctx.Db.Shipments
                    .Include(x => x.Items)
                    .FirstOrDefault(x => x.Id == SelectedBatch.Id);

                if (entityBatch != null)
                {
                    var remainingItems = Items
                        .Where(x => x.Id != SelectedItem.Id)
                        .ToList();

                    var validation = _ctx.ShipmentIntegrity.ValidateUpdate(
                        _ctx.Db,
                        entityBatch,
                        remainingItems,
                        BranchId,
                        BatchDiscountPercent);

                    if (!validation.IsValid)
                    {
                        FailCommand(validation.Message, _ctx.Loc["Confirm"]);
                    }
                }

                var entity = _ctx.Db.ShipmentItems.FirstOrDefault(x => x.Id == SelectedItem.Id);
                if (entity != null)
                {
                    _ctx.Db.ShipmentItems.Remove(entity);
                    _ctx.Save();
                }

                var batchId = SelectedBatch.Id;
                Reload(batchId);
                return;
            }

            Items.Remove(SelectedItem);
            SelectedItem = null;
            Recalculate();
        }

        private void Recalculate()
        {
            if (SelectedBatch == null)
            {
                Subtotal = 0m;
                Total = 0m;
                TotalGross = 0m;
                TotalDiscount = 0m;
                return;
            }

            var tempBatch = new ShipmentBatch
            {
                Id = SelectedBatch.Id,
                BatchNo = BatchNo,
                Date = Date,
                BranchId = BranchId,
                Notes = Notes,
                BatchDiscountPercent = BatchDiscountPercent,
                Items = Items.ToList()
            };

            Subtotal = _ctx.Calc.ShipmentSubtotal(tempBatch);
            Total = _ctx.Calc.ShipmentTotal(tempBatch);
            TotalGross = Items.Sum(x => x.TotalLinePrice);
            TotalDiscount = TotalGross - Total;
        }

        private static void NormalizeEditableItem(ShipmentItem item)
        {
            if (item.QuantitySent < 0) item.QuantitySent = 0m;
            if (item.QuantityReturned < 0) item.QuantityReturned = 0m;
            if (item.QuantityWasted < 0) item.QuantityWasted = 0m;
            if (item.UnitPrice < 0) item.UnitPrice = 0m;
            if (item.ItemDiscountPercent < 0) item.ItemDiscountPercent = 0m;
            if (item.ItemDiscountPercent > 100) item.ItemDiscountPercent = 100m;
        }

        private bool ValidateItems()
        {
            foreach (var item in Items)
            {
                NormalizeEditableItem(item);

                if (item.QuantitySent <= 0)
                {
                    FailCommand($"'{item.ProductName}' satirinda miktar sifirdan buyuk olmalidir.", _ctx.Loc["Confirm"]);
                }

                if (item.QuantityReturned + item.QuantityWasted > item.QuantitySent)
                {
                    FailCommand($"'{item.ProductName}' satirinda iade ve zayi toplami gonderilen miktari asamaz.", _ctx.Loc["Confirm"]);
                }
            }

            return true;
        }

        private bool ValidateBatchNumber()
        {
            var normalizedBatchNo = (BatchNo ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedBatchNo))
            {
                return true;
            }

            var currentBatchId = SelectedBatch?.Id ?? 0;
            var duplicateExists = _ctx.Db.Shipments
                .AsNoTracking()
                .Select(x => new { x.Id, x.BatchNo })
                .AsEnumerable()
                .Any(x =>
                    x.Id != currentBatchId &&
                    string.Equals((x.BatchNo ?? string.Empty).Trim(), normalizedBatchNo, StringComparison.CurrentCultureIgnoreCase));

            if (!duplicateExists)
            {
                return true;
            }

            FailCommand("Ayni numarayla baska bir sevkiyat var.", _ctx.Loc["Confirm"]);
            return false;
        }

        private bool ConfirmCreditLimitForBatch(ShipmentBatch proposedBatch, ShipmentBatch? existingBatch = null)
        {
            var branch = _ctx.Db.Branches
                .AsNoTracking()
                .FirstOrDefault(x => x.Id == proposedBatch.BranchId);

            if (branch == null)
            {
                FailCommand("Secilen cari bulunamadi.", _ctx.Loc["Confirm"]);
            }

            if (branch.CreditLimit <= 0)
            {
                return true;
            }

            var currentBalance = _ctx.Calc.BranchBalance(_ctx.Db, proposedBatch.BranchId);
            var projectedBalance = currentBalance + _ctx.Calc.ShipmentTotal(proposedBatch);
            var baselineBalance = currentBalance;

            if (existingBatch != null && existingBatch.BranchId == proposedBatch.BranchId)
            {
                projectedBalance -= _ctx.Calc.ShipmentTotal(existingBatch);
            }

            var evaluation = _ctx.BranchPolicy.EvaluateCreditLimit(branch, projectedBalance);
            if (!evaluation.ExceedsLimit)
            {
                return true;
            }

            if (existingBatch != null && projectedBalance <= baselineBalance)
            {
                return true;
            }

            return ConfirmCommand(
                _ctx.BranchPolicy.BuildCreditLimitWarning(branch, evaluation, proposedBatch.Date),
                _ctx.Loc["Confirm"],
                System.Windows.MessageBoxImage.Warning);
        }

        private ShipmentBatch BuildWorkingBatch(IEnumerable<ShipmentItem>? items = null)
        {
            return new ShipmentBatch
            {
                Id = SelectedBatch?.Id ?? 0,
                BatchNo = (BatchNo ?? string.Empty).Trim(),
                Date = Date.Date,
                BranchId = BranchId,
                Notes = (Notes ?? string.Empty).Trim(),
                BatchDiscountPercent = BatchDiscountPercent,
                Items = (items ?? Items).Select(CloneShipmentItem).ToList()
            };
        }

        internal ShipmentBatch BuildCurrentBatchSnapshot()
        {
            return BuildWorkingBatch();
        }

        internal string BuildExportBatchText()
        {
            var batch = BuildCurrentBatchSnapshot();
            var branchName = Branches.FirstOrDefault(b => b.Id == batch.BranchId)?.Name ?? "";
            return _ctx.Print.BuildShipmentExportText(batch, branchName);
        }

        private static ShipmentItem CloneShipmentItem(ShipmentItem item)
        {
            return new ShipmentItem
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                UnitType = item.UnitType,
                QuantitySent = item.QuantitySent,
                QuantityReturned = item.QuantityReturned,
                QuantityWasted = item.QuantityWasted,
                UnitPrice = item.UnitPrice,
                ItemDiscountPercent = item.ItemDiscountPercent
            };
        }

        private void RaiseBatchModeProperties()
        {
            Raise(nameof(IsExistingBatch));
            Raise(nameof(HasSelectedItem));
            Raise(nameof(BatchEditorTitle));
            Raise(nameof(BatchEditorHint));
            Raise(nameof(SaveBatchButtonText));
            Raise(nameof(BatchStatusText));
            Raise(nameof(SelectedItemStatusText));
            Raise(nameof(RemoveItemButtonText));
            RemoveItemCommand.RaiseCanExecuteChanged();
        }

        private void ShowItemAddFeedback(string message)
        {
            ItemAddFeedback = message;
            IsItemAddSuccessActive = true;
            _itemAddFeedbackTimer.Stop();
            _itemAddFeedbackTimer.Start();
        }

        private void ClearItemAddFeedback()
        {
            _itemAddFeedbackTimer.Stop();
            IsItemAddSuccessActive = false;
            ItemAddFeedback = "";
        }

        private void ShowSaveBatchFeedback(string message, bool wasUpdate)
        {
            _lastSaveWasUpdate = wasUpdate;
            SaveBatchFeedback = message;
            IsSaveBatchSuccessActive = true;
            Raise(nameof(SaveBatchButtonText));
            _saveBatchFeedbackTimer.Stop();
            _saveBatchFeedbackTimer.Start();
        }

        private void ClearSaveBatchFeedback()
        {
            _saveBatchFeedbackTimer.Stop();
            SaveBatchFeedback = "";
            IsSaveBatchSuccessActive = false;
        }
    }
}
