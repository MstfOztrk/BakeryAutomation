using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace BakeryAutomation.Services
{
    public sealed class DatabaseMaintenanceService
    {
        private static readonly string[] RequiredTables =
        {
            "Products",
            "Branches",
            "Shipments",
            "ShipmentItems",
            "Payments",
            "BranchPriceOverrides",
            "ReturnReceipts",
            "ReturnReceiptItems"
        };

        public string? CreateAutomaticBackup(string databasePath, int maxBackups = 14)
        {
            if (!File.Exists(databasePath))
            {
                return null;
            }

            var backupDirectory = Path.Combine(Path.GetDirectoryName(databasePath)!, "Backups");
            Directory.CreateDirectory(backupDirectory);

            var backupPath = Path.Combine(
                backupDirectory,
                $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");

            CreateBackup(databasePath, backupPath);
            PruneBackups(backupDirectory, maxBackups);

            return backupPath;
        }

        public void CreateBackup(string sourcePath, string destinationPath)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Yedek alinacak veritabani bulunamadi.", sourcePath);
            }

            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (string.IsNullOrWhiteSpace(destinationDirectory))
            {
                throw new InvalidOperationException("Yedek klasoru belirlenemedi.");
            }

            Directory.CreateDirectory(destinationDirectory);

            var tempPath = Path.Combine(
                destinationDirectory,
                $"{Path.GetFileNameWithoutExtension(destinationPath)}_{Guid.NewGuid():N}.tmp");

            try
            {
                using (var sourceConnection = new SqliteConnection($"Data Source={sourcePath};Mode=ReadOnly"))
                using (var destinationConnection = new SqliteConnection($"Data Source={tempPath}"))
                {
                    sourceConnection.Open();
                    destinationConnection.Open();
                    sourceConnection.BackupDatabase(destinationConnection);
                }

                ValidateDatabaseFile(tempPath);
                File.Copy(tempPath, destinationPath, true);
            }
            finally
            {
                TryDelete(tempPath);
            }
        }

        public string? RestoreBackup(string sourcePath, string destinationPath)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Geri yuklenecek yedek dosyasi bulunamadi.", sourcePath);
            }

            ValidateDatabaseFile(sourcePath);

            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (string.IsNullOrWhiteSpace(destinationDirectory))
            {
                throw new InvalidOperationException("Veritabani klasoru belirlenemedi.");
            }

            Directory.CreateDirectory(destinationDirectory);
            string? restorePoint = null;

            if (File.Exists(destinationPath))
            {
                var restorePointDirectory = Path.Combine(destinationDirectory, "Backups", "RestorePoints");
                Directory.CreateDirectory(restorePointDirectory);

                restorePoint = Path.Combine(
                    restorePointDirectory,
                    $"before_restore_{DateTime.Now:yyyyMMdd_HHmmss}.db");

                CreateBackup(destinationPath, restorePoint);
            }

            SqliteConnection.ClearAllPools();
            DeleteSidecarFiles(destinationPath);

            using (var sourceConnection = new SqliteConnection($"Data Source={sourcePath};Mode=ReadOnly"))
            using (var destinationConnection = new SqliteConnection($"Data Source={destinationPath}"))
            {
                sourceConnection.Open();
                destinationConnection.Open();
                sourceConnection.BackupDatabase(destinationConnection);
            }

            SqliteConnection.ClearAllPools();
            DeleteSidecarFiles(destinationPath);

            return restorePoint;
        }

        public void ValidateDatabaseFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Veritabani dosyasi bulunamadi.", filePath);
            }

            using var connection = new SqliteConnection($"Data Source={filePath};Mode=ReadOnly");
            connection.Open();

            var existingTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table';";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    existingTables.Add(reader.GetString(0));
                }
            }

            var missingTables = RequiredTables.Where(table => !existingTables.Contains(table)).ToList();
            if (missingTables.Count > 0)
            {
                throw new InvalidDataException(
                    $"Secilen veritabani eksik tablolar iceriyor: {string.Join(", ", missingTables)}");
            }
        }

        private static void PruneBackups(string backupDirectory, int maxBackups)
        {
            var files = new DirectoryInfo(backupDirectory)
                .GetFiles("backup_*.db")
                .OrderByDescending(x => x.CreationTimeUtc)
                .ToList();

            for (var i = maxBackups; i < files.Count; i++)
            {
                TryDelete(files[i].FullName);
            }
        }

        private static void TryDelete(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // Best effort cleanup only.
            }
        }

        private static void DeleteSidecarFiles(string databasePath)
        {
            TryDelete($"{databasePath}-wal");
            TryDelete($"{databasePath}-shm");
        }
    }
}
