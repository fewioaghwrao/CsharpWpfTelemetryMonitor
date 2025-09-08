using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp.Models
{
    public class AppSettings
    {
        public int SampleIntervalMs { get; set; } = 1000;
        public string DatabasePath { get; set; } = "telemetry.db";
        public int MaxInMemory { get; set; } = 2000;
        public double AlertThreshold { get; set; } = 80.0;
        public string DeviceId { get; set; } = "dev-001";

        // ▼ 音声関連（追加）
        public bool IsVoiceEnabled { get; set; } = true;  // ON/OFF
        public string VoiceName { get; set; } = "";        // 音声名
        public int VoiceRate { get; set; } = 0;          // -10 ～ 10
        public int VoiceVolume { get; set; } = 100;      // 0 ～ 100
    }
}
