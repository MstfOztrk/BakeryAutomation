using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;

namespace BakeryAutomation.Services
{
    public sealed class IdentifierCleanupChange
    {
        public string TableName { get; init; } = "";
        public int EntityId { get; init; }
        public string OldValue { get; init; } = "";
        public string NewValue { get; init; } = "";
    }

    public sealed class IdentifierCleanupResult
    {
        public string? BackupPath { get; init; }
        public IReadOnlyList<IdentifierCleanupChange> ShipmentChanges { get; init; } = Array.Empty<IdentifierCleanupChange>();
        public IReadOnlyList<IdentifierCleanupChange> ReturnReceiptChanges { get; init; } = Array.Empty<IdentifierCleanupChange>();
        public int TotalChanges => ShipmentChanges.Count + ReturnReceiptChanges.Count;
    }

    public sealed class IdentifierCleanupService
    {
        public IdentifierCleanupResult NormalizeAndDeduplicate(string databasePath)
        {
            if (!File.Exists(databasePath))
            {
                return new IdentifierCleanupResult();
            }

            List<IdentifierCleanupChange> shipmentChanges;
            List<IdentifierCleanupChange> returnReceiptChanges;

            using (var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly"))
            {
                connection.Open();

                shipmentChanges = TableExists(connection, "Shipments")
                    ? BuildChanges(
                        LoadRows(connection, "Shipments", "Id", "BatchNo", "Date"),
                        "Shipments",
                        "F")
                    : new List<IdentifierCleanupChange>();

                returnReceiptChanges = TableExists(connection, "ReturnReceipts")
                    ? BuildChanges(
                        LoadRows(connection, "ReturnReceipts", "Id", "ReturnNo", "Date"),
                        "ReturnReceipts",
                        "I")
                    : new List<IdentifierCleanupChange>();
            }

            if (shipmentChanges.Count == 0 && returnReceiptChanges.Count == 0)
            {
                return new IdentifierCleanupResult();
            }

            string? backupPath = null;
            try
            {
                backupPath = new DatabaseMaintenanceService().CreateAutomaticBackup(databasePath);
            }
            catch (Exception exception)
            {
                AppLogService.LogWarning(
                    "Identifier cleanup backup",
                    $"Cleanup oncesi otomatik yedek alinamadi.{Environment.NewLine}{exception}");
            }

            using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                connection.Open();
                using var transaction = connection.BeginTransaction();

                ApplyChanges(connection, transaction, "Shipments", "BatchNo", shipmentChanges);
                ApplyChanges(connection, transaction, "ReturnReceipts", "ReturnNo", returnReceiptChanges);

                transaction.Commit();
            }

            var result = new IdentifierCleanupResult
            {
                BackupPath = backupPath,
                ShipmentChanges = shipmentChanges,
                ReturnReceiptChanges = returnReceiptChanges
            };

            AppLogService.LogWarning("Identifier cleanup", BuildLogMessage(result));
            return result;
        }

        private static List<IdentifierCleanupChange> BuildChanges(
            IReadOnlyList<IdentifierRow> rows,
            string tableName,
            string prefix)
        {
            var usedValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var changes = new List<IdentifierCleanupChange>();

            foreach (var row in rows.OrderBy(x => x.Date).ThenBy(x => x.Id))
            {
                var originalValue = row.Identifier ?? string.Empty;
                var normalizedBaseValue = string.IsNullOrWhiteSpace(originalValue)
                    ? $"{prefix}-{row.Date:yyyyMMdd}-{row.Id:0000}"
                    : originalValue.Trim();

                var uniqueValue = normalizedBaseValue;
                var suffix = 2;
                while (!usedValues.Add(uniqueValue))
                {
                    uniqueValue = $"{normalizedBaseValue}-{suffix++}";
                }

                if (string.Equals(originalValue, uniqueValue, StringComparison.Ordinal))
                {
                    continue;
                }

                changes.Add(new IdentifierCleanupChange
                {
                    TableName = tableName,
                    EntityId = row.Id,
                    OldValue = originalValue,
                    NewValue = uniqueValue
                });
            }

            return changes;
        }

        private static List<IdentifierRow> LoadRows(
            SqliteConnection connection,
            string tableName,
            string idColumn,
            string identifierColumn,
            string dateColumn)
        {
            var rows = new List<IdentifierRow>();

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT {idColumn}, {identifierColumn}, {dateColumn} FROM {tableName};";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(new IdentifierRow
                {
                    Id = reader.GetInt32(0),
                    Identifier = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    Date = ParseDate(reader.GetValue(2))
                });
            }

            return rows;
        }

        private static void ApplyChanges(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string tableName,
            string columnName,
            IReadOnlyList<IdentifierCleanupChange> changes)
        {
            if (changes.Count == 0)
            {
                return;
            }

            foreach (var change in changes)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = $"UPDATE {tableName} SET {columnName} = $value WHERE Id = $id;";
                command.Parameters.AddWithValue("$value", change.NewValue);
                command.Parameters.AddWithValue("$id", change.EntityId);
                command.ExecuteNonQuery();
            }
        }

        private static bool TableExists(SqliteConnection connection, string tableName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name LIMIT 1;";
            command.Parameters.AddWithValue("$name", tableName);
            return command.ExecuteScalar() != null;
        }

        private static DateTime ParseDate(object value)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.Date;
            }

            if (value is string text)
            {
                if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedRoundtrip))
                {
                    return parsedRoundtrip.Date;
                }

                if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    return parsed.Date;
                }
            }

            return DateTime.Today;
        }

        private static string BuildLogMessage(IdentifierCleanupResult result)
        {
            var builder = new StringBuilder()
                .AppendLine("BatchNo/ReturnNo cleanup uygulandi.");

            if (!string.IsNullOrWhiteSpace(result.BackupPath))
            {
                builder.Append("Yedek: ").AppendLine(result.BackupPath);
            }

            builder.Append("Sevkiyat degisiklikleri: ").AppendLine(result.ShipmentChanges.Count.ToString(CultureInfo.InvariantCulture));
            foreach (var change in result.ShipmentChanges)
            {
                builder.Append("Shipments#")
                    .Append(change.EntityId.ToString(CultureInfo.InvariantCulture))
                    .Append(": '")
                    .Append(change.OldValue)
                    .Append("' -> '")
                    .Append(change.NewValue)
                    .AppendLine("'");
            }

            builder.Append("Iade degisiklikleri: ").AppendLine(result.ReturnReceiptChanges.Count.ToString(CultureInfo.InvariantCulture));
            foreach (var change in result.ReturnReceiptChanges)
            {
                builder.Append("ReturnReceipts#")
                    .Append(change.EntityId.ToString(CultureInfo.InvariantCulture))
                    .Append(": '")
                    .Append(change.OldValue)
                    .Append("' -> '")
                    .Append(change.NewValue)
                    .AppendLine("'");
            }

            return builder.ToString();
        }

        private sealed class IdentifierRow
        {
            public int Id { get; init; }
            public string Identifier { get; init; } = "";
            public DateTime Date { get; init; }
        }
    }
}
