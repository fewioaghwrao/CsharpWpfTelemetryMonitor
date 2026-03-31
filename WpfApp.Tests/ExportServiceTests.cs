using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WpfApp.Services;
using Xunit;

namespace WpfApp.Tests;

public class ExportServiceTests : IDisposable
{
    private readonly string _tempDir;

    public ExportServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "WpfAppTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task ExportTelemetryToCsvAsync_Throws_WhenDbFileDoesNotExist()
    {
        // Arrange
        var service = new ExportService();
        string dbPath = Path.Combine(_tempDir, "notfound.db");
        string csvPath = Path.Combine(_tempDir, "out.csv");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            service.ExportTelemetryToCsvAsync(dbPath, csvPath));
    }

    [Fact]
    public async Task ExportTelemetryToCsvAsync_ExportsCsv_FromReadingsTable()
    {
        // Arrange
        string dbPath = Path.Combine(_tempDir, "telemetry.db");
        string csvPath = Path.Combine(_tempDir, "telemetry.csv");
        await CreateSampleReadingsDbAsync(dbPath);

        var service = new ExportService();

        // Act
        await service.ExportTelemetryToCsvAsync(dbPath, csvPath);

        // Assert
        Assert.True(File.Exists(csvPath));

        byte[] bytes = await File.ReadAllBytesAsync(csvPath);
        Assert.True(bytes.Length >= 3);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);

        string text = await File.ReadAllTextAsync(csvPath, Encoding.UTF8);
        string[] lines = text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // readings テーブルの列は id, ts, device_id, value の順
        Assert.Equal("id,ts,device_id,value", lines[0]);
        Assert.Contains("1,2026-03-31T12:00:00.0000000,dev-001,88.5", lines[1]);
        Assert.Contains("2,2026-03-31T12:00:01.0000000,dev-002,42.25", lines[2]);
    }
    [Fact]
    public async Task ExportTelemetryToCsvAsync_Escapes_Comma_NewLine_And_Quotes()
    {
        // Arrange
        string dbPath = Path.Combine(_tempDir, "escape_only.db");
        string csvPath = Path.Combine(_tempDir, "escape_only.csv");
        await CreateEscapingOnlyDbAsync(dbPath);

        var service = new ExportService();

        // Act
        await service.ExportTelemetryToCsvAsync(dbPath, csvPath);

        // Assert
        string text = await File.ReadAllTextAsync(csvPath, Encoding.UTF8);
        string normalized = text.Replace("\r\n", "\n");

        Assert.Contains("timestamp,value,deviceid", normalized);
        Assert.Contains("\"dev,001\"", normalized);
        Assert.Contains("\"line1\nline2\"", normalized);
        Assert.Contains("\"say \"\"hello\"\"\"", normalized);
    }

    private static async Task CreateEscapingOnlyDbAsync(string dbPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        await using var conn = new SqliteConnection($"Data Source={dbPath}");
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE Readings(
    timestamp TEXT NOT NULL,
    value TEXT NOT NULL,
    deviceid TEXT NOT NULL
);

INSERT INTO Readings(timestamp, value, deviceid)
VALUES
('2026-03-31 12:00:00', '123', 'dev,001'),
('2026-03-31 12:00:01', 'line1
line2', 'dev-002'),
('2026-03-31 12:00:02', 'say ""hello""', 'dev-003');
";
        await cmd.ExecuteNonQueryAsync();
    }
    private static async Task CreateSampleReadingsDbAsync(string dbPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        await using var conn = new SqliteConnection($"Data Source={dbPath}");
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
                          CREATE TABLE readings(
                              id INTEGER PRIMARY KEY AUTOINCREMENT,
                              ts TEXT NOT NULL,
                              device_id TEXT NOT NULL,
                              value REAL NOT NULL
                          );

                          INSERT INTO readings(ts, device_id, value)
                          VALUES
                          ('2026-03-31T12:00:00.0000000', 'dev-001', 88.5),
                          ('2026-03-31T12:00:01.0000000', 'dev-002', 42.25);
                          """;
        await cmd.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
        }
    }
}