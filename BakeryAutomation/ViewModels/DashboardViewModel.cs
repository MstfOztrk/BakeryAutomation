using System;
using System.Collections.ObjectModel;
using System.Linq;
using BakeryAutomation.Services;
using Microsoft.EntityFrameworkCore;

namespace BakeryAutomation.ViewModels
{
    public sealed class BranchDebtRow
    {
        public string BranchName { get; set; } = "";
        public decimal Balance { get; set; }
    }

    public sealed class DashboardViewModel : ObservableObject
    {
        private readonly BakeryAppContext _ctx;

        public DateTime Today => DateTime.Today;

        private int _todayShipmentCount;
        public int TodayShipmentCount { get => _todayShipmentCount; set => Set(ref _todayShipmentCount, value); }

        private decimal _todayNetRevenue;
        public decimal TodayNetRevenue { get => _todayNetRevenue; set => Set(ref _todayNetRevenue, value); }

        private decimal _totalReceivable;
        public decimal TotalReceivable { get => _totalReceivable; set => Set(ref _totalReceivable, value); }

        private int _activeBranches;
        public int ActiveBranches { get => _activeBranches; set => Set(ref _activeBranches, value); }

        private int _activeProducts;
        public int ActiveProducts { get => _activeProducts; set => Set(ref _activeProducts, value); }

        public ObservableCollection<BranchDebtRow> TopDebtors { get; } = new();

        public RelayCommand RefreshCommand { get; }

        public DashboardViewModel(BakeryAppContext ctx)
        {
            _ctx = ctx;
            RefreshCommand = new RelayCommand(_ => Refresh());
            Refresh();
        }

        private void Refresh()
        {
            var todayShipments = _ctx.Db.Shipments
                .AsNoTracking()
                .Include(s => s.Items)
                .Where(s => s.Date == DateTime.Today)
                .ToList();

            var todayReturns = _ctx.Db.ReturnReceipts
                .AsNoTracking()
                .Include(r => r.Items)
                .Where(r => r.Date == DateTime.Today)
                .ToList();

            TodayShipmentCount = todayShipments.Count;

            decimal shipmentRevenue = 0m;
            for (int i = 0; i < todayShipments.Count; i++) shipmentRevenue += _ctx.Calc.ShipmentTotal(todayShipments[i]);

            decimal returnAmount = 0m;
            for (int i = 0; i < todayReturns.Count; i++) returnAmount += _ctx.Calc.ReturnTotal(todayReturns[i]);

            TodayNetRevenue = shipmentRevenue - returnAmount;

            ActiveBranches = _ctx.Db.Branches.AsNoTracking().Count(b => b.IsActive);
            ActiveProducts = _ctx.Db.Products.AsNoTracking().Count(p => p.IsActive);

            var branches = _ctx.Db.Branches
                .AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToList();

            var balanceLookup = _ctx.Calc.BuildBranchBalanceLookup(_ctx.Db, branches.Select(x => x.Id));
            TotalReceivable = balanceLookup.Values.Sum();

            TopDebtors.Clear();
            var rows = branches
                .Select(b => new BranchDebtRow
                {
                    BranchName = b.Name,
                    Balance = balanceLookup.GetValueOrDefault(b.Id)
                })
                .OrderByDescending(x => x.Balance)
                .Take(8)
                .ToList();

            for (int i = 0; i < rows.Count; i++) TopDebtors.Add(rows[i]);
        }
    }
}
