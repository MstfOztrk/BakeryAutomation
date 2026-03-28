import os, textwrap, pathlib

root = pathlib.Path('/mnt/data/BakeryAutomationApp')
proj = root/'BakeryAutomation'


def w(path: pathlib.Path, content: str):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(textwrap.dedent(content).lstrip('\n'), encoding='utf-8')

# -------------------- Core project files --------------------
w(proj/'MainWindow.xaml', r'''
<Window x:Class="BakeryAutomation.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:vm="clr-namespace:BakeryAutomation.ViewModels"
        mc:Ignorable="d"
        Title="Bakery Automation" Height="860" Width="1320" WindowStartupLocation="CenterScreen">

    <Window.DataContext>
        <vm:MainViewModel />
    </Window.DataContext>

    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="260" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>

        <!-- Sidebar -->
        <Border Grid.Column="0" Background="{StaticResource SidebarBrush}" Padding="14">
            <DockPanel LastChildFill="True">
                <StackPanel DockPanel.Dock="Top" Margin="0,6,0,18">
                    <TextBlock Text="Bakery" FontSize="20" FontWeight="SemiBold" Foreground="White" />
                    <TextBlock Text="Otomasyon" FontSize="14" Foreground="#FF9CA3AF" />
                </StackPanel>

                <StackPanel>
                    <ToggleButton Style="{StaticResource SidebarNavButton}" IsChecked="{Binding IsDashboardSelected}" Command="{Binding ShowDashboardCommand}" Content="Dashboard" />
                    <ToggleButton Style="{StaticResource SidebarNavButton}" IsChecked="{Binding IsProductsSelected}" Command="{Binding ShowProductsCommand}" Content="Ürünler" />
                    <ToggleButton Style="{StaticResource SidebarNavButton}" IsChecked="{Binding IsBranchesSelected}" Command="{Binding ShowBranchesCommand}" Content="Şubeler / Cariler" />
                    <ToggleButton Style="{StaticResource SidebarNavButton}" IsChecked="{Binding IsShipmentsSelected}" Command="{Binding ShowShipmentsCommand}" Content="Sevkiyat (Batch)" />
                    <ToggleButton Style="{StaticResource SidebarNavButton}" IsChecked="{Binding IsPaymentsSelected}" Command="{Binding ShowPaymentsCommand}" Content="Tahsilat" />
                    <ToggleButton Style="{StaticResource SidebarNavButton}" IsChecked="{Binding IsReportsSelected}" Command="{Binding ShowReportsCommand}" Content="Raporlar" />
                    <ToggleButton Style="{StaticResource SidebarNavButton}" IsChecked="{Binding IsSettingsSelected}" Command="{Binding ShowSettingsCommand}" Content="Ayarlar" />
                </StackPanel>

                <StackPanel DockPanel.Dock="Bottom" Margin="0,14,0,0">
                    <TextBlock Text="Tek kullanıcı • JSON veri" Foreground="#FF9CA3AF" FontSize="12" />
                    <TextBlock Text="Ctrl+S kaydetmez (butonlar)" Foreground="#FF6B7280" FontSize="12" />
                </StackPanel>
            </DockPanel>
        </Border>

        <!-- Content -->
        <Grid Grid.Column="1" Margin="18">
            <ContentControl Content="{Binding Current}" />
        </Grid>
    </Grid>
</Window>
''')

w(proj/'MainWindow.xaml.cs', r'''
using System.Windows;

namespace BakeryAutomation
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
''')

# -------------------- Models updates (snapshots for UI) --------------------
w(proj/'Models/ShipmentBatch.cs', r'''
using System;
using System.Collections.Generic;

namespace BakeryAutomation.Models
{
    public sealed class ShipmentItem
    {
        public int ProductId { get; set; }

        // Snapshot fields for reporting/UI (keeps history even if product name changes)
        public string ProductName { get; set; } = "";
        public UnitType UnitType { get; set; } = UnitType.Piece;

        public decimal QuantitySent { get; set; }
        public decimal UnitPrice { get; set; }

        // Item-level discount (iskonto %)
        public decimal ItemDiscountPercent { get; set; }

        // Returns / waste (from the same batch)
        public decimal QuantityReturned { get; set; }
        public decimal QuantityWasted { get; set; }
    }

    public sealed class ShipmentBatch
    {
        public int Id { get; set; }
        public string BatchNo { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.Today;
        public int BranchId { get; set; }
        public string Notes { get; set; } = "";

        // Batch-level discount (iskonto %)
        public decimal BatchDiscountPercent { get; set; }

        public List<ShipmentItem> Items { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
''')

w(proj/'Models/DirectSale.cs', r'''
using System;
using System.Collections.Generic;

namespace BakeryAutomation.Models
{
    public sealed class DirectSaleItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public UnitType UnitType { get; set; } = UnitType.Piece;

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
    }

    public sealed class DirectSale
    {
        public int Id { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
        public string Note { get; set; } = "";
        public List<DirectSaleItem> Items { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
''')

# -------------------- ViewModels updates & additions --------------------
w(proj/'ViewModels/MainViewModel.cs', r'''
using BakeryAutomation.Services;

namespace BakeryAutomation.ViewModels
{
    public sealed class MainViewModel : ObservableObject
    {
        private ObservableObject _current;
        public ObservableObject Current
        {
            get => _current;
            set => Set(ref _current, value);
        }

        public AppContext Ctx { get; }

        public bool IsDashboardSelected { get => _isDashboardSelected; set => Set(ref _isDashboardSelected, value); }
        public bool IsProductsSelected { get => _isProductsSelected; set => Set(ref _isProductsSelected, value); }
        public bool IsBranchesSelected { get => _isBranchesSelected; set => Set(ref _isBranchesSelected, value); }
        public bool IsShipmentsSelected { get => _isShipmentsSelected; set => Set(ref _isShipmentsSelected, value); }
        public bool IsPaymentsSelected { get => _isPaymentsSelected; set => Set(ref _isPaymentsSelected, value); }
        public bool IsReportsSelected { get => _isReportsSelected; set => Set(ref _isReportsSelected, value); }
        public bool IsSettingsSelected { get => _isSettingsSelected; set => Set(ref _isSettingsSelected, value); }

        private bool _isDashboardSelected;
        private bool _isProductsSelected;
        private bool _isBranchesSelected;
        private bool _isShipmentsSelected;
        private bool _isPaymentsSelected;
        private bool _isReportsSelected;
        private bool _isSettingsSelected;

        public RelayCommand ShowDashboardCommand { get; }
        public RelayCommand ShowProductsCommand { get; }
        public RelayCommand ShowBranchesCommand { get; }
        public RelayCommand ShowShipmentsCommand { get; }
        public RelayCommand ShowPaymentsCommand { get; }
        public RelayCommand ShowReportsCommand { get; }
        public RelayCommand ShowSettingsCommand { get; }

        public MainViewModel()
        {
            Ctx = new AppContext();

            ShowDashboardCommand = new RelayCommand(_ => Navigate(Section.Dashboard));
            ShowProductsCommand = new RelayCommand(_ => Navigate(Section.Products));
            ShowBranchesCommand = new RelayCommand(_ => Navigate(Section.Branches));
            ShowShipmentsCommand = new RelayCommand(_ => Navigate(Section.Shipments));
            ShowPaymentsCommand = new RelayCommand(_ => Navigate(Section.Payments));
            ShowReportsCommand = new RelayCommand(_ => Navigate(Section.Reports));
            ShowSettingsCommand = new RelayCommand(_ => Navigate(Section.Settings));

            _current = new DashboardViewModel(Ctx);
            SetSelected(Section.Dashboard);
        }

        private enum Section
        {
            Dashboard,
            Products,
            Branches,
            Shipments,
            Payments,
            Reports,
            Settings
        }

        private void Navigate(Section section)
        {
            SetSelected(section);

            Current = section switch
            {
                Section.Dashboard => new DashboardViewModel(Ctx),
                Section.Products => new ProductsViewModel(Ctx),
                Section.Branches => new BranchesViewModel(Ctx),
                Section.Shipments => new ShipmentsViewModel(Ctx),
                Section.Payments => new PaymentsViewModel(Ctx),
                Section.Reports => new ReportsViewModel(Ctx),
                Section.Settings => new SettingsViewModel(Ctx),
                _ => Current
            };
        }

        private void SetSelected(Section section)
        {
            IsDashboardSelected = section == Section.Dashboard;
            IsProductsSelected = section == Section.Products;
            IsBranchesSelected = section == Section.Branches;
            IsShipmentsSelected = section == Section.Shipments;
            IsPaymentsSelected = section == Section.Payments;
            IsReportsSelected = section == Section.Reports;
            IsSettingsSelected = section == Section.Settings;
        }
    }
}
''')

