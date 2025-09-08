using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp.ViewModels
{
    internal class LogsViewModel
    {
        public ObservableCollection<string> LogLines { get; } = new();

        public LogsViewModel()
        {
            LoadLogs();
        }

        private void LoadLogs()
        {
            string logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            if (!Directory.Exists(logDir)) return;

            foreach (var file in Directory.GetFiles(logDir, "*.log"))
            {
                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    string? line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        LogLines.Add(line);
                    }
                }
            }
        }

        // 画面用：一覧の再構築（空にしたい場合は showAll=false）
        public void ReloadFromDisk(bool showAll = true)
        {
            Application.Current.Dispatcher.Invoke(() => LogLines.Clear());
            if (showAll) LoadLogs();
        }

        // ここから追加：バックアップ＆クリア処理
        public async Task ClearLogsAsync()
        {
            string baseDir = Directory.GetCurrentDirectory();
            string logsDir = Path.Combine(baseDir, "logs");
            if (!Directory.Exists(logsDir))
                Directory.CreateDirectory(logsDir);

            string backupRoot = Path.Combine(baseDir, "backups", "logs");
            Directory.CreateDirectory(backupRoot);

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupDir = Path.Combine(backupRoot, stamp);
            Directory.CreateDirectory(backupDir);

            // *.log を対象（必要なら "*.*" に変更）
            var files = Directory.EnumerateFiles(logsDir, "*.log").ToList();

            await Task.Run(() =>
            {
                foreach (var src in files)
                {
                    string dest = Path.Combine(backupDir, Path.GetFileName(src));

                    try
                    {
                        // まずは移動を試みる（ロックされていないファイルはこれでOK）
                        File.Move(src, dest);
                    }
                    catch (IOException)
                    {
                        // 使用中などで移動できない場合：コピー→元ファイルを空にする
                        try
                        {
                            File.Copy(src, dest, overwrite: true);

                            // 元を空に（トランケート）
                            using var fs = new FileStream(src, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
                            fs.SetLength(0);
                        }
                        catch
                        {
                            // 一部のファイルは触れない場合がある（ログが開放されていない等）
                            // 無視して次へ（必要ならリトライや警告収集を実装）
                        }
                    }
                    catch (Exception)
                    {
                        // 予期せぬ例外もスキップ（必要に応じてロギング）
                    }
                }

                // 念のため、残存*.log を再スキャンし、サイズ0以外は可能なら削除
                foreach (var f in Directory.EnumerateFiles(logsDir, "*.log"))
                {
                    try
                    {
                        var fi = new FileInfo(f);
                        if (fi.Length == 0)
                        {
                            // ロックされていないなら削除
                            File.Delete(f);
                        }
                    }
                    catch
                    {
                        // ロックなどで削除できない場合はそのまま（サイズ0で放置）
                    }
                }
            });
        }


    }
}
