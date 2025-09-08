using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Options;
using WpfApp.Models;
using WpfApp.ViewModels;

using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Options;
using System;
using System.Threading; // Volatile
using WpfApp.Models;
using WpfApp.ViewModels; // AlertThresholdChangedMessage

namespace WpfApp.Services
{
    public sealed class AlertService :
        IRecipient<AlertThresholdChangedMessage>, IDisposable
    {
        private readonly IMessenger _messenger;
        private readonly IOptionsMonitor<AppSettings> _opt;

        // 監視用に最新の閾値をキャッシュ（スレッドセーフに更新）
        private double _alertThreshold;

        public event Action<string>? OnAlert;

        public AlertService(IOptionsMonitor<AppSettings> opt, IMessenger messenger)
        {
            _opt = opt;
            _messenger = messenger;

            // 初期値
            Volatile.Write(ref _alertThreshold, _opt.CurrentValue.AlertThreshold);

            // appsettings.json 変更（reloadOnChange:true）にも追従
            _opt.OnChange(s =>
            {
                Volatile.Write(ref _alertThreshold, s.AlertThreshold);
            });

            // 設定画面の「OK」直後の即時反映（Messenger）
            _messenger.Register(this); // AlertThresholdChangedMessage を受信
        }

        // SettingsViewModel からの即時通知を受け取る
        public void Receive(AlertThresholdChangedMessage message)
        {
            Volatile.Write(ref _alertThreshold, message.Value);
        }

        // センサ値チェック
        public void Check(SensorReading r)
        {
            var th = Volatile.Read(ref _alertThreshold);
            if (r.Value >= th)
            {
                OnAlert?.Invoke(
                    $"閾値超え: {r.Value:F1} >= {th:F1} 時刻: {r.Timestamp:HH:mm:ss}");
            }
        }

        public void Dispose()
        {
            _messenger.Unregister<AlertThresholdChangedMessage>(this);
        }
    }
}