w(proj/'ViewModels/BranchesViewModel.cs', r'''
using System;
using System.Collections.ObjectModel;
using System.Linq;
using BakeryAutomation.Models;
using BakeryAutomation.Services;

namespace BakeryAutomation.ViewModels
{
    public sealed class BranchPriceOverrideRow : ObservableObject
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";

        private decimal _unitPrice;
        public decimal UnitPrice { get => _unitPrice; set => Set(ref _unitPrice, value); }
    }

    public sealed class BranchesViewModel : ObservableObject
    {
        private readonly AppContext _ctx;

        public Array BranchTypes => Enum.GetValues(typeof(BranchType));

        public ObservableCollection<Branch> Branches { get; } = new();
        public ObservableCollection<Product> Products { get; } = new();
        public ObservableCollection<BranchPriceOverrideRow> Overrides { get; } = new();

        private Branch? _selected;
        public Branch? Selected
        {
            get => _selected;
            set
            {
                if (!Set(ref _selected, value)) return;
                LoadSelectedIntoForm();
            }
        }

        // Form fields
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

        // Override editor
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
                if (value == null) return;
                OverrideProductId = value.ProductId;
                OverrideUnitPrice = value.UnitPrice;
            }
        }

        public RelayCommand NewCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand RefreshCommand { get; }

        public RelayCommand AddOrUpdateOverrideCommand { get; }
        public RelayCommand RemoveOverrideCommand { get; }

        public BranchesViewModel(AppContext ctx)
        {
            _ctx = ctx;

            NewCommand = new RelayCommand(_ => ClearForm());
            SaveCommand = new RelayCommand(_ => Save());
            DeleteCommand = new RelayCommand(_ => Delete(), _ => Selected != null);
            RefreshCommand = new RelayCommand(_ => Reload());

            AddOrUpdateOverrideCommand = new RelayCommand(_ => AddOrUpdateOverride(), _ => Selected != null && OverrideProductId > 0);
            RemoveOverrideCommand = new RelayCommand(_ => RemoveOverride(), _ => Selected != null && SelectedOverride != null);

            Reload();
        }

        private void Reload()
        {
            Branches.Clear();
            Products.Clear();

            var plist = _ctx.Data.Products.OrderBy(p => p.Name).ToList();
            for (int i = 0; i < plist.Count; i++) Products.Add(plist[i]);

            var blist = _ctx.Data.Branches.OrderBy(b => b.Name).ToList();
            for (int i = 0; i < blist.Count; i++) Branches.Add(blist[i]);

            RefreshOverrides();
        }

        private void RefreshOverrides()
        {
            Overrides.Clear();
            SelectedOverride = null;
            OverrideProductId = 0;
            OverrideUnitPrice = 0;

            if (Selected == null) return;

            var rows = _ctx.Data.BranchPriceOverrides
                .Where(x => x.BranchId == Selected.Id)
                .Join(_ctx.Data.Products, x => x.ProductId, p => p.Id, (x, p) => new BranchPriceOverrideRow
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    UnitPrice = x.UnitPrice
                })
                .OrderBy(r => r.ProductName)
                .ToList();

            for (int i = 0; i < rows.Count; i++) Overrides.Add(rows[i]);
        }

        private void LoadSelectedIntoForm()
        {
            if (Selected == null)
            {
                ClearForm();
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

        private void ClearForm()
        {
            Selected = null;
            Name = "";
            Type = BranchType.Branch;
            Address = "";
            ContactName = "";
            Phone = "";
            PaymentTerms = "";
            PaymentDayOfMonth = null;
            CreditLimit = 0;
            IsActive = true;

            Overrides.Clear();
            SelectedOverride = null;
            OverrideProductId = 0;
            OverrideUnitPrice = 0;
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(Name)) return;

            var data = _ctx.Data;

            if (Selected == null)
            {
                var b = new Branch
                {
                    Id = data.NextBranchId++,
                    Name = Name.Trim(),
                    Type = Type,
                    Address = Address.Trim(),
                    ContactName = ContactName.Trim(),
                    Phone = Phone.Trim(),
                    PaymentTerms = PaymentTerms.Trim(),
                    PaymentDayOfMonth = PaymentDayOfMonth,
                    CreditLimit = CreditLimit,
                    IsActive = IsActive,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                data.Branches.Add(b);
            }
            else
            {
                Selected.Name = Name.Trim();
                Selected.Type = Type;
                Selected.Address = Address.Trim();
                Selected.ContactName = ContactName.Trim();
                Selected.Phone = Phone.Trim();
                Selected.PaymentTerms = PaymentTerms.Trim();
                Selected.PaymentDayOfMonth = PaymentDayOfMonth;
                Selected.CreditLimit = CreditLimit;
                Selected.IsActive = IsActive;
                Selected.UpdatedAt = DateTime.Now;

                // Sync overrides list back to model
                data.BranchPriceOverrides.RemoveAll(x => x.BranchId == Selected.Id);
                for (int i = 0; i < Overrides.Count; i++)
                {
                    data.BranchPriceOverrides.Add(new BranchPriceOverride
                    {
                        BranchId = Selected.Id,
                        ProductId = Overrides[i].ProductId,
                        UnitPrice = Overrides[i].UnitPrice
                    });
                }
            }

            _ctx.Save();
            Reload();
            ClearForm();
        }

        private void Delete()
        {
            if (Selected == null) return;

            var data = _ctx.Data;
            var branchId = Selected.Id;

            data.Branches.RemoveAll(b => b.Id == branchId);
            data.BranchPriceOverrides.RemoveAll(x => x.BranchId == branchId);
            data.Shipments.RemoveAll(s => s.BranchId == branchId);
            data.Payments.RemoveAll(p => p.BranchId == branchId);

            _ctx.Save();
            Reload();
            ClearForm();
        }

        private void AddOrUpdateOverride()
        {
            if (Selected == null) return;
            if (OverrideProductId <= 0) return;

            var product = _ctx.Data.Products.FirstOrDefault(p => p.Id == OverrideProductId);
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
            OverrideProductId = 0;
            OverrideUnitPrice = 0;

            // Save fast
            SaveCommand.Execute(null);
        }

        private void RemoveOverride()
        {
            if (Selected == null || SelectedOverride == null) return;

            Overrides.Remove(SelectedOverride);
            SelectedOverride = null;
            OverrideProductId = 0;
            OverrideUnitPrice = 0;

            SaveCommand.Execute(null);
        }
    }
}
''')

