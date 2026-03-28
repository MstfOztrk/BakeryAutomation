using System.Diagnostics;
using System.IO;
using BakeryAutomation.Services;
using Microsoft.Win32;

namespace BakeryAutomation.ViewModels
{
    public sealed class SettingsViewModel : ObservableObject
    {
        private readonly BakeryAppContext _ctx;
        private readonly DatabaseMaintenanceService _databaseMaintenance = new();

        public string DataFolder => System.IO.Path.GetDirectoryName(_ctx.Db.DbPath) ?? "";
        public string DataFile => _ctx.Db.DbPath;

        public RelayCommand BackupCommand { get; }
        public RelayCommand RestoreCommand { get; }
        public RelayCommand OpenFolderCommand { get; }
        public RelayCommand ReloadCommand { get; }

        public class EnumDisplay<T>
        {
            public T? Value { get; set; }
            public string Name { get; set; } = "";
        }

        public System.Collections.Generic.List<EnumDisplay<string>> Languages { get; } = new()
        {
            new() { Value = "tr", Name = "Turkce" },
            new() { Value = "en", Name = "English" }
        };

        private string _selectedLanguage;
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (!Set(ref _selectedLanguage, value)) return;
                _ctx.Loc.CurrentCulture = value;
                _ctx.Settings.Current.Language = value;
                _ctx.Settings.Save();
            }
        }

        public System.Collections.Generic.List<string> Themes { get; } = new() { "Light", "Dark" };

        private string _selectedTheme = "Light";
        public string SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (!Set(ref _selectedTheme, value)) return;
                ((App)System.Windows.Application.Current).SetTheme(value);
                _ctx.Settings.Current.Theme = value;
                _ctx.Settings.Save();
            }
        }

        public SettingsViewModel(BakeryAppContext ctx)
        {
            _ctx = ctx;
            _selectedLanguage = _ctx.Loc.CurrentCulture;
            _selectedTheme = _ctx.Settings.Current.Theme;

            BackupCommand = new RelayCommand(_ => Backup());
            RestoreCommand = new RelayCommand(_ => Restore());
            OpenFolderCommand = new RelayCommand(_ => OpenFolder());
            ReloadCommand = new RelayCommand(_ => Reload());
        }

        private void Backup()
        {
            var dlg = new SaveFileDialog
            {
                FileName = "bakery_backup.db",
                Filter = "Bakery Backup (*.db)|*.db|All files (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true)
            {
                CancelCommand();
                return;
            }

            try
            {
                _databaseMaintenance.CreateBackup(_ctx.Db.DbPath, dlg.FileName);
            }
            catch (Exception ex)
            {
                var logPath = AppLogService.LogException("Manual backup", ex);
                FailCommand(
                    $"Yedek alinamadi.\n\nDetaylar loga yazildi:\n{logPath}",
                    "Yedek Hatasi",
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void Restore()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Bakery Backup (*.db)|*.db|All files (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true)
            {
                CancelCommand();
                return;
            }

            try
            {
                _ctx.Db.Dispose();
                var restorePoint = _databaseMaintenance.RestoreBackup(dlg.FileName, _ctx.Db.DbPath);

                var message = "Veri geri yuklendi. Uygulama yeniden baslatilacak.";
                if (!string.IsNullOrWhiteSpace(restorePoint))
                {
                    message += $"\n\nEski verinin emniyet kopyasi:\n{restorePoint}";
                }

                System.Windows.MessageBox.Show(
                    message,
                    "Geri Yukleme Tamam",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                _ctx.Reload();
                Raise(nameof(DataFolder));
                Raise(nameof(DataFile));

                var logPath = AppLogService.LogException("Database restore", ex);
                FailCommand(
                    $"Geri yukleme basarisiz oldu.\n\nDetaylar loga yazildi:\n{logPath}",
                    "Geri Yukleme Hatasi",
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void OpenFolder()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = System.IO.Path.GetDirectoryName(_ctx.Db.DbPath),
                UseShellExecute = true
            });
        }

        private void Reload()
        {
            _ctx.Reload();
            Raise(nameof(DataFolder));
            Raise(nameof(DataFile));
        }
    }
}
