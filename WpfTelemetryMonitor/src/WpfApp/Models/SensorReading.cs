using System;


namespace WpfApp.Models
{
    public class SensorReading
    {
        public DateTime Timestamp { get; set; }//時間
        public string DeviceId { get; set; } = "dev-001";//デバイス番号（機種・シリアル等が該当）
        public double Value { get; set; }//値（測定値に相当）
    }
}