w(proj/'ViewModels/ShipmentsViewModel.cs', r'''
using System;
using System.Collections.ObjectModel;
using System.Linq;
using BakeryAutomation.Models;
using BakeryAutomation.Services;

namespace BakeryAutomation.ViewModels
{
    public sealed class ShipmentBatchRow
    {
        public ShipmentBatch Batch { get; set; } = new();
        public string Display { get; set; } = "";
    }

    public sealed class ShipmentsViewModel : ObservableObject
    {
        private readonly AppContext _ctx;

        public ObservableCollection<Branch> Branches { get; } = new();
        public ObservableCollection<Product> Products { get; } = new();

        public ObservableCollection<ShipmentBatchRow> Batches { get; } = new();

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
            }
        }

        // Batch form
        private string _batchNo = "";
        public string BatchNo { get => _batchNo; set => Set(ref _batchNo, value); }

        private DateTime _date = DateTime.Today;
        public DateTime Date { get => _date; set => Set(ref _date, value); }

        private int _branchId;
        public int BranchId { get => _branchId; set => Set(ref _branchId, value); }

        private decimal _batchDiscountPercent;
        public decimal BatchDiscountPercent { get => _batchDiscountPercent; set => Set(ref _batchDiscountPercent, value); }

        private string _notes = "";
        public string Notes { get => _notes; set => Set(ref _notes, value); }

        public ObservableCollection<ShipmentItem> Items { get; } = new();

        private ShipmentItem? _selectedItem;
        public ShipmentItem? SelectedItem
        {
            get => _selectedItem;
            set => Set(ref _selectedItem, value);
        }

        // Add item panel
        private int _newProductId;
        public int NewProductId { get => _newProductId; set => Set(ref _newProductId, value); }

        private decimal _newQtySent;
        public decimal NewQtySent { get => _newQtySent; set => Set(ref _newQtySent, value); }

        private decimal _newUnitPrice;
        public decimal NewUnitPrice { get => _newUnitPrice; set => Set(ref _newUnitPrice, value); }

        private decimal _newItemDiscountPercent;
        public decimal NewItemDiscountPercent { get => _newItemDiscountPercent; set => Set(ref _newItemDiscountPercent, value); }

        // Totals
        private decimal _subtotal;
        public decimal Subtotal { get => _subtotal; set => Set(ref _subtotal, value); }

        private decimal _total;
        public decimal Total { get => _total; set => Set(ref _total, value); }

        public RelayCommand NewBatchCommand { get; }
        public RelayCommand SaveBatchCommand { get; }
        public RelayCommand DeleteBatchCommand { get; }
        public RelayCommand RefreshCommand { get; }

        public RelayCommand AddItemCommand { get; }
        public RelayCommand RemoveItemCommand { get; }
        public RelayCommand RecalculateCommand { get; }
        public RelayCommand FillUnitPriceCommand { get; }

        public ShipmentsViewModel(AppContext ctx)
        {
            _ctx = ctx;

            NewBatchCommand = new RelayCommand(_ => NewBatch());
            SaveBatchCommand = new RelayCommand(_ => SaveBatch(), _ => SelectedBatch != null);
            DeleteBatchCommand = new RelayCommand(_ => DeleteBatch(), _ => SelectedBatch != null);
            RefreshCommand = new RelayCommand(_ => Reload());

            AddItemCommand = new RelayCommand(_ => AddItem(), _ => SelectedBatch != null && NewProductId > 0 && NewQtySent > 0);
            RemoveItemCommand = new RelayCommand(_ => RemoveItem(), _ => SelectedBatch != null && SelectedItem != null);
            RecalculateCommand = new RelayCommand(_ => Recalculate());
            FillUnitPriceCommand = new RelayCommand(_ => FillUnitPrice(), _ => NewProductId > 0 && BranchId > 0);

            Reload();
        }

        private void Reload()
        {
            Branches.Clear();
            Products.Clear();
            Batches.Clear();

            var bl = _ctx.Data.Branches.OrderBy(b => b.Name).ToList();
            for (int i = 0; i < bl.Count; i++) Branches.Add(bl[i]);

            var pl = _ctx.Data.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToList();
            for (int i = 0; i < pl.Count; i++) Products.Add(pl[i]);

            var batches = _ctx.Data.Shipments.OrderByDescending(s => s.Date).ThenByDescending(s => s.Id).ToList();
            for (int i = 0; i < batches.Count; i++)
            {
                var b = batches[i];
                var branchName = _ctx.Data.Branches.FirstOrDefault(x => x.Id == b.BranchId)?.Name ?? "(silinmiş)";
                Batches.Add(new ShipmentBatchRow
                {
                    Batch = b,
                    Display = $"{b.Date:yyyy-MM-dd} • {branchName} • {b.BatchNo}"
                });
            }

            SelectedRow = Batches.FirstOrDefault();
        }

        private void NewBatch()
        {
            var data = _ctx.Data;
            var id = data.NextShipmentId++;

            var batch = new ShipmentBatch
            {
                Id = id,
                Date = DateTime.Today,
                BatchNo = $"S-{DateTime.Today:yyyyMMdd}-{id:0000}",
                BranchId = Branches.FirstOrDefault()?.Id ?? 0,
                BatchDiscountPercent = 0,
                Notes = "",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            data.Shipments.Add(batch);
            _ctx.Save();
            Reload();

            // select created
            SelectedRow = Batches.FirstOrDefault(x => x.Batch.Id == id);
        }

        private void LoadBatchIntoForm()
        {
            Items.Clear();
            SelectedItem = null;
            NewProductId = 0;
            NewQtySent = 0;
            NewUnitPrice = 0;
            NewItemDiscountPercent = 0;

            if (SelectedBatch == null)
            {
                BatchNo = "";
                Date = DateTime.Today;
                BranchId = 0;
                Notes = "";
                BatchDiscountPercent = 0;
                Subtotal = 0;
                Total = 0;
                return;
            }

            BatchNo = SelectedBatch.BatchNo;
            Date = SelectedBatch.Date;
            BranchId = SelectedBatch.BranchId;
            Notes = SelectedBatch.Notes;
            BatchDiscountPercent = SelectedBatch.BatchDiscountPercent;

            for (int i = 0; i < SelectedBatch.Items.Count; i++) Items.Add(SelectedBatch.Items[i]);

            Recalculate();
        }

        private void SaveBatch()
        {
            if (SelectedBatch == null) return;

            SelectedBatch.BatchNo = (BatchNo ?? "").Trim();
            SelectedBatch.Date = Date.Date;
            SelectedBatch.BranchId = BranchId;
            SelectedBatch.Notes = (Notes ?? "").Trim();
            SelectedBatch.BatchDiscountPercent = BatchDiscountPercent;
            SelectedBatch.UpdatedAt = DateTime.Now;

            // sync items (already references, but ensure list matches)
            SelectedBatch.Items = Items.ToList();

            _ctx.Save();
            Reload();
        }

        private void DeleteBatch()
        {
            if (SelectedBatch == null) return;

            var id = SelectedBatch.Id;
            _ctx.Data.Shipments.RemoveAll(s => s.Id == id);
            _ctx.Save();
            Reload();
        }

        private void FillUnitPrice()
        {
            if (NewProductId <= 0 || BranchId <= 0) return;
            NewUnitPrice = _ctx.Calc.ResolveUnitPrice(_ctx.Data, NewProductId, BranchId);
        }

        private void AddItem()
        {
            if (SelectedBatch == null) return;
            if (NewProductId <= 0 || NewQtySent <= 0) return;

            var product = _ctx.Data.Products.FirstOrDefault(p => p.Id == NewProductId);
            if (product == null) return;

            var unitPrice = NewUnitPrice;
            if (unitPrice <= 0) unitPrice = _ctx.Calc.ResolveUnitPrice(_ctx.Data, NewProductId, BranchId);

            var existing = Items.FirstOrDefault(x => x.ProductId == NewProductId);
            if (existing == null)
            {
                Items.Add(new ShipmentItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitType = product.UnitType,
                    QuantitySent = NewQtySent,
                    UnitPrice = unitPrice,
                    ItemDiscountPercent = NewItemDiscountPercent,
                    QuantityReturned = 0,
                    QuantityWasted = 0
                });
            }
            else
            {
                existing.QuantitySent += NewQtySent;
                existing.UnitPrice = unitPrice;
                existing.ItemDiscountPercent = NewItemDiscountPercent;
            }

            NewProductId = 0;
            NewQtySent = 0;
            NewUnitPrice = 0;
            NewItemDiscountPercent = 0;

            Recalculate();
            SaveBatch();
        }

        private void RemoveItem()
        {
            if (SelectedItem == null) return;
            Items.Remove(SelectedItem);
            SelectedItem = null;

            Recalculate();
            SaveBatch();
        }

        private void Recalculate()
        {
            if (SelectedBatch == null)
            {
                Subtotal = 0;
                Total = 0;
                return;
            }

            // temp batch for calculation
            var tmp = new ShipmentBatch
            {
                Id = SelectedBatch.Id,
                BatchNo = BatchNo,
                Date = Date,
                BranchId = BranchId,
                Notes = Notes,
                BatchDiscountPercent = BatchDiscountPercent,
                Items = Items.ToList()
            };

            Subtotal = _ctx.Calc.ShipmentSubtotal(tmp);
            Total = _ctx.Calc.ShipmentTotal(tmp);
        }
    }
}
''')

