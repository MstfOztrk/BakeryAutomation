using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BakeryAutomation.Models;
using BakeryAutomation.Services;
using Microsoft.EntityFrameworkCore;

namespace BakeryAutomation.ViewModels
{
    public sealed class PaymentRow
    {
        public Payment Payment { get; set; } = new();
        public string BranchName { get; set; } = "";
        public string ShipmentDisplay { get; set; } = "-";
        public string MethodDisplay { get; set; } = "";
        public string KindDisplay { get; set; } = "";
        public string Display { get; set; } = "";
    }

    public sealed class PaymentBranchRow
    {
        public Branch Branch { get; set; } = new();
        public decimal Balance { get; set; }
        public decimal OpenShipmentAmount { get; set; }
        public int OpenShipmentCount { get; set; }
        public DateTime? LastPaymentDate { get; set; }

        public string Name => Branch.Name;
        public string TypeDisplay => Branch.TypeDisplay;
        public bool IsActive => Branch.IsActive;
        public string StatusText => Branch.IsActive ? "Aktif" : "Pasif";
        public string BalanceText => $"Net bakiye {Balance:n2}";
        public string OpenShipmentsText => OpenShipmentCount > 0
            ? $"{OpenShipmentCount} acik fis / {OpenShipmentAmount:n2}"
            : "Acik fis yok";
        public string LastPaymentText => LastPaymentDate.HasValue
            ? $"Son tahsilat {LastPaymentDate.Value:yyyy-MM-dd}"
            : "Tahsilat yok";
    }

    public sealed class OpenShipmentRow
    {
        public int ShipmentId { get; set; }
        public string BatchNo { get; set; } = "";
        public DateTime Date { get; set; }
        public decimal Total { get; set; }
        public decimal Returns { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get; set; }
        public string Display => $"{Date:yyyy-MM-dd} | {BatchNo} | {Remaining:n2}";
    }

    public sealed class PaymentsViewModel : ObservableObject
    {
        private readonly BakeryAppContext _ctx;
        private readonly Action<int>? _showReturns;
        private bool _isLoadingSelection;

        public class EnumDisplay<T>
        {
            public T? Value { get; set; }
            public string Name { get; set; } = "";
        }

        public List<EnumDisplay<PaymentMethod>> PaymentMethods => new()
        {
            new() { Value = PaymentMethod.Cash, Name = _ctx.Loc["PaymentMethod_Cash"] },
            new() { Value = PaymentMethod.CreditCard, Name = _ctx.Loc["PaymentMethod_CreditCard"] },
            new() { Value = PaymentMethod.BankTransfer, Name = _ctx.Loc["PaymentMethod_BankTransfer"] },
            new() { Value = PaymentMethod.Other, Name = _ctx.Loc["PaymentMethod_Other"] }
        };

        public ObservableCollection<PaymentBranchRow> Branches { get; } = new();
        public ObservableCollection<PaymentRow> PaymentHistory { get; } = new();
        public ObservableCollection<OpenShipmentRow> OpenShipments { get; } = new();

        private PaymentBranchRow? _selectedBranchRow;
        public PaymentBranchRow? SelectedBranchRow
        {
            get => _selectedBranchRow;
            set
            {
                if (!Set(ref _selectedBranchRow, value)) return;

                RefreshSelectedBranchWorkspace();
                RaiseBranchSummaryProperties();

                if (_isLoadingSelection) return;

                if (value == null)
                {
                    ClearEntryForm(resetBranchSelection: false);
                    return;
                }

                PrepareDefaultEntryForSelectedBranch();
            }
        }

        private PaymentRow? _selectedRow;
        public PaymentRow? SelectedRow
        {
            get => _selectedRow;
            set
            {
                if (!Set(ref _selectedRow, value)) return;
                Selected = value?.Payment;
            }
        }

        private Payment? _selected;
        public Payment? Selected
        {
            get => _selected;
            set
            {
                if (!Set(ref _selected, value)) return;
                LoadSelectedIntoForm();
            }
        }

