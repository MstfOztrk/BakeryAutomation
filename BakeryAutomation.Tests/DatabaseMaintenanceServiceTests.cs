using System;
using System.IO;
using System.Linq;
using BakeryAutomation.Models;
using BakeryAutomation.Services;

namespace BakeryAutomation.Tests
{
    public sealed class DatabaseMaintenanceServiceTests
    {
        [Fact]
        public void RestoreBackup_RestoresDatabaseAndCreatesRestorePoint()
        {
            var tempDirectory = CreateTempDirectory();

            try
            {
                var dbPath = Path.Combine(tempDirectory, "bakery.db");
                var backupPath = Path.Combine(tempDirectory, "backup.db");
                var maintenance = new DatabaseMaintenanceService();

                using (var db = CreateDatabase(dbPath))
                {
                    db.Branches.Add(new Branch { Name = "Merkez" });
                    db.SaveChanges();
                }

                maintenance.CreateBackup(dbPath, backupPath);

                using (var db = CreateDatabase(dbPath))
                {
                    db.Branches.Add(new Branch { Name = "Yeni Sube" });
                    db.SaveChanges();
                }

                var restorePoint = maintenance.RestoreBackup(backupPath, dbPath);

                Assert.False(string.IsNullOrWhiteSpace(restorePoint));
                Assert.True(File.Exists(restorePoint));

                using (var restoredDb = CreateDatabase(dbPath))
                {
                    Assert.Equal(1, restoredDb.Branches.Count());
                    Assert.Equal("Merkez", restoredDb.Branches.Single().Name);
                }
            }
            finally
            {
                TryDeleteDirectory(tempDirectory);
            }
        }

        private static AppDbContext CreateDatabase(string dbPath)
        {
            var db = new AppDbContext(dbPath);
            new DatabaseInitializationService().Initialize(db);
            return db;
        }

        private static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), $"BakeryAutomationTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            return path;
        }

        private static void TryDeleteDirectory(string path)
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    if (!Directory.Exists(path))
                    {
                        return;
                    }

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Directory.Delete(path, true);
                    return;
                }
                catch
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
        }
    }
}