w(proj/'ViewModels/PaymentsViewModel.cs', r'''
using System;
using System.Collections.ObjectModel;
using System.Linq;
using BakeryAutomation.Models;
using BakeryAutomation.Services;

namespace BakeryAutomation.ViewModels
{
    public sealed class PaymentRow
    {
        public Payment Payment { get; set; } = new();
        public string BranchName { get; set; } = "";
        public string Display => $"{Payment.Date:yyyy-MM-dd} • {BranchName} • {Payment.Amount:n2}";
    }

    public sealed class PaymentsViewModel : ObservableObject
    {
        private readonly AppContext _ctx;

        public Array PaymentMethods => Enum.GetValues(typeof(PaymentMethod));
        public ObservableCollection<Branch> Branches { get; } = new();
        public ObservableCollection<PaymentRow> Payments { get; } = new();

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

        private int _branchId;
        public int BranchId { get => _branchId; set => Set(ref _branchId, value); }

        private DateTime _date = DateTime.Today;
        public DateTime Date { get => _date; set => Set(ref _date, value); }

        private decimal _amount;
        public decimal Amount { get => _amount; set => Set(ref _amount, value); }

        private PaymentMethod _method = PaymentMethod.Cash;
        public PaymentMethod Method { get => _method; set => Set(ref _method, value); }

        private string _note = "";
        public string Note { get => _note; set => Set(ref _note, value); }

        private string _reference = "";
        public string Reference { get => _reference; set => Set(ref _reference, value); }

        public RelayCommand NewCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand RefreshCommand { get; }

        public PaymentsViewModel(AppContext ctx)
        {
            _ctx = ctx;
            NewCommand = new RelayCommand(_ => ClearForm());
            SaveCommand = new RelayCommand(_ => Save());
            DeleteCommand = new RelayCommand(_ => Delete(), _ => Selected != null);
            RefreshCommand = new RelayCommand(_ => Reload());

            Reload();
        }

        private void Reload()
        {
            Branches.Clear();
            Payments.Clear();

            var bl = _ctx.Data.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToList();
            for (int i = 0; i < bl.Count; i++) Branches.Add(bl[i]);

            var list = _ctx.Data.Payments.OrderByDescending(p => p.Date).ThenByDescending(p => p.Id).ToList();
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                var bn = _ctx.Data.Branches.FirstOrDefault(b => b.Id == p.BranchId)?.Name ?? "(silinmiş)";
                Payments.Add(new PaymentRow { Payment = p, BranchName = bn });
            }

            SelectedRow = Payments.FirstOrDefault();
        }

        private void LoadSelectedIntoForm()
        {
            if (Selected == null)
            {
                ClearForm();
                return;
            }

            BranchId = Selected.BranchId;
            Date = Selected.Date;
            Amount = Selected.Amount;
            Method = Selected.Method;
            Note = Selected.Note;
            Reference = Selected.Reference;
        }

        private void ClearForm()
        {
            Selected = null;
            BranchId = Branches.FirstOrDefault()?.Id ?? 0;
            Date = DateTime.Today;
            Amount = 0;
            Method = PaymentMethod.Cash;
            Note = "";
            Reference = "";
        }

        private void Save()
        {
            if (BranchId <= 0) return;
            if (Amount <= 0) return;

            var data = _ctx.Data;

            if (Selected == null)
            {
                var p = new Payment
                {
                    Id = data.NextPaymentId++,
                    BranchId = BranchId,
                    Date = Date.Date,
                    Amount = Amount,
                    Method = Method,
                    Note = (Note ?? "").Trim(),
                    Reference = (Reference ?? "").Trim(),
                    CreatedAt = DateTime.Now
                };

                data.Payments.Add(p);
            }
            else
            {
                Selected.BranchId = BranchId;
                Selected.Date = Date.Date;
                Selected.Amount = Amount;
                Selected.Method = Method;
                Selected.Note = (Note ?? "").Trim();
                Selected.Reference = (Reference ?? "").Trim();
            }

            _ctx.Save();
            Reload();
            ClearForm();
        }

        private void Delete()
        {
            if (Selected == null) return;

            var id = Selected.Id;
            _ctx.Data.Payments.RemoveAll(p => p.Id == id);
            _ctx.Save();
            Reload();
            ClearForm();
        }
    }
}
''')

