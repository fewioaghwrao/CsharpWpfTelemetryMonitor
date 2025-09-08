// Services/ExportService.cs
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WpfApp.Services
{
    public class ExportService : IExportService
    {
        public async Task ExportTelemetryToCsvAsync(string dbPath, string csvPath, CancellationToken ct = default)
        {
            if (!File.Exists(dbPath))
                throw new FileNotFoundException("DBファイルが見つかりません。", dbPath);

            await using var conn = new SqliteConnection($"Data Source={dbPath}");
            await conn.OpenAsync(ct);

            // 1) ユーザーテーブルを列挙
            var userTables = await GetUserTablesAsync(conn, ct);
            if (userTables.Count == 0)
                throw new InvalidOperationException("ユーザーテーブルが見つかりません。DBが空か、別のDBを読んでいる可能性があります。");

            // 2) 優先候補（Telemetry / Readings / Measures など）を探す
            string? targetTable = userTables
                .FirstOrDefault(n => string.Equals(n, "Telemetry", StringComparison.OrdinalIgnoreCase))
                ?? userTables.FirstOrDefault(n => string.Equals(n, "Readings", StringComparison.OrdinalIgnoreCase))
                ?? userTables.FirstOrDefault(n => string.Equals(n, "Measures", StringComparison.OrdinalIgnoreCase))
                ?? userTables.First(); // 最初のテーブルにフォールバック

            // 3) 列情報取得
            var columns = await GetColumnsAsync(conn, targetTable!, ct);
            if (columns.Count == 0)
                throw new InvalidOperationException($"テーブル '{targetTable}' に列がありません。");

            // 4) 「時刻/値/装置」っぽい列を推測（無ければ全列出力）
            var tsCol = columns.FirstOrDefault(c => MatchAny(c, "timestamp", "time", "createdat", "date"));
            var valCol = columns.FirstOrDefault(c => MatchAny(c, "value", "val", "reading", "measure", "measuredvalue"));
            var devCol = columns.FirstOrDefault(c => MatchAny(c, "deviceid", "device", "dev", "machine", "sensor", "equipment"));

            List<string> exportCols = (tsCol != null && valCol != null && devCol != null)
                ? new List<string> { tsCol, valCol, devCol }
                : columns; // 推測できなければ全列

            // 5) SELECT してCSV（UTF-8 BOMでExcel互換）
            string colList = string.Join(", ", exportCols.Select(c => $"\"{c}\""));
            string sql = $"SELECT {colList} FROM \"{targetTable}\" ORDER BY ROWID;"; // 時系列列が分からないのでROWID順
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            await using var fs = new FileStream(csvPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await using var writer = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            // ヘッダー行
            await writer.WriteLineAsync(string.Join(",", exportCols.Select(Csv)));

            // データ行
            while (await reader.ReadAsync(ct))
            {
                var cells = new List<string>(exportCols.Count);
                for (int i = 0; i < exportCols.Count; i++)
                {
                    if (reader.IsDBNull(i))
                    {
                        cells.Add("");
                        continue;
                    }

                    object val = reader.GetValue(i);

                    // 数値は . 区切り、時刻らしき文字列はそのまま
                    if (val is double d)
                        cells.Add(Csv(d.ToString("G", CultureInfo.InvariantCulture)));
                    else if (val is float f)
                        cells.Add(Csv(((double)f).ToString("G", CultureInfo.InvariantCulture)));
                    else if (val is decimal m)
                        cells.Add(Csv(m.ToString(CultureInfo.InvariantCulture)));
                    else
                        cells.Add(Csv(val?.ToString() ?? ""));
                }
                await writer.WriteLineAsync(string.Join(",", cells));
            }

            // ローカル関数群
            static bool MatchAny(string name, params string[] keys)
                => keys.Any(k => string.Equals(name, k, StringComparison.OrdinalIgnoreCase));

            static string Csv(string? s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                bool needQuote = s.Contains(',') || s.Contains('\n') || s.Contains('\r') || s.Contains('"');
                if (!needQuote) return s;
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            }
        }

        private static async Task<List<string>> GetUserTablesAsync(SqliteConnection conn, CancellationToken ct)
        {
            const string sql = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var list = new List<string>();
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                list.Add(r.GetString(0));
            return list;
        }

        private static async Task<List<string>> GetColumnsAsync(SqliteConnection conn, string table, CancellationToken ct)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info(\"{table}\");";
            var cols = new List<string>();
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                // 列名は2列目（name）
                cols.Add(r.GetString(1));
            }
            return cols;
        }
    }
}

