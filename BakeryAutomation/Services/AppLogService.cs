using System;
using System.IO;
using System.Text;

namespace BakeryAutomation.Services
{
    public static class AppLogService
    {
        private static readonly object SyncRoot = new();

        public static string LogException(string source, Exception exception)
        {
            return WriteEntry("ERROR", source, exception.ToString());
        }

        public static string LogWarning(string source, string message)
        {
            return WriteEntry("WARN", source, message);
        }

        public static string LogInfo(string source, string message)
        {
            return WriteEntry("INFO", source, message);
        }

        private static string WriteEntry(string level, string source, string message)
        {
            var logDirectory = GetLogDirectory();
            var logPath = Path.Combine(logDirectory, $"app_{DateTime.Now:yyyyMMdd}.log");
            var entry = new StringBuilder()
                .Append('[').Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("] ")
                .Append(level).Append(' ').Append(source).AppendLine()
                .AppendLine(message)
                .AppendLine()
                .ToString();

            lock (SyncRoot)
            {
                Directory.CreateDirectory(logDirectory);
                File.AppendAllText(logPath, entry, Encoding.UTF8);
            }

            return logPath;
        }

        private static string GetLogDirectory()
        {
            var appFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BakeryAutomation",
                "Logs");

            Directory.CreateDirectory(appFolder);
            return appFolder;
        }
    }
}
