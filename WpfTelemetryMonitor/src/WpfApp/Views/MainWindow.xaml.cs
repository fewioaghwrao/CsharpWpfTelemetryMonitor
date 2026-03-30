using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Linq;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Input;
using WpfApp.ViewModels;
using WpfTelemetryMonitor;
using CommunityToolkit.Mvvm.Messaging;


namespace WpfApp.Views
{
    public partial class MainWindow : Window
    {
        private bool _confirmedExit = false; // Alt+F4等の全経路ガード用
        private SpeechSynthesizer _synth;
        private bool _canSpeak = false;  // ← 追加：音声の可否
        private bool _duringSpeak = false; // ← 追加：読み上げ中フラグ
        private int _pendingPrompts = 0;
        private readonly IMessenger _messenger;
        public MainWindow(MainWindowViewModel vm, IMessenger messenger)
        {

            InitializeComponent();
            _messenger = messenger;
            // 音声合成エンジンの初期化
            _synth = new SpeechSynthesizer
            {
                Volume = 100, // 0～100
                Rate = -2     // -10～10
            };
            _synth.SetOutputToDefaultAudioDevice();

            // 「とにかく何かで話す」ための準備
            _canSpeak = EnsureAnyVoiceSelected(_synth);

            // 設定変更（保存時）の通知を受けて Rate/Volume だけ適用
            _messenger.Register<VoiceSettingsChangedMessage>(this, (_, msg) =>
            {
                // 念のためクランプ
                int rate = Math.Max(-10, Math.Min(10, msg.Value.Rate));
                int volume = Math.Max(0, Math.Min(100, msg.Value.Volume));

                // UIスレッドで適用
                Dispatcher.Invoke(() =>
                {
                    // すぐ反映させたいなら、再生中を止める:
                    // _synth.SpeakAsyncCancelAll();

                    _synth.Rate = rate;
                    _synth.Volume = volume;
                });
            });

            DataContext = vm;
            CommandBindings.Add(new CommandBinding(
             ApplicationCommands.Close,
             CloseCommand_Executed,
             CloseCommand_CanExecute));

            this.Closing += MainWindow_Closing; // Alt+F4や×ボタンも統一
            _synth.SpeakCompleted += Synth_SpeakCompleted; // ← 追加
        
        }

        private static bool EnsureAnyVoiceSelected(SpeechSynthesizer synth)
        {
            var voices = synth.GetInstalledVoices()
                              .Where(v => v.Enabled)
                              .Select(v => v.VoiceInfo)
                              .ToList();
            if (voices.Count == 0) return false;

            // 文化優先リスト
            var current = CultureInfo.CurrentUICulture?.Name; // 例: "ja-JP"
            string[] preferredCultures = new[]
            {
                current,
                "ja-JP",
                "en-US"
            }.Where(c => !string.IsNullOrEmpty(c)).ToArray();

            // 優先文化にマッチする声を探す
            foreach (var culture in preferredCultures)
            {
                var match = voices.FirstOrDefault(v => v.Culture?.Name == culture);
                if (match != null)
                {
                    synth.SelectVoice(match.Name);
                    return true;
                }
            }

            // 文化一致が無ければ最初の有効な声を選ぶ
            synth.SelectVoice(voices.First().Name);
            return true;
        }

        private void OnSpeakButtonClick(object sender, RoutedEventArgs e)
        {
            if (_duringSpeak == false)
            {
                string text = "こちらは1秒ごとの仮想の計測値を左にリストアップし、右のグラフにてプロットするものとなっております。開始ボタンでサンプリング開始、停止ボタンでサンプリング停止となります。";
                string text1 = "左のリストには計測した日時、計測値、装置名がリストアップされます。";
                string text2 = "右のグラフの横軸は時間、縦軸は計測値で最近の１００秒間の値を表示します。グラフの赤色は閾値、青色は計測値となります。";

                // 非同期で読み上げ
                // _synth.SpeakAsync(fullText);
                ExplanationAPP(text,text1,text2);
            }
            
        }

        private void CloseCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true; // 条件があればここで制御
        }

        private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // 明示的な「閉じる」操作時
            if (ConfirmToExit())
            {
                _confirmedExit = true;
                this.Close();
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Alt+F4、タイトルバー×、タスクバーからの終了等もここで確認
            if (_confirmedExit) return;

            if (!ConfirmToExit())
            {
                e.Cancel = true; // キャンセル
            }
            else
            {
                _confirmedExit = true; // 次回以降は素通し
            }
        }

        private bool ConfirmToExit()
        {
            var dlg = new ConfirmExitDialog
            {
                Owner = this,
                Message = "アプリケーションを終了します。終了してよろしいですか？"
            };
            return dlg.ShowDialog() == true;
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_duringSpeak == false)
            {
                var win = App.HostApp!.Services.GetRequiredService<SettingsViewModel>();
                new SettingsView { DataContext = win }.ShowDialog();
            }
        }


        private void OpenLogs_Click(object sender, RoutedEventArgs e)
        {
            if (_duringSpeak == false)
            {
                var vm = App.HostApp!.Services.GetRequiredService<LogsViewModel>();
                new LogsView { DataContext = vm }.ShowDialog();
            }
        }

        private void OpenMeasureLogs_Click(object sender, RoutedEventArgs e)
        {
            if (_duringSpeak == false)
            {
                var vm = App.HostApp!.Services.GetRequiredService<TelemetryViewModel>();
                new TelemetryView { DataContext = vm }.ShowDialog();
            }
        }

        private void ExplanationAPP(string text1, string text2, string text3)
        {
            OverlayPanel.Visibility = Visibility.Visible;
            Explanation.IsEnabled = false;
            _duringSpeak = true;
            _pendingPrompts = 3;
            _synth.SpeakAsync(text1);
            _synth.SpeakAsync(text2);
            _synth.SpeakAsync(text3);
        }

        private void Synth_SpeakCompleted(object? sender, SpeakCompletedEventArgs e)
        {
            if (Interlocked.Decrement(ref _pendingPrompts) <= 0)
            {
                Dispatcher.Invoke(() =>
                {
                    OverlayPanel.Visibility = Visibility.Collapsed;
                    _duringSpeak = false;
                    Explanation.IsEnabled = true;
                });
            }
        }


        // 終了時にリソース解放
        protected override void OnClosed(System.EventArgs e)
        {
            try
            {
                _messenger.UnregisterAll(this); // ← 追加：登録解除
                _synth.SpeakAsyncCancelAll();
                _synth.Dispose();
            }
            finally
            {
                base.OnClosed(e);
            }
        }

    }
}