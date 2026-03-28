using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BakeryAutomation.Services;

namespace BakeryAutomation
{
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ShowFatalError(
                "Uygulama beklenmeyen bir hatayla karsilasti. Guvenlik icin kapatilacak.",
                e.Exception,
                "UI thread");
            e.Handled = true;
            Shutdown(-1);
        }

        public void SetTheme(string themeName)
        {
            var uri = new Uri($"Resources/Theme.{themeName}.xaml", UriKind.Relative);
            var dict = LoadComponent(uri) as ResourceDictionary;
            if (dict == null) return;

            var existing = Resources.MergedDictionaries.FirstOrDefault(d =>
                d.Source != null &&
                (d.Source.OriginalString.Contains("Theme.Light") || d.Source.OriginalString.Contains("Theme.Dark")));

            if (existing != null)
            {
                Resources.MergedDictionaries.Remove(existing);
            }

            Resources.MergedDictionaries.Add(dict);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var settingsService = new SettingsService();
            LocalizationService.Instance.CurrentCulture = settingsService.Current.Language;
            SetTheme(settingsService.Current.Theme);

            base.OnStartup(e);

            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var dbPath = System.IO.Path.Combine(appData, "BakeryAutomation", "bakery.db");
                new DatabaseMaintenanceService().CreateAutomaticBackup(dbPath);
            }
            catch (Exception ex)
            {
                AppLogService.LogException("Startup automatic backup", ex);
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is not Exception exception)
            {
                return;
            }

            try
            {
                Current?.Dispatcher.Invoke(() =>
                    ShowFatalError(
                        "Uygulama kritik bir hatayla karsilasti.",
                        exception,
                        "AppDomain"));
            }
            catch
            {
                AppLogService.LogException("AppDomain fallback", exception);
            }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            ShowFatalError(
                "Arka planda beklenmeyen bir hata olustu. Uygulama guvenlik icin kapatilacak.",
                e.Exception,
                "TaskScheduler");
            e.SetObserved();
            Shutdown(-1);
        }

        private static void ShowFatalError(string summary, Exception exception, string source)
        {
            var logPath = AppLogService.LogException(source, exception);

            MessageBox.Show(
                $"{summary}\n\nAyrintilar log dosyasina yazildi:\n{logPath}",
                "Uygulama Hatasi",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