w(proj/'ViewModels/ReportsViewModel.cs', r'''
using System;
using System.Collections.ObjectModel;
using System.Linq;
using BakeryAutomation.Services;
using Microsoft.Win32;

namespace BakeryAutomation.ViewModels
{
    public sealed class DailyReportRow
    {
        public DateTime Date { get; set; }
        public int ShipmentCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal Payments { get; set; }
    }

    public sealed class ReportsViewModel : ObservableObject
    {
        private readonly AppContext _ctx;

        public ObservableCollection<BakeryAutomation.Models.Branch> Branches { get; } = new();
        public ObservableCollection<DailyReportRow> Rows { get; } = new();

        private DateTime _from = DateTime.Today.AddDays(-14);
        public DateTime From { get => _from; set => Set(ref _from, value); }

        private DateTime _to = DateTime.Today;
        public DateTime To { get => _to; set => Set(ref _to, value); }

        private int _branchId;
        public int BranchId { get => _branchId; set => Set(ref _branchId, value); }

        private int _totalShipments;
        public int TotalShipments { get => _totalShipments; set => Set(ref _totalShipments, value); }

        private decimal _totalRevenue;
        public decimal TotalRevenue { get => _totalRevenue; set => Set(ref _totalRevenue, value); }

        private decimal _totalPayments;
        public decimal TotalPayments { get => _totalPayments; set => Set(ref _totalPayments, value); }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand ExportCsvCommand { get; }

        public ReportsViewModel(AppContext ctx)
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
            // 0 means all
            Branches.Add(new BakeryAutomation.Models.Branch { Id = 0, Name = "(Tümü)" });

            var bl = _ctx.Data.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToList();
            for (int i = 0; i < bl.Count; i++) Branches.Add(bl[i]);

            BranchId = 0;
        }

        private void Refresh()
        {
            Rows.Clear();

            var from = From.Date;
            var to = To.Date;
            if (to < from) { var tmp = from; from = to; to = tmp; }

            var days = (to - from).Days;
            if (days > 3650) days = 3650;

            int shipmentCount = 0;
            decimal revenue = 0m;
            decimal payments = 0m;

            for (int i = 0; i <= days; i++)
            {
                var d = from.AddDays(i);

                var sh = _ctx.Data.Shipments.Where(s => s.Date.Date == d && (BranchId == 0 || s.BranchId == BranchId)).ToList();
                var pay = _ctx.Data.Payments.Where(p => p.Date.Date == d && (BranchId == 0 || p.BranchId == BranchId)).ToList();

                decimal dayRevenue = 0m;
                for (int k = 0; k < sh.Count; k++) dayRevenue += _ctx.Calc.ShipmentTotal(sh[k]);

                decimal dayPay = 0m;
                for (int k = 0; k < pay.Count; k++) dayPay += pay[k].Amount;

                Rows.Add(new DailyReportRow
                {
                    Date = d,
                    ShipmentCount = sh.Count,
                    Revenue = dayRevenue,
                    Payments = dayPay
                });

                shipmentCount += sh.Count;
                revenue += dayRevenue;
                payments += dayPay;
            }

            TotalShipments = shipmentCount;
            TotalRevenue = revenue;
            TotalPayments = payments;
        }

        private void ExportCsv()
        {
            var dlg = new SaveFileDialog
            {
                FileName = "report.csv",
                Filter = "CSV (*.csv)|*.csv|All files (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true) return;

            var headers = new[] { "Date", "ShipmentCount", "Revenue", "Payments" };
            var rows = Rows.Select(r => new[]
            {
                r.Date.ToString("yyyy-MM-dd"),
                r.ShipmentCount.ToString(),
                r.Revenue.ToString("0.00"),
                r.Payments.ToString("0.00")
            });

            _ctx.Csv.Export(dlg.FileName, headers, rows);
        }
    }
}
''')

w(proj/'ViewModels/SettingsViewModel.cs', r'''
using System.Diagnostics;
using BakeryAutomation.Services;
using Microsoft.Win32;

namespace BakeryAutomation.ViewModels
{
    public sealed class SettingsViewModel : ObservableObject
    {
        private readonly AppContext _ctx;

        public string DataFolder => _ctx.Storage.DataFolder;
        public string DataFile => _ctx.Storage.DataFilePath;

        public RelayCommand BackupCommand { get; }
        public RelayCommand RestoreCommand { get; }
        public RelayCommand OpenFolderCommand { get; }
        public RelayCommand ReloadCommand { get; }

        public SettingsViewModel(AppContext ctx)
        {
            _ctx = ctx;

            BackupCommand = new RelayCommand(_ => Backup());
            RestoreCommand = new RelayCommand(_ => Restore());
            OpenFolderCommand = new RelayCommand(_ => OpenFolder());
            ReloadCommand = new RelayCommand(_ => Reload());
        }

        private void Backup()
        {
            var dlg = new SaveFileDialog
            {
                FileName = "bakery_data_backup.json",
                Filter = "JSON (*.json)|*.json|All files (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true) return;
            _ctx.Storage.BackupTo(dlg.FileName);
        }

        private void Restore()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "JSON (*.json)|*.json|All files (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true) return;
            _ctx.Storage.RestoreFrom(dlg.FileName);
            _ctx.Reload();
        }

        private void OpenFolder()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = _ctx.Storage.DataFolder,
                UseShellExecute = true
            });
        }

        private void Reload()
        {
            _ctx.Reload();
        }
    }
}
''')

# -------------------- Views --------------------

def view(name, xaml, codebehind=True):
    w(proj/f'Views/{name}.xaml', xaml)
    if codebehind:
        w(proj/f'Views/{name}.xaml.cs', f'''\
using System.Windows.Controls;

namespace BakeryAutomation.Views
{{
    public partial class {name} : UserControl
    {{
        public {name}()
        {{
            InitializeComponent();
        }}
    }}
}}
''')

view('DashboardView', r'''
<UserControl x:Class="BakeryAutomation.Views.DashboardView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel>
            <TextBlock Text="Dashboard" FontSize="22" FontWeight="SemiBold" Margin="0,0,0,12" />

            <UniformGrid Columns="5" Margin="0,0,0,12">
                <Border Style="{StaticResource CardBorder}" Margin="0,0,10,0">
                    <StackPanel>
                        <TextBlock Text="Bugün Sevkiyat" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBlock Text="{Binding TodayShipmentCount}" FontSize="24" FontWeight="SemiBold" />
                    </StackPanel>
                </Border>
                <Border Style="{StaticResource CardBorder}" Margin="0,0,10,0">
                    <StackPanel>
                        <TextBlock Text="Bugün Net Ciro" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBlock Text="{Binding TodayNetRevenue, StringFormat={}{0:n2}}" FontSize="24" FontWeight="SemiBold" />
                    </StackPanel>
                </Border>
                <Border Style="{StaticResource CardBorder}" Margin="0,0,10,0">
                    <StackPanel>
                        <TextBlock Text="Toplam Alacak" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBlock Text="{Binding TotalReceivable, StringFormat={}{0:n2}}" FontSize="24" FontWeight="SemiBold" />
                    </StackPanel>
                </Border>
                <Border Style="{StaticResource CardBorder}" Margin="0,0,10,0">
                    <StackPanel>
                        <TextBlock Text="Aktif Şube" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBlock Text="{Binding ActiveBranches}" FontSize="24" FontWeight="SemiBold" />
                    </StackPanel>
                </Border>
                <Border Style="{StaticResource CardBorder}">
                    <StackPanel>
                        <TextBlock Text="Aktif Ürün" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBlock Text="{Binding ActiveProducts}" FontSize="24" FontWeight="SemiBold" />
                    </StackPanel>
                </Border>
            </UniformGrid>

            <Border Style="{StaticResource CardBorder}">
                <DockPanel>
                    <TextBlock DockPanel.Dock="Top" Text="En Çok Borçlu Şubeler" FontSize="16" FontWeight="SemiBold" Margin="0,0,0,10" />
                    <Button DockPanel.Dock="Top" Style="{StaticResource SecondaryButton}" Command="{Binding RefreshCommand}" Content="Yenile" Width="120" HorizontalAlignment="Left" />

                    <DataGrid ItemsSource="{Binding TopDebtors}">
                        <DataGrid.Columns>
                            <DataGridTextColumn Header="Şube" Binding="{Binding BranchName}" Width="*" />
                            <DataGridTextColumn Header="Bakiye" Binding="{Binding Balance, StringFormat={}{0:n2}}" Width="160" />
                        </DataGrid.Columns>
                    </DataGrid>
                </DockPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
''')

