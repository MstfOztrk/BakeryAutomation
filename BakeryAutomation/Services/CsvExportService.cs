using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BakeryAutomation.Services
{
    public sealed class CsvExportService
    {
        public void Export(string filePath, string[] headers, IEnumerable<string[]> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(";", Escape(headers)));

            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(";", Escape(row)));
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private IEnumerable<string> Escape(string[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                var v = values[i] ?? "";
                if (v.Contains(";") || v.Contains('"') || v.Contains("\n") || v.Contains("\r"))
                {
                    v = '"' + v.Replace("\"", "\"\"") + '"';
                }
                yield return v;
            }
        }
    }
}
