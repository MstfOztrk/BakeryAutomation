namespace BakeryAutomation.Services
{
    public sealed class BakeryAppContext
    {
        public CalculationService Calc { get; }
        public CsvExportService Csv { get; }
        public PrintService Print { get; }
        public ShipmentIntegrityService ShipmentIntegrity { get; }
        public BranchPolicyService BranchPolicy { get; }
        public AppDbContext Db { get; private set; }
        public SettingsService Settings { get; }
        
        public LocalizationService Loc { get; }

        public BakeryAppContext(AppDbContext? db = null)
        {
            Loc = LocalizationService.Instance;
            Settings = new SettingsService();
            Calc = new CalculationService();
            Csv = new CsvExportService();
            Print = new PrintService(Calc);
            ShipmentIntegrity = new ShipmentIntegrityService(Calc);
            BranchPolicy = new BranchPolicyService();
            Db = db ?? CreateDatabase();
        }

        public void Save() => Db.SaveChanges();

        public void Reload()
        {
            Db.Dispose();
            Db = CreateDatabase();
        }

        private static AppDbContext CreateDatabase()
        {
            var db = new AppDbContext();
            new DatabaseInitializationService().Initialize(db);
            return db;
        }
    }
}
