using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BakeryAutomation.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BakeryAutomation.Tests
{
    public sealed class IdentifierCleanupServiceTests
    {
        private const string PreUniqueMigration = "20260323080639_AddOperationalIndexes";

        [Fact]
        public void NormalizeAndDeduplicate_RepairsDuplicateAndBlankIdentifiers()
        {
            var tempDirectory = CreateTempDirectory();

            try
            {
                var dbPath = Path.Combine(tempDirectory, "bakery.db");
                CreateDatabaseAtMigration(dbPath, PreUniqueMigration);
                InsertLegacyRows(dbPath);

                var result = new IdentifierCleanupService().NormalizeAndDeduplicate(dbPath);

                Assert.Equal(4, result.TotalChanges);

                using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
                connection.Open();

                var shipmentValues = LoadIdentifierValues(connection, "Shipments", "BatchNo");
                var returnValues = LoadIdentifierValues(connection, "ReturnReceipts", "ReturnNo");

                Assert.Equal(new[] { "F-DUP", "F-DUP-2", "F-20260321-0003" }, shipmentValues);
                Assert.Equal(new[] { "I-DUP", "I-DUP-2", "I-20260321-0003" }, returnValues);
            }
            finally
            {
                TryDeleteDirectory(tempDirectory);
            }
        }

        [Fact]
        public void Initialize_CleansDuplicateIdentifiers_AndEnforcesUniqueIndexes()
        {
            var tempDirectory = CreateTempDirectory();

            try
            {
                var dbPath = Path.Combine(tempDirectory, "bakery.db");
                CreateDatabaseAtMigration(dbPath, PreUniqueMigration);
                InsertLegacyRows(dbPath);

                using (var db = new AppDbContext(dbPath))
                {
                    new DatabaseInitializationService().Initialize(db);
                }

                using var connection = new SqliteConnection($"Data Source={dbPath}");
                connection.Open();

                var shipmentValues = LoadIdentifierValues(connection, "Shipments", "BatchNo");
                var returnValues = LoadIdentifierValues(connection, "ReturnReceipts", "ReturnNo");

                Assert.Equal(3, shipmentValues.Distinct(StringComparer.OrdinalIgnoreCase).Count());
                Assert.Equal(3, returnValues.Distinct(StringComparer.OrdinalIgnoreCase).Count());

                Assert.Throws<SqliteException>(() =>
                    InsertShipment(connection, "F-DUP", new DateTime(2026, 3, 22)));

                Assert.Throws<SqliteException>(() =>
                    InsertReturnReceipt(connection, "I-DUP", new DateTime(2026, 3, 22)));
            }
            finally
            {
                TryDeleteDirectory(tempDirectory);
            }
        }

        private static void CreateDatabaseAtMigration(string dbPath, string migrationId)
        {
            using var db = new AppDbContext(dbPath);
            var migrator = db.Database.GetService<IMigrator>();
            migrator.Migrate(migrationId);
        }

        private static void InsertLegacyRows(string dbPath)
        {
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            InsertShipment(connection, "F-DUP", new DateTime(2026, 3, 20));
            InsertShipment(connection, " F-DUP ", new DateTime(2026, 3, 20));
            InsertShipment(connection, "   ", new DateTime(2026, 3, 21));

            InsertReturnReceipt(connection, "I-DUP", new DateTime(2026, 3, 20));
            InsertReturnReceipt(connection, " I-DUP ", new DateTime(2026, 3, 20));
            InsertReturnReceipt(connection, "", new DateTime(2026, 3, 21));
        }

        private static void InsertShipment(SqliteConnection connection, string batchNo, DateTime date)
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO Shipments (BatchNo, Date, BranchId, Notes, BatchDiscountPercent, CreatedAt, UpdatedAt)
                VALUES ($batchNo, $date, 1, '', 0, $createdAt, $updatedAt);
                """;
            command.Parameters.AddWithValue("$batchNo", batchNo);
            command.Parameters.AddWithValue("$date", date);
            command.Parameters.AddWithValue("$createdAt", date);
            command.Parameters.AddWithValue("$updatedAt", date);
            command.ExecuteNonQuery();
        }

        private static void InsertReturnReceipt(SqliteConnection connection, string returnNo, DateTime date)
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO ReturnReceipts (ReturnNo, Date, BranchId, Notes, CreatedAt, UpdatedAt)
                VALUES ($returnNo, $date, 1, '', $createdAt, $updatedAt);
                """;
            command.Parameters.AddWithValue("$returnNo", returnNo);
            command.Parameters.AddWithValue("$date", date);
            command.Parameters.AddWithValue("$createdAt", date);
            command.Parameters.AddWithValue("$updatedAt", date);
            command.ExecuteNonQuery();
        }

        private static List<string> LoadIdentifierValues(SqliteConnection connection, string tableName, string columnName)
        {
            var values = new List<string>();

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT {columnName} FROM {tableName} ORDER BY Id;";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                values.Add(reader.GetString(0));
            }

            return values;
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
