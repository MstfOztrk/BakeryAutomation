using System;
using System.Collections.ObjectModel;
using System.Linq;
using BakeryAutomation.Models;
using BakeryAutomation.Services;
using Microsoft.EntityFrameworkCore;

namespace BakeryAutomation.ViewModels
{
    public sealed class ProductsViewModel : ObservableObject
    {
        private readonly BakeryAppContext _ctx;

        public class EnumDisplay<T>
        {
            public T? Value { get; set; }
            public string Name { get; set; } = "";
        }

        public System.Collections.Generic.List<EnumDisplay<UnitType>> UnitTypes => new()
        {
            new() { Value = UnitType.Piece, Name = _ctx.Loc["Unit_Piece"] },
            new() { Value = UnitType.Kilogram, Name = _ctx.Loc["Unit_Kg"] },
            new() { Value = UnitType.Tray, Name = _ctx.Loc["Unit_Tray"] }
        };

        public ObservableCollection<Product> Products { get; } = new();

        private Product? _selected;
        public Product? Selected
        {
            get => _selected;
            set
            {
                if (!Set(ref _selected, value)) return;
                LoadSelectedIntoForm();
                RaiseFormStateProperties();
            }
        }

        public bool HasSelectedProduct => Selected != null;

        public string FormModeTitle => Selected == null
            ? "Yeni Urun Kaydi"
            : $"Duzenlenen Urun: {Selected.Name}";

        public string FormModeHint => Selected == null
            ? "Yeni urun eklemek icin form bos. Mevcut kaydi duzenlemekten cikmak icin tekrar secim yapmaniza gerek yok."
            : "Bu alanlar secili urunu gunceller. Yeni bir urun acmak icin 'Yeni Kayit' dugmesine basin.";

        private string _name = "";
        public string Name { get => _name; set => Set(ref _name, value); }

        private string _category = "";
        public string Category { get => _category; set => Set(ref _category, value); }

        private UnitType _unitType = UnitType.Piece;
        public UnitType UnitType { get => _unitType; set => Set(ref _unitType, value); }

        private decimal _defaultUnitPrice;
        public decimal DefaultUnitPrice { get => _defaultUnitPrice; set => Set(ref _defaultUnitPrice, value); }

        private bool _isActive = true;
        public bool IsActive { get => _isActive; set => Set(ref _isActive, value); }

        private string _priceNote = "";
        public string PriceNote { get => _priceNote; set => Set(ref _priceNote, value); }

        public RelayCommand NewCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand RefreshCommand { get; }

        public ProductsViewModel(BakeryAppContext ctx)
        {
            _ctx = ctx;
            NewCommand = new RelayCommand(_ => StartNewEntry());
            SaveCommand = new RelayCommand(_ => Save());
            DeleteCommand = new RelayCommand(_ => Delete(), _ => Selected != null);
            RefreshCommand = new RelayCommand(_ => Reload());

            Reload();
        }

        private void Reload()
        {
            Products.Clear();
            var list = _ctx.Db.Products
                .AsNoTracking()
                .Include(p => p.PriceHistory)
                .OrderBy(p => p.Name)
                .ToList();

            for (int i = 0; i < list.Count; i++) Products.Add(list[i]);
        }

        private void LoadSelectedIntoForm()
        {
            if (Selected == null)
            {
                ResetFormFields();
                return;
            }

            Name = Selected.Name;
            Category = Selected.Category;
            UnitType = Selected.UnitType;
            DefaultUnitPrice = Selected.DefaultUnitPrice;
            IsActive = Selected.IsActive;
            PriceNote = "";
        }

        private void StartNewEntry()
        {
            Set(ref _selected, null, nameof(Selected));
            ResetFormFields();
            RaiseFormStateProperties();
        }

        private void ResetFormFields()
        {
            Name = "";
            Category = "";
            UnitType = UnitType.Piece;
            DefaultUnitPrice = 0m;
            IsActive = true;
            PriceNote = "";
        }