        private OpenShipmentRow? _selectedOpenShipment;
        public OpenShipmentRow? SelectedOpenShipment
        {
            get => _selectedOpenShipment;
            set
            {
                if (!Set(ref _selectedOpenShipment, value)) return;
                if (_isLoadingSelection || value == null) return;
                PrepareShipmentPayment(value);
            }
        }

        private DateTime _date = DateTime.Today;
        public DateTime Date
        {
            get => _date;
            set => Set(ref _date, value);
        }

        private decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set => Set(ref _amount, value);
        }

        private PaymentMethod _method = PaymentMethod.Cash;
        public PaymentMethod Method
        {
            get => _method;
            set => Set(ref _method, value);
        }

        private string _note = "";
        public string Note
        {
            get => _note;
            set => Set(ref _note, value);
        }

        private string _reference = "";
        public string Reference
        {
            get => _reference;
            set => Set(ref _reference, value);
        }

        private bool _isShipmentPayment;
        public bool IsShipmentPayment
        {
            get => _isShipmentPayment;
            set
            {
                if (!Set(ref _isShipmentPayment, value)) return;

                if (!value)
                {
                    SelectedShipmentId = null;
                    ResetShipmentSummary();
                }

                RaiseEntryStateProperties();
            }
        }

        private int? _selectedShipmentId;
        public int? SelectedShipmentId
        {
            get => _selectedShipmentId;
            set
            {
                if (!Set(ref _selectedShipmentId, value)) return;
                RaiseEntryStateProperties();
            }
        }

        private string _selectedShipmentDisplay = "";
        public string SelectedShipmentDisplay
        {
            get => _selectedShipmentDisplay;
            set
            {
                if (!Set(ref _selectedShipmentDisplay, value)) return;
                RaiseEntryStateProperties();
            }
        }

        private decimal _selectedShipmentTotal;
        public decimal SelectedShipmentTotal
        {
            get => _selectedShipmentTotal;
            set => Set(ref _selectedShipmentTotal, value);
        }

        private decimal _selectedShipmentReturns;
        public decimal SelectedShipmentReturns
        {
            get => _selectedShipmentReturns;
            set => Set(ref _selectedShipmentReturns, value);
        }

        private decimal _selectedShipmentPaid;
        public decimal SelectedShipmentPaid
        {
            get => _selectedShipmentPaid;
            set => Set(ref _selectedShipmentPaid, value);
        }

        private decimal _selectedShipmentRemaining;
        public decimal SelectedShipmentRemaining
        {
            get => _selectedShipmentRemaining;
            set
            {
                if (!Set(ref _selectedShipmentRemaining, value)) return;
                RaiseEntryStateProperties();
            }
        }

        public string SelectedBranchName => SelectedBranchRow?.Branch.Name ?? "Cari secin";

        public string SelectedBranchMeta => SelectedBranchRow == null
            ? "Soldaki listeden bir cari secin."
            : $"{SelectedBranchRow.TypeDisplay} | {SelectedBranchRow.StatusText} | {_ctx.BranchPolicy.FormatTermsSummary(SelectedBranchRow.Branch)}";

        public decimal SelectedBranchBalance => SelectedBranchRow?.Balance ?? 0m;

        public int SelectedBranchOpenShipmentCount => OpenShipments.Count;

        public decimal SelectedBranchOpenShipmentAmount => OpenShipments.Sum(x => x.Remaining);

        public string SelectedBranchLastPayment => SelectedBranchRow?.LastPaymentText ?? "Tahsilat yok";

        public string SelectedBranchReconciliationHint
        {
            get
            {
                if (SelectedBranchRow == null)
                {
                    return "Bir cari secildiginde o subenin acik fisleri ve tahsilat gecmisi tek ekranda acilir.";
                }

                if (SelectedBranchBalance <= 0m)
                {
                    return "Bu carinin net bakiyesi kapanmis gorunuyor. Gerekirse serbest tahsilat yine girebilirsiniz.";
                }

                if (SelectedBranchOpenShipmentCount == 0)
                {
                    return "Acik fis kalmadi. Net bakiyeyi etkileyen serbest tahsilat veya bagimsiz iade olabilir.";
                }

                var difference = SelectedBranchOpenShipmentAmount - SelectedBranchBalance;
                if (difference > 0.01m)
                {
                    return $"Acik fis toplamindan {difference:n2} daha dusuk net bakiye var. Bu fark fise baglanmamis tahsilat veya iade olabilir.";
                }

                return "Acik fis satirina tikladiginiz anda form kalan tutarla hazirlanir.";
            }
        }

