using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BakeryAutomation.Services
{
    public sealed class DatabaseInitializationService
    {
        private static readonly string[] BaselineTables =
        {
            "Products",
            "Branches",
            "Shipments",
            "ShipmentItems",
            "PriceChanges",
            "Payments",
            "DirectSales",
            "DirectSaleItems",
            "BranchPriceOverrides",
            "ReturnReceipts",
            "ReturnReceiptItems"
        };

        public void Initialize(AppDbContext db)
        {
            var databaseExists = File.Exists(db.DbPath);
            if (databaseExists)
            {
                new SchemaUpgradeService(db).ApplyLegacyUpgrades();
                new IdentifierCleanupService().NormalizeAndDeduplicate(db.DbPath);
                BaselineExistingDatabaseIfNeeded(db);
            }

            db.Database.Migrate();
        }

        private static void BaselineExistingDatabaseIfNeeded(AppDbContext db)
        {
            var availableMigrations = db.Database.GetMigrations().ToList();
            if (availableMigrations.Count == 0)
            {
                return;
            }

            using var connection = new SqliteConnection($"Data Source={db.DbPath}");
            connection.Open();

            if (!HasAllBaselineTables(connection))
            {
                return;
            }

            var historyTableExists = TableExists(connection, "__EFMigrationsHistory");
            if (historyTableExists && HistoryHasRows(connection))
            {
                return;
            }

            using var transaction = connection.BeginTransaction();

            using (var createCommand = connection.CreateCommand())
            {
                createCommand.Transaction = transaction;
                createCommand.CommandText =
                    """
                    CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                        "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                        "ProductVersion" TEXT NOT NULL
                    );
                    """;
                createCommand.ExecuteNonQuery();
            }

            using (var insertCommand = connection.CreateCommand())
            {
                insertCommand.Transaction = transaction;
                insertCommand.CommandText =
                    """
                    INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                    VALUES ($migrationId, $productVersion);
                    """;

                insertCommand.Parameters.AddWithValue("$migrationId", availableMigrations[0]);
                insertCommand.Parameters.AddWithValue(
                    "$productVersion",
                    typeof(DbContext).Assembly.GetName().Version?.ToString() ?? "8.0.2");

                insertCommand.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        private static bool HasAllBaselineTables(SqliteConnection connection)
        {
            foreach (var table in BaselineTables)
            {
                if (!TableExists(connection, table))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HistoryHasRows(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsHistory\";";
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        private static bool TableExists(SqliteConnection connection, string tableName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name LIMIT 1;";
            command.Parameters.AddWithValue("$name", tableName);
            return command.ExecuteScalar() != null;
        }
    }
}
