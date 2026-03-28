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

        public BakeryAppContext Ctx { get; }

        public bool IsDashboardSelected { get => _isDashboardSelected; set => Set(ref _isDashboardSelected, value); }
        public bool IsProductsSelected { get => _isProductsSelected; set => Set(ref _isProductsSelected, value); }
        public bool IsBranchesSelected { get => _isBranchesSelected; set => Set(ref _isBranchesSelected, value); }
        public bool IsShipmentsSelected { get => _isShipmentsSelected; set => Set(ref _isShipmentsSelected, value); }
        public bool IsPaymentsSelected { get => _isPaymentsSelected; set => Set(ref _isPaymentsSelected, value); }
        public bool IsReturnsSelected { get => _isReturnsSelected; set => Set(ref _isReturnsSelected, value); }
        public bool IsReportsSelected { get => _isReportsSelected; set => Set(ref _isReportsSelected, value); }
        public bool IsSettingsSelected { get => _isSettingsSelected; set => Set(ref _isSettingsSelected, value); }

        private bool _isDashboardSelected;
        private bool _isProductsSelected;
        private bool _isBranchesSelected;
        private bool _isShipmentsSelected;
        private bool _isPaymentsSelected;
        private bool _isReturnsSelected;
        private bool _isReportsSelected;
        private bool _isSettingsSelected;

        public RelayCommand ShowDashboardCommand { get; }
        public RelayCommand ShowProductsCommand { get; }
        public RelayCommand ShowBranchesCommand { get; }
        public RelayCommand ShowShipmentsCommand { get; }
        public RelayCommand ShowPaymentsCommand { get; }
        public RelayCommand ShowReturnsCommand { get; }
        public RelayCommand ShowReportsCommand { get; }
        public RelayCommand ShowSettingsCommand { get; }

        public MainViewModel()
        {
            Ctx = new BakeryAppContext();

            ShowDashboardCommand = new RelayCommand(_ => Navigate(Section.Dashboard));
            ShowProductsCommand = new RelayCommand(_ => Navigate(Section.Products));
            ShowBranchesCommand = new RelayCommand(_ => Navigate(Section.Branches));
            ShowShipmentsCommand = new RelayCommand(_ => Navigate(Section.Shipments));
            ShowPaymentsCommand = new RelayCommand(_ => Navigate(Section.Payments));
            ShowReturnsCommand = new RelayCommand(_ => Navigate(Section.Returns));
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
            Returns,
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
                Section.Branches => new BranchesViewModel(Ctx, ShowStatementReport, ShowPaymentsForBranch),
                Section.Shipments => new ShipmentsViewModel(Ctx),
                Section.Payments => new PaymentsViewModel(Ctx, showReturns: branchId => ShowReturnsForBranch(branchId, true)),
                Section.Returns => new ReturnsViewModel(Ctx),
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
            IsReturnsSelected = section == Section.Returns;
            IsReportsSelected = section == Section.Reports;
            IsSettingsSelected = section == Section.Settings;
        }

        private void ShowStatementReport(int branchId)
        {
            // Switch to Reports view
            SetSelected(Section.Reports);
            
            var vm = new ReportsViewModel(Ctx);
            vm.IsStatementMode = true;
            vm.BranchId = branchId;
            vm.RefreshCommand.Execute(null);

            Current = vm;
        }

        private void ShowPaymentsForBranch(int branchId)
        {
            SetSelected(Section.Payments);
            Current = new PaymentsViewModel(Ctx, branchId, openedBranchId => ShowReturnsForBranch(openedBranchId, true));
        }

        private void ShowReturnsForBranch(int branchId, bool startInFreeMode = false)
        {
            SetSelected(Section.Returns);
            Current = new ReturnsViewModel(Ctx, branchId, startInFreeMode);
        }
    }
}