view('ProductsView', r'''
<UserControl x:Class="BakeryAutomation.Views.ProductsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <TextBlock Text="Ürünler" FontSize="22" FontWeight="SemiBold" Margin="0,0,0,12" />

        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="420" />
            </Grid.ColumnDefinitions>

            <Border Style="{StaticResource CardBorder}" Margin="0,0,12,0">
                <DockPanel>
                    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="0,0,0,8">
                        <Button Style="{StaticResource SecondaryButton}" Command="{Binding RefreshCommand}" Content="Yenile" Width="120" Margin="0,0,10,0" />
                    </StackPanel>

                    <DataGrid ItemsSource="{Binding Products}" SelectedItem="{Binding Selected}" IsReadOnly="True">
                        <DataGrid.Columns>
                            <DataGridTextColumn Header="Ad" Binding="{Binding Name}" Width="*" />
                            <DataGridTextColumn Header="Kategori" Binding="{Binding Category}" Width="160" />
                            <DataGridTextColumn Header="Birim" Binding="{Binding UnitType}" Width="90" />
                            <DataGridTextColumn Header="Fiyat" Binding="{Binding DefaultUnitPrice, StringFormat={}{0:n2}}" Width="120" />
                            <DataGridCheckBoxColumn Header="Aktif" Binding="{Binding IsActive}" Width="60" />
                        </DataGrid.Columns>
                    </DataGrid>
                </DockPanel>
            </Border>

            <Border Grid.Column="1" Style="{StaticResource CardBorder}">
                <ScrollViewer VerticalScrollBarVisibility="Auto">
                    <StackPanel>
                        <TextBlock Text="Ürün Kartı" FontSize="16" FontWeight="SemiBold" Margin="0,0,0,10" />

                        <TextBlock Text="Ad" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}" />

                        <TextBlock Text="Kategori" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding Category, UpdateSourceTrigger=PropertyChanged}" />

                        <TextBlock Text="Birim" Foreground="{StaticResource TextMutedBrush}" />
                        <ComboBox ItemsSource="{Binding UnitTypes}" SelectedItem="{Binding UnitType}" />

                        <TextBlock Text="KDV (%)" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding VatRate, UpdateSourceTrigger=PropertyChanged}" />

                        <TextBlock Text="Varsayılan Fiyat" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding DefaultUnitPrice, UpdateSourceTrigger=PropertyChanged}" />

                        <CheckBox Content="Aktif" IsChecked="{Binding IsActive}" Margin="0,0,0,8" />

                        <TextBlock Text="Fiyat Notu (opsiyonel)" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding PriceNote, UpdateSourceTrigger=PropertyChanged}" />

                        <Button Style="{StaticResource SecondaryButton}" Command="{Binding NewCommand}" Content="Yeni" />
                        <Button Style="{StaticResource PrimaryButton}" Command="{Binding SaveCommand}" Content="Kaydet" />
                        <Button Style="{StaticResource DangerButton}" Command="{Binding DeleteCommand}" Content="Sil" />
                    </StackPanel>
                </ScrollViewer>
            </Border>
        </Grid>
    </Grid>
</UserControl>
''')

view('BranchesView', r'''
<UserControl x:Class="BakeryAutomation.Views.BranchesView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <TextBlock Text="Şubeler / Cariler" FontSize="22" FontWeight="SemiBold" Margin="0,0,0,12" />

        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="520" />
            </Grid.ColumnDefinitions>

            <Border Style="{StaticResource CardBorder}" Margin="0,0,12,0">
                <DockPanel>
                    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="0,0,0,8">
                        <Button Style="{StaticResource SecondaryButton}" Command="{Binding RefreshCommand}" Content="Yenile" Width="120" Margin="0,0,10,0" />
                    </StackPanel>

                    <DataGrid ItemsSource="{Binding Branches}" SelectedItem="{Binding Selected}" IsReadOnly="True">
                        <DataGrid.Columns>
                            <DataGridTextColumn Header="Ad" Binding="{Binding Name}" Width="*" />
                            <DataGridTextColumn Header="Tip" Binding="{Binding Type}" Width="120" />
                            <DataGridTextColumn Header="Telefon" Binding="{Binding Phone}" Width="130" />
                            <DataGridTextColumn Header="Limit" Binding="{Binding CreditLimit, StringFormat={}{0:n2}}" Width="120" />
                            <DataGridCheckBoxColumn Header="Aktif" Binding="{Binding IsActive}" Width="60" />
                        </DataGrid.Columns>
                    </DataGrid>
                </DockPanel>
            </Border>

            <Border Grid.Column="1" Style="{StaticResource CardBorder}">
                <ScrollViewer VerticalScrollBarVisibility="Auto">
                    <StackPanel>
                        <TextBlock Text="Cari Kart" FontSize="16" FontWeight="SemiBold" Margin="0,0,0,10" />

                        <TextBlock Text="Ad" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}" />

                        <TextBlock Text="Tip" Foreground="{StaticResource TextMutedBrush}" />
                        <ComboBox ItemsSource="{Binding BranchTypes}" SelectedItem="{Binding Type}" />

                        <TextBlock Text="Adres" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding Address, UpdateSourceTrigger=PropertyChanged}" />

                        <TextBlock Text="Yetkili" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding ContactName, UpdateSourceTrigger=PropertyChanged}" />

                        <TextBlock Text="Telefon" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding Phone, UpdateSourceTrigger=PropertyChanged}" />

                        <TextBlock Text="Ödeme Şartı" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding PaymentTerms, UpdateSourceTrigger=PropertyChanged}" />

                        <TextBlock Text="Ödeme Günü (Ay içinde)" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding PaymentDayOfMonth, UpdateSourceTrigger=PropertyChanged}" />

                        <TextBlock Text="Kredi Limiti" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding CreditLimit, UpdateSourceTrigger=PropertyChanged}" />

                        <CheckBox Content="Aktif" IsChecked="{Binding IsActive}" Margin="0,0,0,8" />

                        <StackPanel Orientation="Horizontal" Margin="0,6,0,6">
                            <Button Style="{StaticResource SecondaryButton}" Command="{Binding NewCommand}" Content="Yeni" Width="120" Margin="0,0,10,0" />
                            <Button Style="{StaticResource PrimaryButton}" Command="{Binding SaveCommand}" Content="Kaydet" Width="120" Margin="0,0,10,0" />
                            <Button Style="{StaticResource DangerButton}" Command="{Binding DeleteCommand}" Content="Sil" Width="120" />
                        </StackPanel>

                        <Separator Margin="0,8,0,8" />
                        <TextBlock Text="Şubeye Özel Fiyat" FontSize="14" FontWeight="SemiBold" Margin="0,0,0,6" />

                        <DataGrid ItemsSource="{Binding Overrides}" SelectedItem="{Binding SelectedOverride}" IsReadOnly="True" Height="200">
                            <DataGrid.Columns>
                                <DataGridTextColumn Header="Ürün" Binding="{Binding ProductName}" Width="*" />
                                <DataGridTextColumn Header="Fiyat" Binding="{Binding UnitPrice, StringFormat={}{0:n2}}" Width="120" />
                            </DataGrid.Columns>
                        </DataGrid>

                        <TextBlock Text="Ürün" Foreground="{StaticResource TextMutedBrush}" />
                        <ComboBox ItemsSource="{Binding Products}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding OverrideProductId}" />

                        <TextBlock Text="Özel Fiyat" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding OverrideUnitPrice, UpdateSourceTrigger=PropertyChanged}" />

                        <StackPanel Orientation="Horizontal">
                            <Button Style="{StaticResource PrimaryButton}" Command="{Binding AddOrUpdateOverrideCommand}" Content="Ekle / Güncelle" Width="160" Margin="0,0,10,0" />
                            <Button Style="{StaticResource DangerButton}" Command="{Binding RemoveOverrideCommand}" Content="Kaldır" Width="120" />
                        </StackPanel>
                    </StackPanel>
                </ScrollViewer>
            </Border>
        </Grid>
    </Grid>
</UserControl>
''')

