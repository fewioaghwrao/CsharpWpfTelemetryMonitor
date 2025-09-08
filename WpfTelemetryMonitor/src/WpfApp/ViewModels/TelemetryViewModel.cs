using System.Collections.ObjectModel;
using Microsoft.Data.Sqlite;
using System.IO;
using System;
using System.Collections.Generic;
using WpfApp.Models;
using System.Windows;
using System.Threading.Tasks;
using System.DirectoryServices;

namespace WpfApp.ViewModels
{
    public class TelemetryViewModel
    {
        public ObservableCollection<SensorReading> Readings { get; } = new();

        public TelemetryViewModel()
        {
            LoadReadings();
        }

        private void LoadReadings()
        {
            string dbPath = Path.GetFullPath("telemetry.db");
            if (!File.Exists(dbPath)) return;

            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT ts, device_id, value FROM readings ORDER BY id DESC";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Readings.Add(new SensorReading
                {
                    Timestamp = System.DateTime.Parse(reader.GetString(0)),
                    DeviceId = reader.GetString(1),
                    Value = reader.GetDouble(2)
                });
            }
        }

        // 画面用：一覧の再構築（空にしたい場合は showAll=false）
        public void ReloadFromDatabese(bool showAll = true)
        {
            Application.Current.Dispatcher.Invoke(() => Readings.Clear());
            if (showAll) LoadReadings();
        }

        // ここから追加：バックアップ＆クリア処理
        public async Task ClearDatabaseAsync()
        {
            string baseDir = Directory.GetCurrentDirectory();
            string dbPath = Path.GetFullPath("telemetry.db");
            if (!File.Exists(dbPath))
                throw new FileNotFoundException("DBファイルが見つかりません。", dbPath);

            // 1) バックアップ先作成
            string backupRoot = Path.Combine(baseDir, "backups", "db");
            Directory.CreateDirectory(backupRoot);
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupDir = Path.Combine(backupRoot, stamp);
            Directory.CreateDirectory(backupDir);
            string backupPath = Path.Combine(backupDir, Path.GetFileName(dbPath));

            // 2) 一貫性のあるバックアップを取得（推奨：VACUUM INTO）
            //    ※ DBがWALでもTRUNCATEチェックポイント → VACUUM INTO で安全
            await using (var src = new SqliteConnection($"Data Source={dbPath}"))
            {
                await src.OpenAsync();
                await ExecAsync(src, "PRAGMA wal_checkpoint(TRUNCATE);"); // WAL→本体へ反映
                await ExecAsync(src, $"VACUUM INTO '{backupPath.Replace("'", "''")}';"); // 安全コピー
            }

            // 3) テーブル内データを全削除 → 採番リセット → VACUUM
            await using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                await conn.OpenAsync();

                // ユーザーテーブル一覧
                var tables = await GetUserTablesAsync(conn);
                if (tables.Count == 0) return;

                await using (var tx = await conn.BeginTransactionAsync())
                {
                    await ExecAsync(conn, "PRAGMA foreign_keys=OFF;", (SqliteTransaction?)tx);

                    foreach (var t in tables)
                    {
                        await ExecAsync(conn, $"DELETE FROM \"{t}\";", (SqliteTransaction?)tx);
                    }

                    // AUTOINCREMENTを初期化（無ければ無視）
                    await ExecIgnoreAsync(conn, "DELETE FROM sqlite_sequence;", (SqliteTransaction?)tx);

                    await tx.CommitAsync();
                }

                // サイズ縮小
                await ExecAsync(conn, "PRAGMA wal_checkpoint(TRUNCATE);");
                await ExecAsync(conn, "VACUUM;");
            }
        }
        // --- helpers ---
        static async Task<List<string>> GetUserTablesAsync(SqliteConnection conn)
        {
            const string sql =
                "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
            var list = new List<string>();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(r.GetString(0));
            return list;
        }

        static async Task ExecAsync(SqliteConnection conn, string sql, SqliteTransaction? tx = null)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            if (tx != null) cmd.Transaction = tx;
            await cmd.ExecuteNonQueryAsync();
        }

        static async Task ExecIgnoreAsync(SqliteConnection conn, string sql, SqliteTransaction? tx = null)
        {
            try { await ExecAsync(conn, sql, tx); } catch { /* 無ければスルー */ }
        }
    }
}