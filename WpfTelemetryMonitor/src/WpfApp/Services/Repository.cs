using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;
using WpfApp.Models;


namespace WpfApp.Services
{
    public class Repository
    {
        private readonly string _dbPath;


        public Repository(IOptions<AppSettings> options)
        {
            _dbPath = Path.GetFullPath(options.Value.DatabasePath);
            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
            Initialize();
        }


        private void Initialize()
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"CREATE TABLE IF NOT EXISTS readings(
id INTEGER PRIMARY KEY AUTOINCREMENT,
ts TEXT NOT NULL,
device_id TEXT NOT NULL,
value REAL NOT NULL
);";
            cmd.ExecuteNonQuery();
        }


        public async Task InsertAsync(SensorReading r)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO readings(ts, device_id, value) VALUES ($ts, $dev, $val);";
            cmd.Parameters.AddWithValue("$ts", r.Timestamp.ToString("o"));
            cmd.Parameters.AddWithValue("$dev", r.DeviceId);
            cmd.Parameters.AddWithValue("$val", r.Value);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
