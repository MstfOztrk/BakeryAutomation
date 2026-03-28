using System;
using System.IO;
using System.Text.Json;

namespace BakeryAutomation.Services
{
    public class AppSettings
    {
        public string Theme { get; set; } = "Light";
        public string Language { get; set; } = "tr";
    }

    public class SettingsService
    {
        private readonly string _filePath;
        public AppSettings Current { get; private set; } = new AppSettings();

        public SettingsService()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BakeryAutomation");
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "settings.json");

            Load();
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    Current = new AppSettings();
                }
            }
            catch (Exception ex)
            {
                AppLogService.LogException("Settings load", ex);
                Current = new AppSettings();
            }
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                AppLogService.LogException("Settings save", ex);
            }
        }
    }
}
