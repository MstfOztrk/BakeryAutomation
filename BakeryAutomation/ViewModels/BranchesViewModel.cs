using System;
using System.Collections.ObjectModel;
using System.Linq;
using BakeryAutomation.Models;
using BakeryAutomation.Services;
using Microsoft.EntityFrameworkCore;

namespace BakeryAutomation.ViewModels
{
    public sealed class BranchPriceOverrideRow : ObservableObject
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";

        private decimal _unitPrice;
        public decimal UnitPrice { get => _unitPrice; set => Set(ref _unitPrice, value); }
    }

    public sealed class BranchListRow
    {
        public Branch Branch { get; set; } = new();
        public decimal Balance { get; set; }
        public string TermsSummary { get; set; } = "";
        public string NextCollectionDateText { get; set; } = "";
        public string CreditStatus { get; set; } = "";

        public string Name => Branch.Name;
        public string TypeDisplay => Branch.TypeDisplay;
        public string Phone => Branch.Phone;
        public bool IsActive => Branch.IsActive;
        public decimal CreditLimit => Branch.CreditLimit;
    }

    public sealed class BranchesViewModel : ObservableObject
    {
        private readonly BakeryAppContext _ctx;
        private readonly Action<int>? _showStatement;
        private readonly Action<int>? _showPayments;
        private bool _isReloadingBranches;

        public class EnumDisplay<T>
        {
            public T? Value { get; set; }
            public string Name { get; set; } = "";
        }

        public System.Collections.Generic.List<EnumDisplay<BranchType>> BranchTypes => new()
        {
            new() { Value = BranchType.Branch, Name = _ctx.Loc["BranchType_Branch"] },
            new() { Value = BranchType.Market, Name = _ctx.Loc["BranchType_Market"] },
            new() { Value = BranchType.Grocery, Name = _ctx.Loc["BranchType_Grocery"] }
        };

        public ObservableCollection<BranchListRow> Branches { get; } = new();
        public ObservableCollection<Product> Products { get; } = new();
        public ObservableCollection<BranchPriceOverrideRow> Overrides { get; } = new();

        private BranchListRow? _selectedRow;
        public BranchListRow? SelectedRow
        {
            get => _selectedRow;
            set
            {
                if (!Set(ref _selectedRow, value)) return;
                if (_isReloadingBranches && value == null) return;
                Selected = value?.Branch;
            }
        }

        private Branch? _selected;
        public Branch? Selected
        {
            get => _selected;
            set
            {
                if (!Set(ref _selected, value)) return;
                LoadSelectedIntoForm();
                RaiseSelectionStateProperties();
            }
        }

        public bool HasSelectedBranch => Selected != null;

        public string FormModeTitle => Selected == null
            ? "Yeni Cari Kaydi"
            : $"Duzenlenen Cari: {Selected.Name}";

        public string FormModeHint => Selected == null
            ? "Form yeni cari acmak icin bos. Onceki secimi tasimadan dogrudan yeni kayit girebilirsiniz."
            : "Bu alanlar secili cariyi gunceller. Yeni cari acmak icin 'Yeni Kayit' dugmesine basin.";

        public string SelectedBranchSummary
        {
            get
            {
                if (Selected == null)
                {
                    return "Ozel fiyat eklemek icin soldaki listeden bir cari secin.";
                }

                var termsSummary = _ctx.BranchPolicy.FormatTermsSummary(Selected);
                var nextCollectionDate = _ctx.BranchPolicy.GetNextCollectionDate(Selected, DateTime.Today);
                var termsText = string.IsNullOrWhiteSpace(termsSummary) ? "Vade tanimsiz" : termsSummary;
                var nextCollectionText = nextCollectionDate?.ToString("yyyy-MM-dd") ?? "-";

                return $"Secili cari: {Selected.Name} | Vade: {termsText} | Sonraki tahsilat: {nextCollectionText}";
            }
        }

        private string _name = "";
        public string Name { get => _name; set => Set(ref _name, value); }

        private BranchType _type = BranchType.Branch;
        public BranchType Type { get => _type; set => Set(ref _type, value); }

        private string _address = "";
        public string Address { get => _address; set => Set(ref _address, value); }

        private string _contactName = "";
        public string ContactName { get => _contactName; set => Set(ref _contactName, value); }

        private string _phone = "";
        public string Phone { get => _phone; set => Set(ref _phone, value); }

        private string _paymentTerms = "";
        public string PaymentTerms { get => _paymentTerms; set => Set(ref _paymentTerms, value); }

        private int? _paymentDayOfMonth;
        public int? PaymentDayOfMonth { get => _paymentDayOfMonth; set => Set(ref _paymentDayOfMonth, value); }

        private decimal _creditLimit;
        public decimal CreditLimit { get => _creditLimit; set => Set(ref _creditLimit, value); }

        private bool _isActive = true;
        public bool IsActive { get => _isActive; set => Set(ref _isActive, value); }

        private int _overrideProductId;
        public int OverrideProductId { get => _overrideProductId; set => Set(ref _overrideProductId, value); }

        private decimal _overrideUnitPrice;
        public decimal OverrideUnitPrice { get => _overrideUnitPrice; set => Set(ref _overrideUnitPrice, value); }

        private BranchPriceOverrideRow? _selectedOverride;
        public BranchPriceOverrideRow? SelectedOverride
        {
            get => _selectedOverride;
            set
            {
                if (!Set(ref _selectedOverride, value)) return;
                if (value == null)
                {
                    OverrideProductId = 0;
                    OverrideUnitPrice = 0m;
                    return;
                }

                OverrideProductId = value.ProductId;
                OverrideUnitPrice = value.UnitPrice;
            }
        }

        public RelayCommand NewCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand RefreshCommand { get; }
        public RelayCommand ViewStatementCommand { get; }
        public RelayCommand OpenCollectionsCommand { get; }
        public RelayCommand AddOrUpdateOverrideCommand { get; }
        public RelayCommand RemoveOverrideCommand { get; }

        public BranchesViewModel(BakeryAppContext ctx, Action<int>? showStatement = null, Action<int>? showPayments = null)
        {
            _ctx = ctx;
            _showStatement = showStatement;
            _showPayments = showPayments;

            NewCommand = new RelayCommand(_ => StartNewEntry());
            SaveCommand = new RelayCommand(_ => Save());
            DeleteCommand = new RelayCommand(_ => Delete(), _ => Selected != null);
            RefreshCommand = new RelayCommand(_ => Reload());
            ViewStatementCommand = new RelayCommand(_ => ViewStatement(), _ => Selected != null);
            OpenCollectionsCommand = new RelayCommand(_ => OpenCollections(), _ => Selected != null);
            AddOrUpdateOverrideCommand = new RelayCommand(_ => AddOrUpdateOverride(), _ => OverrideProductId > 0);
            RemoveOverrideCommand = new RelayCommand(_ => RemoveOverride(), _ => SelectedOverride != null);

            Reload();
        }

        private void Reload()
        {
            var preservedSelectedBranchId = Selected?.Id ?? _selectedRow?.Branch.Id ?? 0;

            _isReloadingBranches = true;
            Branches.Clear();
            Products.Clear();

            var products = _ctx.Db.Products
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToList();

            foreach (var product in products) Products.Add(product);

            var branches = _ctx.Db.Branches
                .AsNoTracking()
                .OrderBy(b => b.Name)
                .ToList();

            var balanceLookup = _ctx.Calc.BuildBranchBalanceLookup(_ctx.Db, branches.Select(x => x.Id));
            foreach (var branch in branches)
            {
                var creditEvaluation = _ctx.BranchPolicy.EvaluateCreditLimit(
                    branch,
                    balanceLookup.GetValueOrDefault(branch.Id));

                Branches.Add(new BranchListRow
                {
                    Branch = branch,
                    Balance = balanceLookup.GetValueOrDefault(branch.Id),
                    TermsSummary = _ctx.BranchPolicy.FormatTermsSummary(branch),
                    NextCollectionDateText = _ctx.BranchPolicy.GetNextCollectionDate(branch, DateTime.Today)?.ToString("yyyy-MM-dd") ?? "-",
                    CreditStatus = BuildCreditStatus(creditEvaluation)
                });
            }

            _isReloadingBranches = false;

            if (preservedSelectedBranchId > 0)
            {
                var restoredRow = Branches.FirstOrDefault(x => x.Branch.Id == preservedSelectedBranchId);
                if (restoredRow != null)
                {
                    SelectedRow = restoredRow;
                    return;
                }

                StartNewEntry();
            }
            else
            {
                RefreshOverrides();
                RaiseSelectionStateProperties();
            }
        }

        private void RefreshOverrides()
        {
            Overrides.Clear();

            if (Selected == null)
            {
                SelectedOverride = null;
                return;
            }

            var overrides = _ctx.Db.BranchPriceOverrides
                .AsNoTracking()
                .Where(x => x.BranchId == Selected.Id)
                .Join(_ctx.Db.Products.AsNoTracking(), x => x.ProductId, p => p.Id, (x, p) => new BranchPriceOverrideRow
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    UnitPrice = x.UnitPrice
                })
                .OrderBy(x => x.ProductName)
                .ToList();

            foreach (var row in overrides) Overrides.Add(row);
            SelectedOverride = null;
        }

        private void LoadSelectedIntoForm()
        {
            if (Selected == null)
            {
                ResetFormFields();
                return;
            }

            Name = Selected.Name;
            Type = Selected.Type;
            Address = Selected.Address;
            ContactName = Selected.ContactName;
            Phone = Selected.Phone;
            PaymentTerms = Selected.PaymentTerms;
            PaymentDayOfMonth = Selected.PaymentDayOfMonth;
            CreditLimit = Selected.CreditLimit;
            IsActive = Selected.IsActive;
            RefreshOverrides();
        }

        private void StartNewEntry()
        {
            Set(ref _selectedRow, null);
            Set(ref _selected, null);
            ResetFormFields();
            RaiseSelectionStateProperties();
        }

        private void ResetFormFields()
        {
            Name = "";
            Type = BranchType.Branch;
            Address = "";
            ContactName = "";
            Phone = "";
            PaymentTerms = "";
            PaymentDayOfMonth = null;
            CreditLimit = 0m;
            IsActive = true;
            Overrides.Clear();
            SelectedOverride = null;
            OverrideProductId = 0;
            OverrideUnitPrice = 0m;
        }

        private void Save()
        {
            var normalizedName = (Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                FailCommand("Cari adi bos birakilamaz.", _ctx.Loc["Confirm"]);
            }

            if (PaymentDayOfMonth.HasValue && (PaymentDayOfMonth < 1 || PaymentDayOfMonth > 31))
            {
                FailCommand("Tahsilat gunu 1-31 arasinda olmalidir.", _ctx.Loc["Confirm"]);
            }

            if (CreditLimit < 0)
            {
                FailCommand("Kredi limiti negatif olamaz.", _ctx.Loc["Confirm"]);
            }

            var duplicateExists = _ctx.Db.Branches
                .AsNoTracking()
                .Select(x => new { x.Id, x.Name })
                .AsEnumerable()
                .Any(x =>
                    x.Id != (Selected?.Id ?? 0) &&
                    string.Equals(x.Name?.Trim(), normalizedName, StringComparison.CurrentCultureIgnoreCase));

            if (duplicateExists)
            {
                FailCommand("Ayni adla baska bir cari kaydi var.", _ctx.Loc["Confirm"]);
            }

            Branch entity;
            if (Selected == null)
            {
                entity = new Branch
                {
                    Name = normalizedName,
                    Type = Type,
                    Address = (Address ?? "").Trim(),
                    ContactName = (ContactName ?? "").Trim(),
                    Phone = (Phone ?? "").Trim(),
                    PaymentTerms = (PaymentTerms ?? "").Trim(),
                    PaymentDayOfMonth = PaymentDayOfMonth,
                    CreditLimit = CreditLimit,
                    IsActive = IsActive,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _ctx.Db.Branches.Add(entity);
                _ctx.Save();
            }
            else
            {
                entity = _ctx.Db.Branches.FirstOrDefault(x => x.Id == Selected.Id) ?? Selected;
                entity.Name = normalizedName;
                entity.Type = Type;
                entity.Address = (Address ?? "").Trim();
                entity.ContactName = (ContactName ?? "").Trim();
                entity.Phone = (Phone ?? "").Trim();
                entity.PaymentTerms = (PaymentTerms ?? "").Trim();
                entity.PaymentDayOfMonth = PaymentDayOfMonth;
                entity.CreditLimit = CreditLimit;
                entity.IsActive = IsActive;
                entity.UpdatedAt = DateTime.Now;
                _ctx.Save();
            }

            SyncOverrides(entity.Id);
            Reload();
            SelectedRow = Branches.FirstOrDefault(x => x.Branch.Id == entity.Id);
        }

        private void SyncOverrides(int branchId)
        {
            var existing = _ctx.Db.BranchPriceOverrides.Where(x => x.BranchId == branchId).ToList();
            _ctx.Db.BranchPriceOverrides.RemoveRange(existing);

            foreach (var row in Overrides)
            {
                _ctx.Db.BranchPriceOverrides.Add(new BranchPriceOverride
                {
                    BranchId = branchId,
                    ProductId = row.ProductId,
                    UnitPrice = row.UnitPrice
                });
            }

            _ctx.Save();
        }

        private void Delete()
        {
            if (Selected == null) return;

            if (!ConfirmCommand(_ctx.Loc["ConfirmDelete"], _ctx.Loc["Confirm"]))
            {
                return;
            }

            var branchId = Selected.Id;
            var branch = _ctx.Db.Branches.FirstOrDefault(x => x.Id == branchId);
            var overrides = _ctx.Db.BranchPriceOverrides.Where(x => x.BranchId == branchId).ToList();
            var hasHistory =
                _ctx.Db.Shipments.Any(x => x.BranchId == branchId) ||
                _ctx.Db.Payments.Any(x => x.BranchId == branchId) ||
                _ctx.Db.ReturnReceipts.Any(x => x.BranchId == branchId);

            if (branch != null && hasHistory)
            {
                branch.IsActive = false;
                branch.UpdatedAt = DateTime.Now;
                _ctx.Db.BranchPriceOverrides.RemoveRange(overrides);
                _ctx.Save();

                System.Windows.MessageBox.Show(
                    "Hareketli cari silinmedi. Kayit pasif yapildi.",
                    _ctx.Loc["Confirm"],
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                SucceedCommand("Pasif Yapildi");
            }
            else
            {
                _ctx.Db.BranchPriceOverrides.RemoveRange(overrides);
                if (branch != null)
                {
                    _ctx.Db.Branches.Remove(branch);
                }

                _ctx.Save();
            }

            Reload();
            StartNewEntry();
        }

        private void AddOrUpdateOverride()
        {
            if (OverrideProductId <= 0) return;

            if (OverrideUnitPrice < 0)
            {
                FailCommand("Ozel fiyat negatif olamaz.", _ctx.Loc["Confirm"]);
            }

            var product = Products.FirstOrDefault(x => x.Id == OverrideProductId);
            if (product == null) return;

            var existing = Overrides.FirstOrDefault(x => x.ProductId == OverrideProductId);
            if (existing == null)
            {
                Overrides.Add(new BranchPriceOverrideRow
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = OverrideUnitPrice
                });
            }
            else
            {
                existing.UnitPrice = OverrideUnitPrice;
            }

            SelectedOverride = null;
            PersistOverridesIfSelectedBranch("Ozel Fiyat Kaydedildi");
        }

        private void RemoveOverride()
        {
            if (SelectedOverride == null) return;
            Overrides.Remove(SelectedOverride);
            SelectedOverride = null;
            PersistOverridesIfSelectedBranch("Ozel Fiyat Kaldirildi");
        }

        private void ViewStatement()
        {
            if (Selected == null) return;
            _showStatement?.Invoke(Selected.Id);
        }

        private void OpenCollections()
        {
            if (Selected == null) return;
            _showPayments?.Invoke(Selected.Id);
        }

        private void PersistOverridesIfSelectedBranch(string buttonText)
        {
            if (Selected == null || Selected.Id <= 0)
            {
                return;
            }

            SyncOverrides(Selected.Id);
            RefreshOverrides();
            SucceedCommand(buttonText);
        }

        private void RaiseSelectionStateProperties()
        {
            Raise(nameof(HasSelectedBranch));
            Raise(nameof(FormModeTitle));
            Raise(nameof(FormModeHint));
            Raise(nameof(SelectedBranchSummary));
            DeleteCommand.RaiseCanExecuteChanged();
            ViewStatementCommand.RaiseCanExecuteChanged();
            OpenCollectionsCommand.RaiseCanExecuteChanged();
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
    }
}