view('ShipmentsView', r'''
<UserControl x:Class="BakeryAutomation.Views.ShipmentsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <TextBlock Text="Sevkiyat (Batch)" FontSize="22" FontWeight="SemiBold" Margin="0,0,0,12" />

        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="340" />
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="420" />
            </Grid.ColumnDefinitions>

            <!-- Batch list -->
            <Border Style="{StaticResource CardBorder}" Margin="0,0,12,0">
                <DockPanel>
                    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="0,0,0,8">
                        <Button Style="{StaticResource PrimaryButton}" Command="{Binding NewBatchCommand}" Content="Yeni Batch" Width="120" Margin="0,0,10,0" />
                        <Button Style="{StaticResource SecondaryButton}" Command="{Binding RefreshCommand}" Content="Yenile" Width="120" />
                    </StackPanel>

                    <ListBox ItemsSource="{Binding Batches}" SelectedItem="{Binding SelectedRow}" DisplayMemberPath="Display" />
                </DockPanel>
            </Border>

            <!-- Items -->
            <Border Grid.Column="1" Style="{StaticResource CardBorder}" Margin="0,0,12,0">
                <DockPanel>
                    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="0,0,0,8">
                        <Button Style="{StaticResource SecondaryButton}" Command="{Binding RecalculateCommand}" Content="Hesapla" Width="120" Margin="0,0,10,0" />
                        <Button Style="{StaticResource SecondaryButton}" Command="{Binding SaveBatchCommand}" Content="Kaydet" Width="120" Margin="0,0,10,0" />
                        <Button Style="{StaticResource DangerButton}" Command="{Binding DeleteBatchCommand}" Content="Sil" Width="120" />
                    </StackPanel>

                    <DataGrid ItemsSource="{Binding Items}" SelectedItem="{Binding SelectedItem}">
                        <DataGrid.Columns>
                            <DataGridTextColumn Header="Ürün" Binding="{Binding ProductName}" Width="*" />
                            <DataGridTextColumn Header="Birim" Binding="{Binding UnitType}" Width="80" />
                            <DataGridTextColumn Header="Gönder" Binding="{Binding QuantitySent, UpdateSourceTrigger=PropertyChanged}" Width="90" />
                            <DataGridTextColumn Header="İade" Binding="{Binding QuantityReturned, UpdateSourceTrigger=PropertyChanged}" Width="80" />
                            <DataGridTextColumn Header="Zayi" Binding="{Binding QuantityWasted, UpdateSourceTrigger=PropertyChanged}" Width="80" />
                            <DataGridTextColumn Header="Fiyat" Binding="{Binding UnitPrice, UpdateSourceTrigger=PropertyChanged}" Width="90" />
                            <DataGridTextColumn Header="İskonto%" Binding="{Binding ItemDiscountPercent, UpdateSourceTrigger=PropertyChanged}" Width="90" />
                        </DataGrid.Columns>
                    </DataGrid>

                    <StackPanel Orientation="Horizontal">
                        <TextBlock Text="Ara Toplam:" Foreground="{StaticResource TextMutedBrush}" VerticalAlignment="Center" />
                        <TextBlock Text="{Binding Subtotal, StringFormat={}{0:n2}}" Margin="8,0,0,0" FontWeight="SemiBold" VerticalAlignment="Center" />
                        <TextBlock Text="  •  Toplam:" Foreground="{StaticResource TextMutedBrush}" Margin="18,0,0,0" VerticalAlignment="Center" />
                        <TextBlock Text="{Binding Total, StringFormat={}{0:n2}}" Margin="8,0,0,0" FontWeight="SemiBold" VerticalAlignment="Center" />
                    </StackPanel>

                    <Button Style="{StaticResource DangerButton}" Command="{Binding RemoveItemCommand}" Content="Seçili Ürünü Kaldır" Width="180" />
                </DockPanel>
            </Border>

            <!-- Batch / Add item panel -->
            <Border Grid.Column="2" Style="{StaticResource CardBorder}">
                <ScrollViewer VerticalScrollBarVisibility="Auto">
                    <StackPanel>
                        <TextBlock Text="Batch Bilgisi" FontSize="16" FontWeight="SemiBold" Margin="0,0,0,10" />

                        <TextBlock Text="Batch No" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding BatchNo, UpdateSourceTrigger=PropertyChanged}" />

                        <TextBlock Text="Tarih" Foreground="{StaticResource TextMutedBrush}" />
                        <DatePicker SelectedDate="{Binding Date}" />

                        <TextBlock Text="Şube" Foreground="{StaticResource TextMutedBrush}" />
                        <ComboBox ItemsSource="{Binding Branches}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding BranchId}" />

                        <TextBlock Text="Batch İskonto (%)" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding BatchDiscountPercent, UpdateSourceTrigger=PropertyChanged}" />

                        <TextBlock Text="Not" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding Notes, UpdateSourceTrigger=PropertyChanged}" AcceptsReturn="True" Height="70" />

                        <Separator Margin="0,10,0,10" />
                        <TextBlock Text="Ürün Ekle" FontSize="14" FontWeight="SemiBold" Margin="0,0,0,6" />

                        <TextBlock Text="Ürün" Foreground="{StaticResource TextMutedBrush}" />
                        <ComboBox ItemsSource="{Binding Products}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding NewProductId}" />

                        <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
                            <Button Style="{StaticResource SecondaryButton}" Command="{Binding FillUnitPriceCommand}" Content="Fiyat Çek" Width="120" Margin="0,0,10,0" />
                            <TextBlock Text="(şube özel fiyat varsa onu alır)" Foreground="{StaticResource TextMutedBrush}" VerticalAlignment="Center" />
                        </StackPanel>

                        <TextBlock Text="Gönderim Miktarı" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding NewQtySent, UpdateSourceTrigger=PropertyChanged}" />

                        <TextBlock Text="Birim Fiyat" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding NewUnitPrice, UpdateSourceTrigger=PropertyChanged}" />

                        <TextBlock Text="Ürün İskonto (%)" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding NewItemDiscountPercent, UpdateSourceTrigger=PropertyChanged}" />

                        <Button Style="{StaticResource PrimaryButton}" Command="{Binding AddItemCommand}" Content="Ekle" />
                    </StackPanel>
                </ScrollViewer>
            </Border>
        </Grid>
    </Grid>
</UserControl>
''')

view('PaymentsView', r'''
<UserControl x:Class="BakeryAutomation.Views.PaymentsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <TextBlock Text="Tahsilat" FontSize="22" FontWeight="SemiBold" Margin="0,0,0,12" />

        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="420" />
            </Grid.ColumnDefinitions>

            <Border Style="{StaticResource CardBorder}" Margin="0,0,12,0">
                <DockPanel>
                    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="0,0,0,8">
                        <Button Style="{StaticResource SecondaryButton}" Command="{Binding RefreshCommand}" Content="Yenile" Width="120" Margin="0,0,10,0" />
                    </StackPanel>

                    <DataGrid ItemsSource="{Binding Payments}" SelectedItem="{Binding SelectedRow}" IsReadOnly="True">
                        <DataGrid.Columns>
                            <DataGridTextColumn Header="Tarih" Binding="{Binding Payment.Date, StringFormat={}{0:yyyy-MM-dd}}" Width="120" />
                            <DataGridTextColumn Header="Şube" Binding="{Binding BranchName}" Width="*" />
                            <DataGridTextColumn Header="Tutar" Binding="{Binding Payment.Amount, StringFormat={}{0:n2}}" Width="120" />
                            <DataGridTextColumn Header="Yöntem" Binding="{Binding Payment.Method}" Width="120" />
                        </DataGrid.Columns>
                    </DataGrid>
                </DockPanel>
            </Border>

            <Border Grid.Column="1" Style="{StaticResource CardBorder}">
                <ScrollViewer VerticalScrollBarVisibility="Auto">
                    <StackPanel>
                        <TextBlock Text="Tahsilat Kaydı" FontSize="16" FontWeight="SemiBold" Margin="0,0,0,10" />

                        <TextBlock Text="Şube" Foreground="{StaticResource TextMutedBrush}" />
                        <ComboBox ItemsSource="{Binding Branches}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding BranchId}" />

                        <TextBlock Text="Tarih" Foreground="{StaticResource TextMutedBrush}" />
                        <DatePicker SelectedDate="{Binding Date}" />

                        <TextBlock Text="Tutar" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding Amount, UpdateSourceTrigger=PropertyChanged}" />

                        <TextBlock Text="Yöntem" Foreground="{StaticResource TextMutedBrush}" />
                        <ComboBox ItemsSource="{Binding PaymentMethods}" SelectedItem="{Binding Method}" />

                        <TextBlock Text="Referans" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding Reference, UpdateSourceTrigger=PropertyChanged}" />

                        <TextBlock Text="Not" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBox Text="{Binding Note, UpdateSourceTrigger=PropertyChanged}" AcceptsReturn="True" Height="70" />

                        <Button Style="{StaticResource SecondaryButton}" Command="{Binding NewCommand}" Content="Yeni" />
                        <Button Style="{StaticResource PrimaryButton}" Command="{Binding SaveCommand}" Content="Kaydet" />
                        <Button Style="{StaticResource DangerButton}" Command="{Binding DeleteCommand}" Content="Sil" />
                    </StackPanel>
                </ScrollViewer>
            </Border>
        </Grid>
    </Grid>
</UserControl>
''')

