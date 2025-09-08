using Microsoft.Extensions.Options;
using System;
using System.Timers;
using WpfApp.Models;


namespace WpfApp.Services
{
    public class TelemetrySimulator : IDisposable
    {
        private readonly System.Timers.Timer _timer;
        private readonly Random _rnd = new();
        public event Action<SensorReading>? OnReading;


        public TelemetrySimulator(IOptions<AppSettings> options)
        {
            _timer = new System.Timers.Timer(options.Value.SampleIntervalMs);
            _timer.Elapsed += (_, __) => Emit();
        }


        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();


        private void Emit()
        {
            var reading = new SensorReading
            {
                Timestamp = DateTime.Now,
                Value = 50 + 40 * Math.Sin(DateTime.Now.Second / 60.0 * 2 * Math.PI) + _rnd.NextDouble() * 5,
                DeviceId = "dev-001"
            };
            OnReading?.Invoke(reading);
        }


        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
