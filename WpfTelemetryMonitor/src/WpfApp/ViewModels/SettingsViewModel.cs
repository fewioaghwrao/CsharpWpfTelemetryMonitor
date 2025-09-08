using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Speech.Synthesis; // ← 追加：音声一覧取得に使用
using System.Text.Json;
using WpfApp.Models;
using System;
using System.Windows;


namespace WpfApp.ViewModels
{
    public class CloseSettingsViewMessage { }
    // 音声設定まとめて渡したい場合のメッセージ
    public class VoiceSettingsChangedMessage : ValueChangedMessage<VoiceSettingsDto>
    {
        public VoiceSettingsChangedMessage(VoiceSettingsDto value) : base(value) { }
    }

    public record VoiceSettingsDto(
        bool IsEnabled,
        string VoiceName,
        int Rate,
        int Volume
    );

    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IOptionsMonitor<AppSettings> _optMonitor;
        private readonly IMessenger _messenger;
        // ▼ 追加：テスト再生用の音声合成インスタンス（画面が開いている間だけ使う）
        private readonly SpeechSynthesizer _previewSynth = new();

        [ObservableProperty] private double alertThreshold;
        [ObservableProperty] private int sampleIntervalMs;

        // ▼ 追加：音声関連
        public ObservableCollection<string> InstalledVoices { get; } = new();
        [ObservableProperty] private bool isVoiceEnabled;
        [ObservableProperty] private string? selectedVoice;
        [ObservableProperty] private int voiceRate;    // -10 ～ 10
        [ObservableProperty] private int voiceVolume;  // 0 ～ 100

        public SettingsViewModel(IOptionsMonitor<AppSettings> optMonitor, IMessenger messenger)
        {
            _optMonitor = optMonitor;
            _messenger = messenger;

            var s = _optMonitor.CurrentValue;
            alertThreshold = s.AlertThreshold;
            sampleIntervalMs = s.SampleIntervalMs;

            // ▼ 音声の現在値を反映
            isVoiceEnabled = s.IsVoiceEnabled;
            voiceRate = Clamp(s.VoiceRate, -10, 10);
            voiceVolume = Clamp(s.VoiceVolume, 0, 100);

            // ▼ インストール音声を列挙
            using (var synth = new SpeechSynthesizer())
            {
                var names = synth.GetInstalledVoices()
                                 .Where(v => v.Enabled)
                                 .Select(v => v.VoiceInfo.Name)
                                 .Distinct()
                                 .OrderBy(n => n);

                foreach (var n in names)
                    InstalledVoices.Add(n);
            }

            // 既定選択（保存値が無ければ最初の音声）
            selectedVoice = !string.IsNullOrWhiteSpace(s.VoiceName) && InstalledVoices.Contains(s.VoiceName)
                          ? s.VoiceName
                          : InstalledVoices.FirstOrDefault();

        }
        // SettingsViewModel クラスの中に追加
        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        // （必要なら double 用も）
        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        // ▼ 追加：「テスト読み上げ」コマンド
        [RelayCommand]
        private void TestSpeak()
        {
            if (!isVoiceEnabled) return;

            try
            {
                // 直前の再生を止める
                _previewSynth.SpeakAsyncCancelAll();

                // 選択中の音声・レート・音量を反映
                if (!string.IsNullOrWhiteSpace(selectedVoice))
                {
                    try { _previewSynth.SelectVoice(selectedVoice); } catch { /* 音声が無い場合は既定を使う */ }
                }
                _previewSynth.Rate = Clamp(voiceRate, -10, 10);
                _previewSynth.Volume = Clamp(voiceVolume, 0, 100);

                // 短いサンプルを非同期で再生（UIはブロックしない）
                _previewSynth.SpeakAsync("テスト読み上げです。現在の設定が反映されています。");
            }
            catch(Exception ex)
            {
                MessageBox.Show($"音声の再生に失敗しました。\n{ex.Message}", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                // 必要ならログ等
            }
        }

        [RelayCommand]
        private void Save()
        {
            // 入力のクランプ（フィールドをクランプ）
            voiceRate = Clamp(voiceRate, -10, 10);
            voiceVolume = Clamp(voiceVolume, 0, 100);
            // 1. 新しい設定を作成
            var newSettings = new AppSettings
            {
                AlertThreshold = AlertThreshold,
                SampleIntervalMs = SampleIntervalMs,
                DatabasePath = _optMonitor.CurrentValue.DatabasePath,
                MaxInMemory = _optMonitor.CurrentValue.MaxInMemory,

                // ▼ 音声関連（保存）: 右辺を“フィールド”に
                IsVoiceEnabled = isVoiceEnabled,
                VoiceName = selectedVoice ?? "",
                VoiceRate = voiceRate,
                VoiceVolume = voiceVolume
            };

            // 2. appsettings.json のパス
            var configPath = Path.Combine(AppContext.BaseDirectory, "Config", "appsettings.json");

            // 3. JSONとして保存
            var json = JsonSerializer.Serialize(new { AppSettings = newSettings }, 
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);

            // 4. Messengerで全体に通知
            _messenger.Send(new AlertThresholdChangedMessage(AlertThreshold));
            _messenger.Send(new VoiceSettingsChangedMessage(
      new VoiceSettingsDto(
            isVoiceEnabled,
            selectedVoice ?? "",
            voiceRate,
            voiceVolume
      )));


            // 5. 「閉じろ」という通知を送る
            _messenger.Send(new CloseSettingsViewMessage());

        }
    }

    // メッセージ定義
    public class AlertThresholdChangedMessage : ValueChangedMessage<double>
    {
        public AlertThresholdChangedMessage(double value) : base(value) { }
    }
}