        public string EntryTitle => Selected == null ? "Hizli Tahsilat" : "Tahsilati Duzenle";

        public string EntryModeText => IsShipmentPayment ? "Fise bagli tahsilat" : "Serbest tahsilat";

        public string EntryHint
        {
            get
            {
                if (SelectedBranchRow == null)
                {
                    return "Tahsilat baslatmak icin once bir cari secin.";
                }

                if (!IsShipmentPayment)
                {
                    return "Bu tahsilat cari bakiyesini dusurur ama belli bir fise baglanmaz.";
                }

                if (SelectedShipmentId == null)
                {
                    return "Acik fis secerek tahsilati dogrudan o fisin kalanina baglayin.";
                }

                return "Secili fisin kalan tutari otomatik getirildi. Isterseniz parcali tahsilat girebilirsiniz.";
            }
        }

        public bool HasSelectedBranch => SelectedBranchRow != null;

        public bool HasShipmentSelection => IsShipmentPayment && SelectedShipmentId.HasValue;

        public RelayCommand NewCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand RefreshCommand { get; }
        public RelayCommand GeneralPaymentCommand { get; }
        public RelayCommand GeneralReturnCommand { get; }
        public RelayCommand FillRemainingAmountCommand { get; }

        public PaymentsViewModel(BakeryAppContext ctx, int? initialBranchId = null, Action<int>? showReturns = null)
        {
            _ctx = ctx;
            _showReturns = showReturns;

            NewCommand = new RelayCommand(_ => PrepareDefaultEntryForSelectedBranch(), _ => HasSelectedBranch);
            SaveCommand = new RelayCommand(_ => Save(), _ => HasSelectedBranch);
            DeleteCommand = new RelayCommand(_ => Delete(), _ => Selected != null);
            RefreshCommand = new RelayCommand(_ => Reload(
                SelectedBranchRow?.Branch.Id,
                prepareEntry: Selected == null,
                    preferredPaymentId: Selected?.Id,
                    preferredShipmentId: Selected == null && IsShipmentPayment ? SelectedShipmentId : null));
            GeneralPaymentCommand = new RelayCommand(_ => PrepareGeneralPaymentForSelectedBranch(), _ => HasSelectedBranch);
            GeneralReturnCommand = new RelayCommand(_ => OpenGeneralReturnForSelectedBranch(), _ => HasSelectedBranch);
            FillRemainingAmountCommand = new RelayCommand(_ => Amount = SelectedShipmentRemaining, _ => HasShipmentSelection && SelectedShipmentRemaining > 0m);

            Reload(initialBranchId, prepareEntry: true);
        }