view('ReportsView', r'''
<UserControl x:Class="BakeryAutomation.Views.ReportsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel>
            <TextBlock Text="Raporlar" FontSize="22" FontWeight="SemiBold" Margin="0,0,0,12" />

            <Border Style="{StaticResource CardBorder}" Margin="0,0,0,12">
                <StackPanel>
                    <TextBlock Text="Filtre" FontSize="16" FontWeight="SemiBold" Margin="0,0,0,10" />

                    <WrapPanel>
                        <StackPanel Width="180" Margin="0,0,12,0">
                            <TextBlock Text="Başlangıç" Foreground="{StaticResource TextMutedBrush}" />
                            <DatePicker SelectedDate="{Binding From}" />
                        </StackPanel>
                        <StackPanel Width="180" Margin="0,0,12,0">
                            <TextBlock Text="Bitiş" Foreground="{StaticResource TextMutedBrush}" />
                            <DatePicker SelectedDate="{Binding To}" />
                        </StackPanel>
                        <StackPanel Width="260" Margin="0,0,12,0">
                            <TextBlock Text="Şube" Foreground="{StaticResource TextMutedBrush}" />
                            <ComboBox ItemsSource="{Binding Branches}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding BranchId}" />
                        </StackPanel>
                        <StackPanel Width="220">
                            <TextBlock Text=" " />
                            <WrapPanel>
                                <Button Style="{StaticResource PrimaryButton}" Command="{Binding RefreshCommand}" Content="Hesapla" Width="120" Margin="0,0,10,0" />
                                <Button Style="{StaticResource SecondaryButton}" Command="{Binding ExportCsvCommand}" Content="CSV" Width="80" />
                            </WrapPanel>
                        </StackPanel>
                    </WrapPanel>
                </StackPanel>
            </Border>

            <UniformGrid Columns="3" Margin="0,0,0,12">
                <Border Style="{StaticResource CardBorder}" Margin="0,0,10,0">
                    <StackPanel>
                        <TextBlock Text="Sevkiyat" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBlock Text="{Binding TotalShipments}" FontSize="24" FontWeight="SemiBold" />
                    </StackPanel>
                </Border>
                <Border Style="{StaticResource CardBorder}" Margin="0,0,10,0">
                    <StackPanel>
                        <TextBlock Text="Net Ciro" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBlock Text="{Binding TotalRevenue, StringFormat={}{0:n2}}" FontSize="24" FontWeight="SemiBold" />
                    </StackPanel>
                </Border>
                <Border Style="{StaticResource CardBorder}">
                    <StackPanel>
                        <TextBlock Text="Tahsilat" Foreground="{StaticResource TextMutedBrush}" />
                        <TextBlock Text="{Binding TotalPayments, StringFormat={}{0:n2}}" FontSize="24" FontWeight="SemiBold" />
                    </StackPanel>
                </Border>
            </UniformGrid>

            <Border Style="{StaticResource CardBorder}">
                <TextBlock Text="Günlük Özet" FontSize="16" FontWeight="SemiBold" Margin="0,0,0,10" />
                <DataGrid ItemsSource="{Binding Rows}" IsReadOnly="True">
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="Tarih" Binding="{Binding Date, StringFormat={}{0:yyyy-MM-dd}}" Width="140" />
                        <DataGridTextColumn Header="Sevkiyat" Binding="{Binding ShipmentCount}" Width="100" />
                        <DataGridTextColumn Header="Ciro" Binding="{Binding Revenue, StringFormat={}{0:n2}}" Width="160" />
                        <DataGridTextColumn Header="Tahsilat" Binding="{Binding Payments, StringFormat={}{0:n2}}" Width="160" />
                    </DataGrid.Columns>
                </DataGrid>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
''')

view('SettingsView', r'''
<UserControl x:Class="BakeryAutomation.Views.SettingsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <StackPanel>
        <TextBlock Text="Ayarlar" FontSize="22" FontWeight="SemiBold" Margin="0,0,0,12" />

        <Border Style="{StaticResource CardBorder}">
            <StackPanel>
                <TextBlock Text="Veri" FontSize="16" FontWeight="SemiBold" Margin="0,0,0,10" />

                <TextBlock Text="Veri klasörü" Foreground="{StaticResource TextMutedBrush}" />
                <TextBox Text="{Binding DataFolder}" IsReadOnly="True" />

                <TextBlock Text="Veri dosyası" Foreground="{StaticResource TextMutedBrush}" />
                <TextBox Text="{Binding DataFile}" IsReadOnly="True" />

                <WrapPanel>
                    <Button Style="{StaticResource SecondaryButton}" Command="{Binding OpenFolderCommand}" Content="Klasörü Aç" Width="140" Margin="0,0,10,0" />
                    <Button Style="{StaticResource SecondaryButton}" Command="{Binding BackupCommand}" Content="Yedek Al" Width="120" Margin="0,0,10,0" />
                    <Button Style="{StaticResource SecondaryButton}" Command="{Binding RestoreCommand}" Content="Geri Yükle" Width="120" Margin="0,0,10,0" />
                    <Button Style="{StaticResource SecondaryButton}" Command="{Binding ReloadCommand}" Content="Veriyi Yenile" Width="140" />
                </WrapPanel>

                <TextBlock Text="Not: Geri yükleme sonrası uygulamayı yeniden açmanız daha temiz olur." Foreground="{StaticResource TextMutedBrush}" Margin="0,10,0,0" />
            </StackPanel>
        </Border>
    </StackPanel>
</UserControl>
''')

# -------------------- README --------------------
w(root/'README.md', r'''
# BakeryAutomation (WPF)

Tek kullanıcının Windows PC'de kullanacağı, JSON dosyaya kayıt yapan basit fırın otomasyonu.

## Özellikler
- Ürünler: ekle/sil/güncelle, fiyat geçmişi
- Şubeler/Cariler: kart, şubeye özel fiyat
- Sevkiyat (Batch): gönderim + iade + zayi + ürün iskonto + batch iskonto
- Tahsilat: şube bazlı ödeme girişi
- Rapor: tarih aralığında günlük özet + CSV export
- Ayarlar: veri dosyası konumu, yedekle/geri yükle

## Kurulum
1. Windows'ta Visual Studio 2022 ile `BakeryAutomationApp.sln` aç.
2. Build Configuration: `Release`
3. Build/Run.

## Veri
Uygulama veriyi şurada tutar:
`%AppData%\BakeryAutomation\data.json`

## Notlar
- NuGet kullanılmadı (internet gerektirmez).
- Para birimi alanları `decimal`.
- İskonto alanları % (0-100).
''')

print('Done writing files.')