        private void Save()
        {
            var normalizedName = (Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                FailCommand("Urun adi bos birakilamaz.", _ctx.Loc["Confirm"]);
            }

            if (DefaultUnitPrice < 0)
            {
                FailCommand("Varsayilan fiyat negatif olamaz.", _ctx.Loc["Confirm"]);
            }

            var duplicateExists = _ctx.Db.Products
                .AsNoTracking()
                .Select(x => new { x.Id, x.Name })
                .AsEnumerable()
                .Any(x =>
                    x.Id != (Selected?.Id ?? 0) &&
                    string.Equals(x.Name?.Trim(), normalizedName, StringComparison.CurrentCultureIgnoreCase));

            if (duplicateExists)
            {
                FailCommand("Ayni adla baska bir urun kaydi var.", _ctx.Loc["Confirm"]);
            }

            if (Selected == null)
            {
                var p = new Product
                {
                    // Id autogenerated
                    Name = normalizedName,
                    Category = (Category ?? string.Empty).Trim(),
                    UnitType = UnitType,
                    DefaultUnitPrice = DefaultUnitPrice,
                    IsActive = IsActive,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                if (p.DefaultUnitPrice > 0)
                {
                    p.PriceHistory.Add(new PriceChange
                    {
                        At = DateTime.Now,
                        OldPrice = 0m,
                        NewPrice = p.DefaultUnitPrice,
                        Note = string.IsNullOrWhiteSpace(PriceNote) ? "Initial" : PriceNote.Trim()
                    });
                }

                _ctx.Db.Products.Add(p);
            }
            else
            {
                var entity = _ctx.Db.Products
                    .Include(p => p.PriceHistory)
                    .FirstOrDefault(p => p.Id == Selected.Id);

                if (entity == null)
                {
                    FailCommand("Secili urun bulunamadi.", _ctx.Loc["Confirm"]);
                }

                var oldPrice = entity.DefaultUnitPrice;

                entity.Name = normalizedName;
                entity.Category = (Category ?? string.Empty).Trim();
                entity.UnitType = UnitType;
                entity.IsActive = IsActive;

                if (entity.DefaultUnitPrice != DefaultUnitPrice)
                {
                    entity.PriceHistory.Add(new PriceChange
                    {
                        At = DateTime.Now,
                        OldPrice = oldPrice,
                        NewPrice = DefaultUnitPrice,
                        Note = string.IsNullOrWhiteSpace(PriceNote) ? "Price update" : PriceNote.Trim()
                    });

                    entity.DefaultUnitPrice = DefaultUnitPrice;
                }

                entity.UpdatedAt = DateTime.Now;
            }

            _ctx.Save();
            Reload();
            StartNewEntry();
        }

        private void Delete()
        {
            if (Selected == null) return;

            if (!ConfirmCommand(_ctx.Loc["ConfirmDelete"], _ctx.Loc["Confirm"]))
            {
                return;
            }

            var product = _ctx.Db.Products
                .Include(p => p.PriceHistory)
                .FirstOrDefault(p => p.Id == Selected.Id);

            if (product == null) return;

            var overrides = _ctx.Db.BranchPriceOverrides.Where(x => x.ProductId == product.Id).ToList();
            var priceHistory = _ctx.Db.PriceChanges.Where(x => x.ProductId == product.Id).ToList();
            var hasUsage =
                _ctx.Db.ShipmentItems.Any(x => x.ProductId == product.Id) ||
                _ctx.Db.ReturnReceiptItems.Any(x => x.ProductId == product.Id) ||
                _ctx.Db.DirectSaleItems.Any(x => x.ProductId == product.Id);

            if (hasUsage)
            {
                product.IsActive = false;
                product.UpdatedAt = DateTime.Now;
                _ctx.Db.BranchPriceOverrides.RemoveRange(overrides);
                _ctx.Save();

                System.Windows.MessageBox.Show(
                    "Hareketli urun silinmedi. Urun pasif yapildi.",
                    _ctx.Loc["Confirm"],
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                SucceedCommand("Pasif Yapildi");
            }
            else
            {
                _ctx.Db.BranchPriceOverrides.RemoveRange(overrides);
                _ctx.Db.PriceChanges.RemoveRange(priceHistory);
                _ctx.Db.Products.Remove(product);
                _ctx.Save();
            }

            Reload();
            StartNewEntry();
        }

        private void RaiseFormStateProperties()
        {
            Raise(nameof(HasSelectedProduct));
            Raise(nameof(FormModeTitle));
            Raise(nameof(FormModeHint));
            DeleteCommand.RaiseCanExecuteChanged();
        }
    }
}