        private void Reload(
            int? preferredBranchId = null,
            bool prepareEntry = true,
            int? preferredPaymentId = null,
            int? preferredShipmentId = null)
        {
            var previouslySelectedBranchId = preferredBranchId ?? SelectedBranchRow?.Branch.Id;

            Branches.Clear();
            PaymentHistory.Clear();
            OpenShipments.Clear();

            var branchEntities = _ctx.Db.Branches
                .AsNoTracking()
                .ToList();

            var branchIds = branchEntities
                .Select(x => x.Id)
                .ToList();

            var balanceLookup = _ctx.Calc.BuildBranchBalanceLookup(_ctx.Db, branchIds);
            var openShipmentSummaryLookup = BuildOpenShipmentSummaryLookup(branchIds);
            var lastPaymentLookup = _ctx.Db.Payments
                .AsNoTracking()
                .GroupBy(x => x.BranchId)
                .Select(g => new
                {
                    BranchId = g.Key,
                    LastPaymentDate = g.Max(x => x.Date)
                })
                .ToDictionary(x => x.BranchId, x => (DateTime?)x.LastPaymentDate);

            var rows = branchEntities
                .Select(branch =>
                {
                    var openSummary = openShipmentSummaryLookup.GetValueOrDefault(branch.Id);

                    return new PaymentBranchRow
                    {
                        Branch = branch,
                        Balance = balanceLookup.GetValueOrDefault(branch.Id),
                        OpenShipmentAmount = openSummary.Amount,
                        OpenShipmentCount = openSummary.Count,
                        LastPaymentDate = lastPaymentLookup.GetValueOrDefault(branch.Id)
                    };
                })
                .OrderByDescending(x => x.IsActive)
                .ThenByDescending(x => x.Balance)
                .ThenByDescending(x => x.OpenShipmentAmount)
                .ThenBy(x => x.Name)
                .ToList();

            foreach (var row in rows)
            {
                Branches.Add(row);
            }

            var branchRow = Branches.FirstOrDefault(x => x.Branch.Id == previouslySelectedBranchId)
                ?? Branches.FirstOrDefault();

            _isLoadingSelection = true;
            SelectedBranchRow = branchRow;
            _isLoadingSelection = false;

            if (branchRow == null)
            {
                ClearEntryForm(resetBranchSelection: false);
                return;
            }

            if (preferredPaymentId.HasValue)
            {
                var selectedPaymentRow = PaymentHistory.FirstOrDefault(x => x.Payment.Id == preferredPaymentId.Value);
                if (selectedPaymentRow != null)
                {
                    SelectedRow = selectedPaymentRow;
                    return;
                }
            }

            if (!prepareEntry) return;
            PrepareDefaultEntryForSelectedBranch(preferredShipmentId);
        }

        private void RefreshSelectedBranchWorkspace()
        {
            var currentShipmentId = _selectedOpenShipment?.ShipmentId;

            PaymentHistory.Clear();
            OpenShipments.Clear();

            if (SelectedBranchRow == null)
            {
                SetSelectedOpenShipmentSilently(null);
                return;
            }

            var branchId = SelectedBranchRow.Branch.Id;

            LoadPaymentHistory(branchId);
            LoadOpenShipments(branchId);

            var matchingShipment = currentShipmentId.HasValue
                ? OpenShipments.FirstOrDefault(x => x.ShipmentId == currentShipmentId.Value)
                : null;

            SetSelectedOpenShipmentSilently(matchingShipment);
        }

        private void LoadPaymentHistory(int branchId)
        {
            var payments = _ctx.Db.Payments
                .AsNoTracking()
                .Where(x => x.BranchId == branchId)
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Id)
                .ToList();

            var shipmentIds = payments
                .Where(x => x.ShipmentId.HasValue)
                .Select(x => x.ShipmentId!.Value)
                .Distinct()
                .ToList();

            var shipmentLookup = shipmentIds.Count == 0
                ? new Dictionary<int, string>()
                : _ctx.Db.Shipments
                    .AsNoTracking()
                    .Where(x => shipmentIds.Contains(x.Id))
                    .Select(x => new { x.Id, x.BatchNo })
                    .ToDictionary(x => x.Id, x => x.BatchNo);

