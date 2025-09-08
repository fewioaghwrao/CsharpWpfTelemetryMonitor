using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.IO;

// ★ 追加：OxyPlot
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using WpfApp.Models;
using WpfApp.Services;

namespace WpfApp.ViewModels
{
    public partial class MainWindowViewModel : ObservableRecipient, IRecipient<AlertThresholdChangedMessage>
    {
        private readonly IOptionsMonitor<AppSettings> _optMonitor;

        private readonly TelemetrySimulator _sim;
        private readonly Repository _repo;
        private readonly AlertService _alerts;

        private readonly IDialogService _dialog;
        private readonly IExportService _export;
        private readonly IOptionsMonitor<AppSettings> _opt;

        public ObservableCollection<SensorReading> Readings { get; } = new();

        [ObservableProperty]
        private string status = "開始可能";

        [ObservableProperty]
        private double alertThreshold;

        [ObservableProperty]
        private string deviceId = "dev-XXX";

        [ObservableProperty] private bool isBusy;
        public string DatabasePath => _opt.CurrentValue.DatabasePath;


        // ★ 追加：OxyPlot 用
        public PlotModel PlotModel { get; }
        // 実測値（青）
        private readonly LineSeries _seriesBlue;
        // 閾値（赤・段差）
        private readonly StairStepSeries _seriesThreshold;
        private readonly DateTimeAxis _xAxis;
        private readonly LinearAxis _yAxis;

        public MainWindowViewModel(TelemetrySimulator sim, Repository repo, AlertService alerts, 
            IOptionsMonitor<AppSettings> optMonitor, IMessenger messenger, IDialogService dialog,
            IExportService export, IOptionsMonitor<AppSettings> opt)
        {
            _dialog = dialog;
            _export = export;
            _opt = opt;
            _sim = sim;
            _repo = repo;
            _alerts = alerts;
            _optMonitor = optMonitor;
            alertThreshold = _optMonitor.CurrentValue.AlertThreshold;

            IsActive = true;       // これで自動 Register/Unregister

            _sim.OnReading += OnReading;
            _alerts.OnAlert += m => Status = $"警告: {m}";

            // ★ グラフ初期化
            PlotModel = new PlotModel
            {
                Title = "最近の100秒間",
                PlotMargins = new OxyThickness(40, 10, 20, 30)
            };

            _xAxis = new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                Title = "時間",
                StringFormat = "HH:mm:ss",
                IntervalType = DateTimeIntervalType.Seconds,
                MinorIntervalType = DateTimeIntervalType.Seconds,
                IsPanEnabled = false,
                IsZoomEnabled = false,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            };
            _yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "計測値",
                IsPanEnabled = false,
                IsZoomEnabled = false,
                MajorGridlineStyle = LineStyle.Solid
            };

            PlotModel.Axes.Add(_xAxis);
            PlotModel.Axes.Add(_yAxis);

            _seriesBlue = new LineSeries
            {
                Color = OxyColors.Blue,
                StrokeThickness = 2,
                LineStyle = LineStyle.Solid
            };

            _seriesThreshold = new StairStepSeries
            {
                Color = OxyColors.Red,
                StrokeThickness = 2,
                LineStyle = LineStyle.Dash,   // 破線だと見やすい
                MarkerType = MarkerType.None
            };
            PlotModel.Series.Add(_seriesBlue);
            PlotModel.Series.Add(_seriesThreshold);

            // 初期の表示範囲（現在-100秒〜現在）
            var now = DateTime.Now;
            _xAxis.Minimum = DateTimeAxis.ToDouble(now.AddSeconds(-100));
            _xAxis.Maximum = DateTimeAxis.ToDouble(now);

            // 設定に持っているなら反映
            if (!string.IsNullOrWhiteSpace(optMonitor.CurrentValue.DeviceId))
                deviceId = optMonitor.CurrentValue.DeviceId;

            // 設定変更時にも追従させたい場合
            optMonitor.OnChange(s =>
            {
                if (!string.IsNullOrWhiteSpace(s.DeviceId))
                    DeviceId = s.DeviceId;
            });
        }

        private async void OnReading(SensorReading r)
        {
            // UIスレッドでリスト＆グラフ更新
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                // ListView 用
                Readings.Add(r);
                if (Readings.Count > 2000) Readings.RemoveAt(0);

                // グラフ用：点追加
                double x = DateTimeAxis.ToDouble(r.Timestamp);
                _seriesBlue.Points.Add(new DataPoint(x, r.Value));
                // 閾値（黄）：その時点の閾値を追加（水平が維持される）
                _seriesThreshold.Points.Add(new DataPoint(x, AlertThreshold));

                // 100秒より古い点を掃除（両シリーズ）
                double tail = DateTimeAxis.ToDouble(DateTime.Now.AddSeconds(-100));
                while (_seriesBlue.Points.Count > 0 && _seriesBlue.Points[0].X < tail)
                    _seriesBlue.Points.RemoveAt(0);
                while (_seriesThreshold.Points.Count > 0 && _seriesThreshold.Points[0].X < tail)
                    _seriesThreshold.Points.RemoveAt(0);

                // X軸の表示範囲更新 & 再描画
                var now = DateTime.Now;
                _xAxis.Minimum = DateTimeAxis.ToDouble(now.AddSeconds(-100));
                _xAxis.Maximum = DateTimeAxis.ToDouble(now);
                PlotModel.InvalidatePlot(true);
            });

            // 永続化（非UI）
            try { await _repo.InsertAsync(r); }
            catch (Exception ex) { Log.Error(ex, "Insert failed"); }

            // アラート
            _alerts.Check(r);
        }

        [RelayCommand]
        private void Start()
        {
            _sim.Start();
            Status = "サンプリング中";
        }

        [RelayCommand]
        private void Stop()
        {
            _sim.Stop();
            Status = "停止中";
        }
        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task ExportCsvAsync()
        {
            try
            {
                isBusy = true;

                var dbPath = DatabasePath;
                if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
                {
                    _dialog.ShowError($"DBファイルが見つかりません。\n{dbPath}");
                    return;
                }

                var picker = new SaveFilePickerOptions(
                    Title: "CSV にエクスポート",
                    DefaultFileName: $"telemetry_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                    DefaultExt: "csv",
                    Filter: "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
                );

                var csvPath = await _dialog.ShowSaveFilePickerAsync(picker);
                if (string.IsNullOrEmpty(csvPath)) return; // キャンセル

                await _export.ExportTelemetryToCsvAsync(dbPath, csvPath);

                _dialog.ShowInfo("CSV エクスポートが完了しました。");
            }
            catch (Exception ex)
            {
                _dialog.ShowError($"エクスポート中にエラーが発生しました。\n{ex.Message}");
            }
            finally
            {
                isBusy = false;
            }
        }

        // メッセージ受信時に即時反映
        public void Receive(AlertThresholdChangedMessage message)
        {
            AlertThreshold = message.Value;
            // 2) 閾値系列に「今の時刻の点」を追加 → ここから新しい高さの水平線になる
            var t = DateTimeAxis.ToDouble(DateTime.Now);
            _seriesThreshold.Points.Add(new DataPoint(t, AlertThreshold));

            // 古い点の掃除（100秒窓）
            double tail = DateTimeAxis.ToDouble(DateTime.Now.AddSeconds(-100));
            while (_seriesThreshold.Points.Count > 0 && _seriesThreshold.Points[0].X < tail)
                _seriesThreshold.Points.RemoveAt(0);

            PlotModel.InvalidatePlot(true);

        }
    }
}
