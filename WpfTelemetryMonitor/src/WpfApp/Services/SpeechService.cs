// 例: WpfApp.Services.SpeechService.cs
using CommunityToolkit.Mvvm.Messaging;
using System.Speech.Synthesis;
using WpfApp.ViewModels;

namespace WpfApp.Services
{
    public class SpeechService : IDisposable
    {
        private readonly SpeechSynthesizer _synth = new();

        private bool _enabled = true;

        public SpeechService(IMessenger messenger)
        {
            // 初期設定は AppSettings 読み込み側で行ってもOK

            messenger.Register<VoiceSettingsChangedMessage>(this, (_, msg) =>
            {
                _enabled = msg.Value.IsEnabled;

                if (!string.IsNullOrWhiteSpace(msg.Value.VoiceName))
                {
                    try { _synth.SelectVoice(msg.Value.VoiceName); } catch { /* 無視 or ログ */ }
                }

                _synth.Rate = msg.Value.Rate;     // -10 ～ 10
                _synth.Volume = msg.Value.Volume; // 0 ～ 100
            });
        }

        public void SpeakAsync(string text)
        {
            if (!_enabled || string.IsNullOrWhiteSpace(text)) return;
            _synth.SpeakAsync(text);
        }

        public void Dispose() => _synth.Dispose();
    }
}
