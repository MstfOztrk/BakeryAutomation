using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BakeryAutomation.Models;
using BakeryAutomation.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace BakeryAutomation.ViewModels
{
    public sealed class DailyReportRow
    {
        public DateTime Date { get; set; }
        public decimal OpeningBalance { get; set; }
        public int ShipmentCount { get; set; }
        public decimal ShipmentAmount { get; set; }
        public int ReturnCount { get; set; }
        public decimal ReturnAmount { get; set; }
        public decimal NetRevenue { get; set; }
        public int PaymentCount { get; set; }
        public decimal Payments { get; set; }
        public decimal ClosingBalance { get; set; }
    }

    public sealed class StatementRow
    {
        public DateTime Date { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public string DocumentNo { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Debt { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }

    public sealed class BranchReportRow
    {
        public string BranchName { get; set; } = string.Empty;
        public string TypeDisplay { get; set; } = string.Empty;
        public string TermsDisplay { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public int ShipmentCount { get; set; }
        public decimal ShipmentAmount { get; set; }
        public int ReturnCount { get; set; }
        public decimal ReturnAmount { get; set; }
        public decimal NetRevenue { get; set; }
        public int PaymentCount { get; set; }
        public decimal Payments { get; set; }
        public decimal ClosingBalance { get; set; }
        public string CreditStatus { get; set; } = string.Empty;
    }

    public sealed class PaymentMethodSummaryRow
    {
        public string MethodName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
        public decimal Share { get; set; }
    }

    public sealed class ReportsViewModel : ObservableObject
    {
        private readonly BakeryAppContext _ctx;
        private readonly Dictionary<int, Branch> _branchFilterLookup = new();

        public ObservableCollection<Branch> Branches { get; } = new();
        public ObservableCollection<DailyReportRow> Rows { get; } = new();
        public ObservableCollection<StatementRow> StatementRows { get; } = new();
        public ObservableCollection<BranchReportRow> BranchSummaryRows { get; } = new();
        public ObservableCollection<PaymentMethodSummaryRow> PaymentMethodRows { get; } = new();

        private DateTime _from = DateTime.Today.AddDays(-14);
        public DateTime From
        {
            get => _from;
            set
            {
                if (!Set(ref _from, value)) return;
                RaiseScopeProperties();
            }
        }

        private DateTime _to = DateTime.Today;
        public DateTime To
        {
            get => _to;
            set
            {
                if (!Set(ref _to, value)) return;
                RaiseScopeProperties();
            }
        }

        private int _branchId;
        public int BranchId
        {
            get => _branchId;
            set
            {
                if (!Set(ref _branchId, value)) return;
                RaiseScopeProperties();
            }
        }

        private bool _isStatementMode;
        public bool IsStatementMode
        {
            get => _isStatementMode;
            set
            {
                if (!Set(ref _isStatementMode, value)) return;
                Raise(nameof(IsDailyMode));
                RaiseReportPresentationProperties();
            }
        }

        public bool IsDailyMode
        {
            get => !IsStatementMode;
            set => IsStatementMode = !value;
        }

        private int _totalShipments;
        public int TotalShipments { get => _totalShipments; set => Set(ref _totalShipments, value); }

        private decimal _totalRevenue;
        public decimal TotalRevenue { get => _totalRevenue; set => Set(ref _totalRevenue, value); }

        private int _totalReturnsCount;
        public int TotalReturnsCount { get => _totalReturnsCount; set => Set(ref _totalReturnsCount, value); }

        private decimal _totalReturns;
        public decimal TotalReturns { get => _totalReturns; set => Set(ref _totalReturns, value); }

        private decimal _totalNetRevenue;
        public decimal TotalNetRevenue { get => _totalNetRevenue; set => Set(ref _totalNetRevenue, value); }

        private int _totalPaymentsCount;
        public int TotalPaymentsCount { get => _totalPaymentsCount; set => Set(ref _totalPaymentsCount, value); }

        private decimal _totalPayments;
        public decimal TotalPayments { get => _totalPayments; set => Set(ref _totalPayments, value); }

        private decimal _openingBalance;
        public decimal OpeningBalance { get => _openingBalance; set => Set(ref _openingBalance, value); }

        private decimal _closingBalance;
        public decimal ClosingBalance { get => _closingBalance; set => Set(ref _closingBalance, value); }

        private decimal _collectionRate;
        public decimal CollectionRate { get => _collectionRate; set => Set(ref _collectionRate, value); }

        private decimal _returnRate;
        public decimal ReturnRate { get => _returnRate; set => Set(ref _returnRate, value); }

        public decimal PeriodNetChange => ClosingBalance - OpeningBalance;
        public string PeriodText
        {
            get
            {
                var (from, to) = GetNormalizedDateRange();
                return $"{from:yyyy-MM-dd} - {to:yyyy-MM-dd}";
            }
        }

        public string ReportScopeTitle
        {
            get
            {
                var selectedBranch = GetSelectedBranchInfo();
                return selectedBranch == null
                    ? "Tum cariler icin toplu muhasebe raporu"
                    : $"{selectedBranch.Name} icin muhasebe raporu";
            }
        }

        public string ReportScopeDescription
        {
            get
            {
                var selectedBranch = GetSelectedBranchInfo();
                if (selectedBranch == null)
                {
                    return "Filtre tum carileri kapsar. Donem ici hareketler, cari bazli kapanis bakiyeleri ve tahsilat dagilimi birlikte listelenir.";
                }

                var termsSummary = _ctx.BranchPolicy.FormatTermsSummary(selectedBranch);
                var nextCollectionDate = _ctx.BranchPolicy.GetNextCollectionDate(selectedBranch, To.Date);
                var termsText = string.IsNullOrWhiteSpace(termsSummary) ? "Vade tanimsiz" : termsSummary;
                var phoneText = string.IsNullOrWhiteSpace(selectedBranch.Phone) ? "-" : selectedBranch.Phone;
                var nextCollectionText = nextCollectionDate?.ToString("yyyy-MM-dd") ?? "-";
                var creditText = selectedBranch.CreditLimit > 0m ? selectedBranch.CreditLimit.ToString("n2") : "Limitsiz";

                return $"{selectedBranch.TypeDisplay} | Vade: {termsText} | Sonraki tahsilat: {nextCollectionText} | Telefon: {phoneText} | Kredi limiti: {creditText}";
            }
        }

        public string DetailTitle => IsStatementMode ? "Cari Ekstre Detayi" : "Gunluk Muhasebe Ozeti";

        public string DetailHint => IsStatementMode
            ? "Devreden bakiye ile birlikte borc ve alacak hareketleri tarih sirasiyla listelenir."
            : "Gun bazinda sevkiyat, iade, tahsilat ve gun sonu bakiye akisi listelenir.";

        public string ShipmentCardNote => $"{TotalShipments} fis";
        public string ReturnCardNote => $"{TotalReturnsCount} iade";
        public string PaymentCardNote => $"{TotalPaymentsCount} tahsilat";
        public string OpeningBalanceNote => $"Donem hareketi {PeriodNetChange:n2}";
        public string ClosingBalanceNote => BranchId == 0 ? "Toplu kapanis bakiyesi" : "Cari kapanis bakiyesi";
        public string CollectionRateNote => TotalNetRevenue > 0m ? "Tahsilat / net ciro" : "Net ciro yok";
        public string ReturnRateNote => TotalRevenue > 0m ? "Iade / sevkiyat" : "Sevkiyat yok";

        public RelayCommand RefreshCommand { get; }
        public RelayCommand ExportCsvCommand { get; }

        public ReportsViewModel(BakeryAppContext ctx)
        {
            _ctx = ctx;
            RefreshCommand = new RelayCommand(_ => Refresh());
            ExportCsvCommand = new RelayCommand(_ => ExportCsv());

            LoadBranches();
            Refresh();
        }

        private void LoadBranches()
        {
            Branches.Clear();
            _branchFilterLookup.Clear();

            Branches.Add(new Branch { Id = 0, Name = "(Tumu)" });

            var branches = _ctx.Db.Branches
                .AsNoTracking()
                .OrderByDescending(b => b.IsActive)
                .ThenBy(b => b.Name)
                .ToList();

            foreach (var branch in branches)
            {
                _branchFilterLookup[branch.Id] = branch;

                Branches.Add(new Branch
                {
                    Id = branch.Id,
                    Name = branch.IsActive ? branch.Name : $"{branch.Name} (Pasif)",
                    Type = branch.Type,
                    Address = branch.Address,
                    ContactName = branch.ContactName,
                    Phone = branch.Phone,
                    PaymentTerms = branch.PaymentTerms,
                    PaymentDayOfMonth = branch.PaymentDayOfMonth,
                    CreditLimit = branch.CreditLimit,
                    IsActive = branch.IsActive,
                    CreatedAt = branch.CreatedAt,
                    UpdatedAt = branch.UpdatedAt
                });
            }

            BranchId = 0;
            RaiseScopeProperties();
        }

        private void Refresh()
        {
            Rows.Clear();
            StatementRows.Clear();
            BranchSummaryRows.Clear();
            PaymentMethodRows.Clear();

            var (from, to) = GetNormalizedDateRange();
            var scopedBranches = _ctx.Db.Branches
                .AsNoTracking()
                .Where(b => BranchId == 0 || b.Id == BranchId)
                .OrderBy(b => b.Name)
                .ToList();

            var branchIds = scopedBranches.Select(x => x.Id).ToList();

            var shipments = _ctx.Db.Shipments
                .AsNoTracking()
                .Include(s => s.Items)
                .Where(s => s.Date >= from && s.Date <= to && (BranchId == 0 || s.BranchId == BranchId))
                .ToList();

            var returns = _ctx.Db.ReturnReceipts
                .AsNoTracking()
                .Include(r => r.Items)
                .Where(r => r.Date >= from && r.Date <= to && (BranchId == 0 || r.BranchId == BranchId))
                .ToList();

            var payments = _ctx.Db.Payments
                .AsNoTracking()
                .Where(p => p.Date >= from && p.Date <= to && (BranchId == 0 || p.BranchId == BranchId))
                .ToList();

            var shipmentTotals = shipments.ToDictionary(x => x.Id, x => _ctx.Calc.ShipmentTotal(x));
            var returnTotals = returns.ToDictionary(x => x.Id, x => _ctx.Calc.ReturnTotal(x));

            var openingLookup = _ctx.Calc.BuildBranchBalanceLookup(_ctx.Db, branchIds, from.AddDays(-1));
            var closingLookup = _ctx.Calc.BuildBranchBalanceLookup(_ctx.Db, branchIds, to);

            UpdateSummary(shipments, returns, payments, shipmentTotals, returnTotals, openingLookup, closingLookup);
            RefreshBranchSummary(scopedBranches, shipments, returns, payments, shipmentTotals, returnTotals, openingLookup, closingLookup, to);
            RefreshPaymentMethodSummary(payments);

            if (IsStatementMode)
            {
                RefreshStatement(from, shipments, returns, payments, shipmentTotals, returnTotals);
            }
            else
            {
                RefreshDaily(from, to, shipments, returns, payments, shipmentTotals, returnTotals);
            }
        }

        private void UpdateSummary(
            IReadOnlyCollection<ShipmentBatch> shipments,
            IReadOnlyCollection<ReturnReceipt> returns,
            IReadOnlyCollection<Payment> payments,
            IReadOnlyDictionary<int, decimal> shipmentTotals,
            IReadOnlyDictionary<int, decimal> returnTotals,
            IReadOnlyDictionary<int, decimal> openingLookup,
            IReadOnlyDictionary<int, decimal> closingLookup)
        {
            TotalShipments = shipments.Count;
            TotalRevenue = shipmentTotals.Values.Sum();
            TotalReturnsCount = returns.Count;
            TotalReturns = returnTotals.Values.Sum();
            TotalNetRevenue = TotalRevenue - TotalReturns;
            TotalPaymentsCount = payments.Count;
            TotalPayments = payments.Sum(p => p.Amount);
            OpeningBalance = openingLookup.Values.Sum();
            ClosingBalance = closingLookup.Values.Sum();
            CollectionRate = TotalNetRevenue > 0m ? TotalPayments / TotalNetRevenue : 0m;
            ReturnRate = TotalRevenue > 0m ? TotalReturns / TotalRevenue : 0m;

            RaiseSummaryPresentationProperties();
        }

        private void RefreshDaily(
            DateTime from,
            DateTime to,
            IReadOnlyCollection<ShipmentBatch> shipments,
            IReadOnlyCollection<ReturnReceipt> returns,
            IReadOnlyCollection<Payment> payments,
            IReadOnlyDictionary<int, decimal> shipmentTotals,
            IReadOnlyDictionary<int, decimal> returnTotals)
        {
            var shipmentAmountLookup = shipments
                .GroupBy(s => s.Date.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => shipmentTotals[x.Id]));
            var shipmentCountLookup = shipments
                .GroupBy(s => s.Date.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var returnAmountLookup = returns
                .GroupBy(r => r.Date.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => returnTotals[x.Id]));
            var returnCountLookup = returns
                .GroupBy(r => r.Date.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var paymentAmountLookup = payments
                .GroupBy(p => p.Date.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
            var paymentCountLookup = payments
                .GroupBy(p => p.Date.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var runningBalance = OpeningBalance;
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                var shipmentAmount = shipmentAmountLookup.GetValueOrDefault(date, 0m);
                var returnAmount = returnAmountLookup.GetValueOrDefault(date, 0m);
                var paymentAmount = paymentAmountLookup.GetValueOrDefault(date, 0m);
                var closingBalance = runningBalance + shipmentAmount - returnAmount - paymentAmount;

                Rows.Add(new DailyReportRow
                {
                    Date = date,
                    OpeningBalance = runningBalance,
                    ShipmentCount = shipmentCountLookup.GetValueOrDefault(date, 0),
                    ShipmentAmount = shipmentAmount,
                    ReturnCount = returnCountLookup.GetValueOrDefault(date, 0),
                    ReturnAmount = returnAmount,
                    NetRevenue = shipmentAmount - returnAmount,
                    PaymentCount = paymentCountLookup.GetValueOrDefault(date, 0),
                    Payments = paymentAmount,
                    ClosingBalance = closingBalance
                });

                runningBalance = closingBalance;
            }
        }

        private void RefreshStatement(
            DateTime from,
            IReadOnlyCollection<ShipmentBatch> shipments,
            IReadOnlyCollection<ReturnReceipt> returns,
            IReadOnlyCollection<Payment> payments,
            IReadOnlyDictionary<int, decimal> shipmentTotals,
            IReadOnlyDictionary<int, decimal> returnTotals)
        {
            var paymentShipmentLookup = BuildShipmentNumberLookup(payments);
            var openingBalance = OpeningBalance;

            StatementRows.Add(new StatementRow
            {
                Date = from,
                BranchName = BranchId == 0 ? "Tum Cariler" : GetSelectedBranchInfo()?.Name ?? "-",
                TransactionType = "Devreden",
                DocumentNo = "-",
                Description = "Donem basi devreden bakiye",
                Debt = openingBalance > 0m ? openingBalance : 0m,
                Credit = openingBalance < 0m ? -openingBalance : 0m,
                Balance = openingBalance
            });

            var transactions = shipments.Select(s => new StatementTransaction
            {
                SortOrder = 1,
                SortId = s.Id,
                Date = s.Date,
                BranchName = GetBranchName(s.BranchId),
                Type = "Fis",
                DocumentNo = s.BatchNo,
                Description = string.IsNullOrWhiteSpace(s.Notes) ? "Sevkiyat" : s.Notes,
                Debt = shipmentTotals[s.Id],
                Credit = 0m
            }).Concat(returns.Select(r => new StatementTransaction
            {
                SortOrder = 2,
                SortId = r.Id,
                Date = r.Date,
                BranchName = GetBranchName(r.BranchId),
                Type = "Iade",
                DocumentNo = r.ReturnNo,
                Description = string.IsNullOrWhiteSpace(r.Notes) ? "Iade" : r.Notes,
                Debt = 0m,
                Credit = returnTotals[r.Id]
            })).Concat(payments.Select(p => new StatementTransaction
            {
                SortOrder = 3,
                SortId = p.Id,
                Date = p.Date,
                BranchName = GetBranchName(p.BranchId),
                Type = "Tahsilat",
                DocumentNo = ResolvePaymentDocumentNo(p, paymentShipmentLookup),
                Description = BuildPaymentDescription(p),
                Debt = 0m,
                Credit = p.Amount
            }))
            .OrderBy(x => x.Date)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.SortId)
            .ToList();

            var balance = openingBalance;
            foreach (var transaction in transactions)
            {
                balance += transaction.Debt - transaction.Credit;
                StatementRows.Add(new StatementRow
                {
                    Date = transaction.Date,
                    BranchName = transaction.BranchName,
                    TransactionType = transaction.Type,
                    DocumentNo = transaction.DocumentNo,
                    Description = transaction.Description,
                    Debt = transaction.Debt,
                    Credit = transaction.Credit,
                    Balance = balance
                });
            }
        }

        private void RefreshBranchSummary(
            IReadOnlyCollection<Branch> scopedBranches,
            IReadOnlyCollection<ShipmentBatch> shipments,
            IReadOnlyCollection<ReturnReceipt> returns,
            IReadOnlyCollection<Payment> payments,
            IReadOnlyDictionary<int, decimal> shipmentTotals,
            IReadOnlyDictionary<int, decimal> returnTotals,
            IReadOnlyDictionary<int, decimal> openingLookup,
            IReadOnlyDictionary<int, decimal> closingLookup,
            DateTime referenceDate)
        {
            var shipmentLookup = shipments.GroupBy(x => x.BranchId).ToDictionary(g => g.Key, g => g.ToList());
            var returnLookup = returns.GroupBy(x => x.BranchId).ToDictionary(g => g.Key, g => g.ToList());
            var paymentLookup = payments.GroupBy(x => x.BranchId).ToDictionary(g => g.Key, g => g.ToList());

            var rows = new List<BranchReportRow>();
            foreach (var branch in scopedBranches)
            {
                var branchShipments = shipmentLookup.GetValueOrDefault(branch.Id, new List<ShipmentBatch>());
                var branchReturns = returnLookup.GetValueOrDefault(branch.Id, new List<ReturnReceipt>());
                var branchPayments = paymentLookup.GetValueOrDefault(branch.Id, new List<Payment>());

                var shipmentAmount = branchShipments.Sum(x => shipmentTotals.GetValueOrDefault(x.Id, 0m));
                var returnAmount = branchReturns.Sum(x => returnTotals.GetValueOrDefault(x.Id, 0m));
                var paymentAmount = branchPayments.Sum(x => x.Amount);
                var openingBalance = openingLookup.GetValueOrDefault(branch.Id, 0m);
                var closingBalance = closingLookup.GetValueOrDefault(branch.Id, 0m);

                if (BranchId == 0 &&
                    openingBalance == 0m &&
                    closingBalance == 0m &&
                    branchShipments.Count == 0 &&
                    branchReturns.Count == 0 &&
                    branchPayments.Count == 0)
                {
                    continue;
                }

                var termsSummary = _ctx.BranchPolicy.FormatTermsSummary(branch);
                var nextCollectionDate = _ctx.BranchPolicy.GetNextCollectionDate(branch, referenceDate);
                var termsText = string.IsNullOrWhiteSpace(termsSummary) ? "Vade tanimsiz" : termsSummary;
                var nextCollectionText = nextCollectionDate?.ToString("yyyy-MM-dd") ?? "-";
                var creditStatus = BuildCreditStatus(_ctx.BranchPolicy.EvaluateCreditLimit(branch, closingBalance));

                rows.Add(new BranchReportRow
                {
                    BranchName = branch.Name,
                    TypeDisplay = branch.TypeDisplay,
                    TermsDisplay = $"{termsText} | Tahsilat: {nextCollectionText}",
                    OpeningBalance = openingBalance,
                    ShipmentCount = branchShipments.Count,
                    ShipmentAmount = shipmentAmount,
                    ReturnCount = branchReturns.Count,
                    ReturnAmount = returnAmount,
                    NetRevenue = shipmentAmount - returnAmount,
                    PaymentCount = branchPayments.Count,
                    Payments = paymentAmount,
                    ClosingBalance = closingBalance,
                    CreditStatus = creditStatus
                });
            }

            foreach (var row in rows
                .OrderByDescending(x => x.ClosingBalance)
                .ThenByDescending(x => x.NetRevenue)
                .ThenBy(x => x.BranchName))
            {
                BranchSummaryRows.Add(row);
            }
        }

        private void RefreshPaymentMethodSummary(IReadOnlyCollection<Payment> payments)
        {
            var totalAmount = payments.Sum(x => x.Amount);
            var groups = payments
                .GroupBy(x => x.Method)
                .Select(g => new PaymentMethodSummaryRow
                {
                    MethodName = FormatPaymentMethod(g.Key),
                    Count = g.Count(),
                    Amount = g.Sum(x => x.Amount),
                    Share = totalAmount > 0m ? g.Sum(x => x.Amount) / totalAmount : 0m
                })
                .OrderByDescending(x => x.Amount)
                .ThenBy(x => x.MethodName)
                .ToList();

            foreach (var row in groups)
            {
                PaymentMethodRows.Add(row);
            }
        }

        private Dictionary<int, string> BuildShipmentNumberLookup(IReadOnlyCollection<Payment> payments)
        {
            var shipmentIds = payments
                .Where(x => x.ShipmentId.HasValue)
                .Select(x => x.ShipmentId!.Value)
                .Distinct()
                .ToList();

            if (shipmentIds.Count == 0)
            {
                return new Dictionary<int, string>();
            }

            return _ctx.Db.Shipments
                .AsNoTracking()
                .Where(x => shipmentIds.Contains(x.Id))
                .Select(x => new { x.Id, x.BatchNo })
                .ToDictionary(x => x.Id, x => x.BatchNo);
        }

        private string ResolvePaymentDocumentNo(Payment payment, IReadOnlyDictionary<int, string> paymentShipmentLookup)
        {
            if (!string.IsNullOrWhiteSpace(payment.Reference))
            {
                return payment.Reference;
            }

            if (payment.ShipmentId.HasValue && paymentShipmentLookup.TryGetValue(payment.ShipmentId.Value, out var batchNo))
            {
                return batchNo;
            }

            return "-";
        }

        private string BuildPaymentDescription(Payment payment)
        {
            var parts = new List<string> { FormatPaymentMethod(payment.Method) };

            if (!string.IsNullOrWhiteSpace(payment.Note))
            {
                parts.Add(payment.Note);
            }

            return string.Join(" | ", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private (DateTime From, DateTime To) GetNormalizedDateRange()
        {
            var from = From.Date;
            var to = To.Date;
            if (to < from)
            {
                var temp = from;
                from = to;
                to = temp;
            }

            return (from, to);
        }

        private Branch? GetSelectedBranchInfo()
        {
            return BranchId > 0 && _branchFilterLookup.TryGetValue(BranchId, out var branch)
                ? branch
                : null;
        }

        private string GetBranchName(int branchId)
        {
            if (_branchFilterLookup.TryGetValue(branchId, out var branch))
            {
                return branch.Name;
            }

            return $"Cari #{branchId}";
        }

        private void RaiseScopeProperties()
        {
            Raise(nameof(PeriodText));
            Raise(nameof(ReportScopeTitle));
            Raise(nameof(ReportScopeDescription));
            Raise(nameof(ClosingBalanceNote));
        }

        private void RaiseSummaryPresentationProperties()
        {
            Raise(nameof(PeriodNetChange));
            Raise(nameof(ShipmentCardNote));
            Raise(nameof(ReturnCardNote));
            Raise(nameof(PaymentCardNote));
            Raise(nameof(OpeningBalanceNote));
            Raise(nameof(ClosingBalanceNote));
            Raise(nameof(CollectionRateNote));
            Raise(nameof(ReturnRateNote));
        }

        private void RaiseReportPresentationProperties()
        {
            Raise(nameof(DetailTitle));
            Raise(nameof(DetailHint));
        }

        private void ExportCsv()
        {
            var dialog = new SaveFileDialog
            {
                FileName = IsStatementMode ? "cari_ekstre_detayli.csv" : "gunluk_muhasebe_raporu.csv",
                Filter = "CSV (*.csv)|*.csv|Tum Dosyalar (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
            {
                CancelCommand();
                return;
            }

            if (IsStatementMode)
            {
                var headers = new[] { "Tarih", "Cari", "Islem Turu", "Belge / Referans", "Aciklama", "Borc", "Alacak", "Bakiye" };
                var rows = StatementRows.Select(r => new[]
                {
                    r.Date.ToString("yyyy-MM-dd"),
                    r.BranchName,
                    r.TransactionType,
                    r.DocumentNo,
                    r.Description,
                    r.Debt.ToString("0.00"),
                    r.Credit.ToString("0.00"),
                    r.Balance.ToString("0.00")
                });

                _ctx.Csv.Export(dialog.FileName, headers, rows);
                return;
            }

            var dailyHeaders = new[]
            {
                "Tarih",
                "Devreden Bakiye",
                "Sevkiyat Adedi",
                "Sevkiyat",
                "Iade Adedi",
                "Iade",
                "Net Ciro",
                "Tahsilat Adedi",
                "Tahsilat",
                "Gun Sonu Bakiye"
            };
            var dailyRows = Rows.Select(r => new[]
            {
                r.Date.ToString("yyyy-MM-dd"),
                r.OpeningBalance.ToString("0.00"),
                r.ShipmentCount.ToString(),
                r.ShipmentAmount.ToString("0.00"),
                r.ReturnCount.ToString(),
                r.ReturnAmount.ToString("0.00"),
                r.NetRevenue.ToString("0.00"),
                r.PaymentCount.ToString(),
                r.Payments.ToString("0.00"),
                r.ClosingBalance.ToString("0.00")
            });

            _ctx.Csv.Export(dialog.FileName, dailyHeaders, dailyRows);
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

        private static string BuildCreditStatus(CreditLimitEvaluation evaluation)
        {
            if (!evaluation.HasCreditLimit)
            {
                return "Limitsiz";
            }

            if (evaluation.ExceedsLimit)
            {
                return $"Asim {evaluation.ProjectedBalance - evaluation.CreditLimit:n2}";
            }

            return $"Kalan {evaluation.RemainingCredit:n2}";
        }

        private sealed class StatementTransaction
        {
            public int SortOrder { get; set; }
            public int SortId { get; set; }
            public DateTime Date { get; set; }
            public string BranchName { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string DocumentNo { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public decimal Debt { get; set; }
            public decimal Credit { get; set; }
        }
    }
}