            foreach (var payment in payments)
            {
                var shipmentDisplay = "-";
                if (payment.ShipmentId.HasValue && shipmentLookup.TryGetValue(payment.ShipmentId.Value, out var batchNo))
                {
                    shipmentDisplay = batchNo;
                }

                var kindDisplay = payment.ShipmentId.HasValue ? "Fis" : "Serbest";
                var display = $"{payment.Date:yyyy-MM-dd} | {kindDisplay} | {payment.Amount:n2}";

                PaymentHistory.Add(new PaymentRow
                {
                    Payment = payment,
                    BranchName = SelectedBranchRow?.Name ?? "",
                    ShipmentDisplay = shipmentDisplay,
                    MethodDisplay = FormatPaymentMethod(payment.Method),
                    KindDisplay = kindDisplay,
                    Display = display
                });
            }
        }

        private void LoadOpenShipments(int branchId)
        {
            var shipments = _ctx.Db.Shipments
                .AsNoTracking()
                .Include(x => x.Items)
                .Where(x => x.BranchId == branchId)
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Id)
                .ToList();

            if (shipments.Count == 0)
            {
                return;
            }

            var shipmentIds = shipments.Select(x => x.Id).ToList();
            var returnsLookup = BuildShipmentReturnLookup(shipmentIds);
            var linkedPaymentLookup = BuildShipmentPaymentLookup(shipmentIds);

            foreach (var shipment in shipments)
            {
                var total = _ctx.Calc.ShipmentTotal(shipment);
                var returns = returnsLookup.GetValueOrDefault(shipment.Id);
                var paid = linkedPaymentLookup.GetValueOrDefault(shipment.Id);
                var remaining = total - returns - paid;

                if (remaining <= 0m) continue;

                OpenShipments.Add(new OpenShipmentRow
                {
                    ShipmentId = shipment.Id,
                    BatchNo = shipment.BatchNo,
                    Date = shipment.Date,
                    Total = total,
                    Returns = returns,
                    Paid = paid,
                    Remaining = remaining
                });
            }
        }

        private void LoadSelectedIntoForm()
        {
            if (Selected == null)
            {
                if (!_isLoadingSelection)
                {
                    PrepareDefaultEntryForSelectedBranch();
                }

                return;
            }

            _isLoadingSelection = true;

            FocusBranch(Selected.BranchId);
            Date = Selected.Date;
            Amount = Selected.Amount;
            Method = Selected.Method;
            Note = Selected.Note;
            Reference = Selected.Reference;
            IsShipmentPayment = Selected.ShipmentId.HasValue;
            SelectedShipmentId = Selected.ShipmentId;
            UpdateSelectedShipmentSummary(applyDefaults: false);

            var matchingShipment = Selected.ShipmentId.HasValue
                ? OpenShipments.FirstOrDefault(x => x.ShipmentId == Selected.ShipmentId.Value)
                : null;

            SetSelectedOpenShipmentSilently(matchingShipment);

            _isLoadingSelection = false;
            RaiseEntryStateProperties();
        }

        private void FocusBranch(int branchId)
        {
            var target = Branches.FirstOrDefault(x => x.Branch.Id == branchId)
                ?? Branches.FirstOrDefault();

            SelectedBranchRow = target;
        }

        private void PrepareDefaultEntryForSelectedBranch(int? preferredShipmentId = null)
        {
            if (SelectedBranchRow == null)
            {
                ClearEntryForm(resetBranchSelection: false);
                return;
            }

            var preferredShipment = preferredShipmentId.HasValue
                ? OpenShipments.FirstOrDefault(x => x.ShipmentId == preferredShipmentId.Value)
                : null;

            if (preferredShipment != null)
            {
                SelectedOpenShipment = preferredShipment;
                return;
            }

            if (_selectedOpenShipment != null)
            {
                var existingSelection = OpenShipments.FirstOrDefault(x => x.ShipmentId == _selectedOpenShipment.ShipmentId);
                if (existingSelection != null)
                {
                    SelectedOpenShipment = existingSelection;
                    return;
                }
            }

            if (OpenShipments.Count > 0)
            {
                SelectedOpenShipment = OpenShipments[0];
                return;
            }

            PrepareGeneralPaymentForSelectedBranch();
        }

        private void PrepareShipmentPayment(OpenShipmentRow shipmentRow)
        {
            if (SelectedBranchRow == null) return;

            ClearSelectedPaymentState();
            Date = DateTime.Today;
            Amount = 0m;
            Method = PaymentMethod.Cash;
            Reference = "";
            Note = "";
            IsShipmentPayment = true;
            SelectedShipmentId = shipmentRow.ShipmentId;
            UpdateSelectedShipmentSummary(applyDefaults: true);
        }

        private void PrepareGeneralPaymentForSelectedBranch()
        {
            if (SelectedBranchRow == null)
            {
                ClearEntryForm(resetBranchSelection: false);
                return;
            }

            ClearSelectedPaymentState();
            Date = DateTime.Today;
            Amount = 0m;
            Method = PaymentMethod.Cash;
            Note = "";
            Reference = "";
            IsShipmentPayment = false;
            SelectedShipmentId = null;
            ResetShipmentSummary();
            SetSelectedOpenShipmentSilently(null);
        }

        private void OpenGeneralReturnForSelectedBranch()
        {
            if (SelectedBranchRow == null)
            {
                return;
            }

            if (_showReturns == null)
            {
                FailCommand("Iade ekranina gecis baglanmamis.", _ctx.Loc["Confirm"]);
            }

            CancelCommand();
            _showReturns(SelectedBranchRow.Branch.Id);
        }

        private void ClearEntryForm(bool resetBranchSelection)
        {
            ClearSelectedPaymentState();
            Date = DateTime.Today;
            Amount = 0m;
            Method = PaymentMethod.Cash;
            Note = "";
            Reference = "";
            IsShipmentPayment = false;
            SelectedShipmentId = null;
            ResetShipmentSummary();
            SetSelectedOpenShipmentSilently(null);

            if (resetBranchSelection)
            {
                Set(ref _selectedBranchRow, null);
                PaymentHistory.Clear();
                OpenShipments.Clear();
                RaiseBranchSummaryProperties();
            }

            RaiseEntryStateProperties();
        }

        private void ClearSelectedPaymentState()
        {
            Set(ref _selectedRow, null);
            Set(ref _selected, null);
            RaiseEntryStateProperties();
        }

        private void Save()
        {
            var branchId = SelectedBranchRow?.Branch.Id ?? 0;
            if (branchId <= 0)
            {
                FailCommand("Tahsilat icin cari secin.", _ctx.Loc["Confirm"]);
            }

            if (Amount <= 0)
            {
                FailCommand("Tahsilat tutari sifirdan buyuk olmalidir.", _ctx.Loc["Confirm"]);
            }

            ShipmentBatch? shipment = null;
            if (IsShipmentPayment)
            {
                if (SelectedShipmentId == null)
                {
                    FailCommand("Fis tahsilati icin acik fis secin.", _ctx.Loc["Confirm"]);
                }

                shipment = _ctx.Db.Shipments
                    .AsNoTracking()
                    .Include(x => x.Items)
                    .FirstOrDefault(x => x.Id == SelectedShipmentId.Value);

                if (shipment == null)
                {
                    FailCommand("Secilen sevkiyat bulunamadi.", _ctx.Loc["Confirm"]);
                }

                if (shipment.BranchId != branchId)
                {
                    FailCommand("Tahsilat ve sevkiyat ayni cariye ait olmalidir.", _ctx.Loc["Confirm"]);
                }

                var maxAllowed = CalculateShipmentRemaining(shipment.Id, Selected?.Id);
                if (Amount > maxAllowed)
                {
                    FailCommand($"Bu fis icin en fazla {maxAllowed:n2} tahsilat girilebilir.", _ctx.Loc["Confirm"]);
                }
            }

            var isEditing = Selected != null;
            var editedPaymentId = Selected?.Id;
            var preferredShipmentId = IsShipmentPayment ? SelectedShipmentId : null;

            if (!isEditing)
            {
                _ctx.Db.Payments.Add(new Payment
                {
                    BranchId = branchId,
                    Date = Date.Date,
                    Amount = Amount,
                    Method = Method,
                    Note = (Note ?? "").Trim(),
                    Reference = (Reference ?? "").Trim(),
                    ShipmentId = IsShipmentPayment ? SelectedShipmentId : null,
                    CreatedAt = DateTime.Now
                });
            }
            else
            {
                var entity = _ctx.Db.Payments.FirstOrDefault(x => x.Id == Selected!.Id);
                if (entity == null)
                {
                    FailCommand("Secili tahsilat bulunamadi.", _ctx.Loc["Confirm"]);
                }

                entity.BranchId = branchId;
                entity.Date = Date.Date;
                entity.Amount = Amount;
                entity.Method = Method;
                entity.Note = (Note ?? "").Trim();
                entity.Reference = (Reference ?? "").Trim();
                entity.ShipmentId = IsShipmentPayment ? SelectedShipmentId : null;
            }

            _ctx.Save();

            if (isEditing && editedPaymentId.HasValue)
            {
                Reload(branchId, prepareEntry: false, preferredPaymentId: editedPaymentId.Value);
                return;
            }

            Reload(branchId, prepareEntry: true, preferredShipmentId: preferredShipmentId);
        }

        private void Delete()
        {
            if (Selected == null) return;

            if (!ConfirmCommand(_ctx.Loc["ConfirmDeletePayment"], _ctx.Loc["Confirm"]))
            {
                return;
            }

            var deletedPayment = _ctx.Db.Payments.Find(Selected.Id);
            if (deletedPayment != null)
            {
                _ctx.Db.Payments.Remove(deletedPayment);
                _ctx.Save();
            }

            Reload(SelectedBranchRow?.Branch.Id, prepareEntry: true, preferredShipmentId: SelectedShipmentId);
        }

        private void UpdateSelectedShipmentSummary(bool applyDefaults)
        {
            ResetShipmentSummary();

            if (!IsShipmentPayment || SelectedShipmentId == null)
            {
                return;
            }

            var shipment = _ctx.Db.Shipments
                .AsNoTracking()
                .Include(x => x.Items)
                .FirstOrDefault(x => x.Id == SelectedShipmentId.Value);

            if (shipment == null)
            {
                return;
            }

            SelectedShipmentDisplay = $"{shipment.Date:yyyy-MM-dd} | {shipment.BatchNo}";
            SelectedShipmentTotal = _ctx.Calc.ShipmentTotal(shipment);
            SelectedShipmentReturns = _ctx.Calc.LinkedReturnAmountForShipment(_ctx.Db, shipment.Id);
            SelectedShipmentPaid = GetPaidAmountForShipment(shipment.Id, Selected?.Id);
            SelectedShipmentRemaining = CalculateShipmentRemaining(shipment, Selected?.Id);

            if (!applyDefaults) return;

            Amount = SelectedShipmentRemaining;
            if (string.IsNullOrWhiteSpace(Note) || Note.StartsWith("Fis Tahsilati:", StringComparison.OrdinalIgnoreCase))
            {
                Note = $"Fis Tahsilati: {shipment.BatchNo}";
            }
        }

        private void ResetShipmentSummary()
        {
            SelectedShipmentDisplay = "";
            SelectedShipmentTotal = 0m;
            SelectedShipmentReturns = 0m;
            SelectedShipmentPaid = 0m;
            SelectedShipmentRemaining = 0m;
        }

        private decimal GetPaidAmountForShipment(int shipmentId, int? excludePaymentId = null)
        {
            return _ctx.Calc.LinkedPaymentAmountForShipment(_ctx.Db, shipmentId, excludePaymentId);
        }

        private decimal CalculateShipmentRemaining(int shipmentId, int? excludePaymentId = null)
        {
            var shipment = _ctx.Db.Shipments
                .AsNoTracking()
                .Include(x => x.Items)
                .FirstOrDefault(x => x.Id == shipmentId);

            if (shipment == null) return 0m;
            return CalculateShipmentRemaining(shipment, excludePaymentId);
        }

        private decimal CalculateShipmentRemaining(ShipmentBatch shipment, int? excludePaymentId = null)
        {
            var total = _ctx.Calc.ShipmentTotal(shipment);
            var returns = _ctx.Calc.LinkedReturnAmountForShipment(_ctx.Db, shipment.Id);
            var paid = GetPaidAmountForShipment(shipment.Id, excludePaymentId);
            var remaining = total - returns - paid;
            return remaining < 0m ? 0m : remaining;
        }

        private Dictionary<int, decimal> BuildShipmentReturnLookup(IReadOnlyCollection<int> shipmentIds)
        {
            if (shipmentIds.Count == 0)
            {
                return new Dictionary<int, decimal>();
            }

            return _ctx.Db.ReturnReceiptItems
                .AsNoTracking()
                .Where(x => x.SourceShipmentId.HasValue && shipmentIds.Contains(x.SourceShipmentId.Value))
                .AsEnumerable()
                .GroupBy(x => x.SourceShipmentId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.Quantity * x.UnitPrice));
        }

        private Dictionary<int, decimal> BuildShipmentPaymentLookup(IReadOnlyCollection<int> shipmentIds)
        {
            if (shipmentIds.Count == 0)
            {
                return new Dictionary<int, decimal>();
            }

            return _ctx.Db.Payments
                .AsNoTracking()
                .Where(x => x.ShipmentId.HasValue && shipmentIds.Contains(x.ShipmentId.Value))
                .AsEnumerable()
                .GroupBy(x => x.ShipmentId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.Amount));
        }

        private Dictionary<int, (decimal Amount, int Count)> BuildOpenShipmentSummaryLookup(IReadOnlyCollection<int> branchIds)
        {
            var result = branchIds.ToDictionary(x => x, _ => (Amount: 0m, Count: 0));
            if (branchIds.Count == 0)
            {
                return result;
            }

            var shipments = _ctx.Db.Shipments
                .AsNoTracking()
                .Include(x => x.Items)
                .Where(x => branchIds.Contains(x.BranchId))
                .ToList();

            if (shipments.Count == 0)
            {
                return result;
            }

            var shipmentIds = shipments.Select(x => x.Id).ToList();
            var returnsLookup = BuildShipmentReturnLookup(shipmentIds);
            var paymentLookup = BuildShipmentPaymentLookup(shipmentIds);

            foreach (var shipment in shipments)
            {
                var total = _ctx.Calc.ShipmentTotal(shipment);
                var returns = returnsLookup.GetValueOrDefault(shipment.Id);
                var paid = paymentLookup.GetValueOrDefault(shipment.Id);
                var remaining = total - returns - paid;

                if (remaining <= 0m) continue;

                var current = result.GetValueOrDefault(shipment.BranchId);
                result[shipment.BranchId] = (current.Amount + remaining, current.Count + 1);
            }

            return result;
        }

        private void SetSelectedOpenShipmentSilently(OpenShipmentRow? shipmentRow)
        {
            Set(ref _selectedOpenShipment, shipmentRow);
        }

        private void RaiseBranchSummaryProperties()
        {
            Raise(nameof(SelectedBranchName));
            Raise(nameof(SelectedBranchMeta));
            Raise(nameof(SelectedBranchBalance));
            Raise(nameof(SelectedBranchOpenShipmentCount));
            Raise(nameof(SelectedBranchOpenShipmentAmount));
            Raise(nameof(SelectedBranchLastPayment));
            Raise(nameof(SelectedBranchReconciliationHint));
            RaiseEntryStateProperties();
        }

        private void RaiseEntryStateProperties()
        {
            Raise(nameof(EntryTitle));
            Raise(nameof(EntryModeText));
            Raise(nameof(EntryHint));
            Raise(nameof(HasSelectedBranch));
            Raise(nameof(HasShipmentSelection));

            NewCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            GeneralPaymentCommand.RaiseCanExecuteChanged();
            GeneralReturnCommand.RaiseCanExecuteChanged();
            FillRemainingAmountCommand.RaiseCanExecuteChanged();
        }

        private string FormatPaymentMethod(PaymentMethod method)
        {
            return method switch
            {
                PaymentMethod.Cash => _ctx.Loc["PaymentMethod_Cash"],
                PaymentMethod.CreditCard => _ctx.Loc["PaymentMethod_CreditCard"],
                PaymentMethod.BankTransfer => _ctx.Loc["PaymentMethod_BankTransfer"],
                PaymentMethod.Other => _ctx.Loc["PaymentMethod_Other"],
                _ => method.ToString()
            };
        }
    }
}
