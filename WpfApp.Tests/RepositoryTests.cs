using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;
using WpfApp.Models;
using WpfApp.Services;
using Xunit;

namespace WpfApp.Tests;

public class RepositoryTests : IDisposable
{
    private readonly string _tempDir;

    public RepositoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "WpfAppTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task Constructor_CreatesReadingsTable_AndInsertAsync_InsertsOneRow()
    {
        // Arrange
        string dbPath = Path.Combine(_tempDir, "telemetry.db");
        var settings = Options.Create(new AppSettings
        {
            DatabasePath = dbPath
        });

        var repository = new Repository(settings);

        var reading = new SensorReading
        {
            Timestamp = new DateTime(2026, 3, 31, 12, 0, 0),
            DeviceId = "dev-001",
            Value = 88.5
        };

        // Act
        await repository.InsertAsync(reading);

        // Assert
        await using var conn = new SqliteConnection($"Data Source={Path.GetFullPath(dbPath)}");
        await conn.OpenAsync();

        await using var countCmd = conn.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM readings;";
        var count = (long)(await countCmd.ExecuteScalarAsync())!;
        Assert.Equal(1, count);

        await using var rowCmd = conn.CreateCommand();
        rowCmd.CommandText = "SELECT ts, device_id, value FROM readings LIMIT 1;";
        await using var reader = await rowCmd.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal(reading.Timestamp.ToString("o"), reader.GetString(0));
        Assert.Equal(reading.DeviceId, reader.GetString(1));
        Assert.Equal(reading.Value, reader.GetDouble(2), 3);
    }

    [Fact]
    public async Task Constructor_CreatesDatabaseFile_AndReadingsTable()
    {
        // Arrange
        string dbPath = Path.Combine(_tempDir, "telemetry2.db");
        var settings = Options.Create(new AppSettings
        {
            DatabasePath = dbPath
        });

        // Act
        var repository = new Repository(settings);

        // Assert
        Assert.True(File.Exists(Path.GetFullPath(dbPath)));

        await using var conn = new SqliteConnection($"Data Source={Path.GetFullPath(dbPath)}");
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
                          SELECT COUNT(*)
                          FROM sqlite_master
                          WHERE type='table' AND name='readings';
                          """;

        var count = (long)(await cmd.ExecuteScalarAsync())!;
        Assert.Equal(1, count);
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
            // テスト後の掃除失敗は握りつぶす
        }
    }
}