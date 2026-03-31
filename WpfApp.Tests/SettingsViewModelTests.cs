using CommunityToolkit.Mvvm.Messaging;
using System;
using System.IO;
using System.Text.Json;
using WpfApp.Models;
using WpfApp.ViewModels;
using Xunit;

namespace WpfApp.Tests;

public class SettingsViewModelTests : IDisposable
{
    private readonly string _configDir;
    private readonly string _configPath;
    private readonly string _backupPath;

    public SettingsViewModelTests()
    {
        _configDir = Path.Combine(AppContext.BaseDirectory, "Config");
        _configPath = Path.Combine(_configDir, "appsettings.json");
        _backupPath = Path.Combine(_configDir, "appsettings.json.testbackup");

        Directory.CreateDirectory(_configDir);

        if (File.Exists(_configPath))
        {
            File.Copy(_configPath, _backupPath, overwrite: true);
        }
    }

    [Fact]
    public void Save_Writes_AppSettingsJson_And_SendsMessages()
    {
        // Arrange
        var options = new TestOptionsMonitor<AppSettings>(new AppSettings
        {
            AlertThreshold = 80.0,
            SampleIntervalMs = 1000,
            DatabasePath = "telemetry.db",
            MaxInMemory = 2000,
            IsVoiceEnabled = true,
            VoiceName = "",
            VoiceRate = 0,
            VoiceVolume = 100
        });

        var messenger = new WeakReferenceMessenger();

        bool alertReceived = false;
        double alertValue = 0;

        bool voiceReceived = false;
        VoiceSettingsDto? voiceValue = null;

        bool closeReceived = false;

        var alertToken = new object();
        var voiceToken = new object();
        var closeToken = new object();

        messenger.Register<object, AlertThresholdChangedMessage>(
            alertToken,
            static (r, m) =>
            {
                var state = (AlertState)r;
                state.Received = true;
                state.Value = m.Value;
            });

        messenger.Register<object, VoiceSettingsChangedMessage>(
            voiceToken,
            static (r, m) =>
            {
                var state = (VoiceState)r;
                state.Received = true;
                state.Value = m.Value;
            });

        messenger.Register<object, CloseSettingsViewMessage>(
            closeToken,
            static (r, m) =>
            {
                var state = (CloseState)r;
                state.Received = true;
            });

        var alertState = new AlertState();
        var voiceState = new VoiceState();
        var closeState = new CloseState();

        // Register の recipient に state を使う
        messenger.UnregisterAll(alertToken);
        messenger.UnregisterAll(voiceToken);
        messenger.UnregisterAll(closeToken);

        messenger.Register<AlertState, AlertThresholdChangedMessage>(
            alertState,
            static (r, m) =>
            {
                r.Received = true;
                r.Value = m.Value;
            });

        messenger.Register<VoiceState, VoiceSettingsChangedMessage>(
            voiceState,
            static (r, m) =>
            {
                r.Received = true;
                r.Value = m.Value;
            });

        messenger.Register<CloseState, CloseSettingsViewMessage>(
            closeState,
            static (r, m) =>
            {
                r.Received = true;
            });

        var vm = new SettingsViewModel(options, messenger);

        vm.AlertThreshold = 95.5;
        vm.SampleIntervalMs = 1500;
        vm.IsVoiceEnabled = false;
        vm.VoiceRate = 99;
        vm.VoiceVolume = -5;

        if (vm.InstalledVoices.Count > 0)
        {
            vm.SelectedVoice = vm.InstalledVoices[0];
        }
        else
        {
            vm.SelectedVoice = "";
        }

        // Act
        vm.SaveCommand.Execute(null);

        // Assert: ファイル保存
        Assert.True(File.Exists(_configPath));

        string json = File.ReadAllText(_configPath);
        using var doc = JsonDocument.Parse(json);

        var appSettings = doc.RootElement.GetProperty("AppSettings");

        Assert.Equal(95.5, appSettings.GetProperty("AlertThreshold").GetDouble());
        Assert.Equal(1500, appSettings.GetProperty("SampleIntervalMs").GetInt32());
        Assert.Equal("telemetry.db", appSettings.GetProperty("DatabasePath").GetString());
        Assert.Equal(2000, appSettings.GetProperty("MaxInMemory").GetInt32());

        Assert.False(appSettings.GetProperty("IsVoiceEnabled").GetBoolean());
        Assert.Equal(vm.SelectedVoice ?? "", appSettings.GetProperty("VoiceName").GetString());
        Assert.Equal(10, appSettings.GetProperty("VoiceRate").GetInt32());
        Assert.Equal(0, appSettings.GetProperty("VoiceVolume").GetInt32());

        // Assert: メッセージ送信
        alertReceived = alertState.Received;
        alertValue = alertState.Value;

        voiceReceived = voiceState.Received;
        voiceValue = voiceState.Value;

        closeReceived = closeState.Received;

        Assert.True(alertReceived);
        Assert.Equal(95.5, alertValue);

        Assert.True(voiceReceived);
        Assert.NotNull(voiceValue);
        Assert.False(voiceValue!.IsEnabled);
        Assert.Equal(vm.SelectedVoice ?? "", voiceValue.VoiceName);
        Assert.Equal(10, voiceValue.Rate);
        Assert.Equal(0, voiceValue.Volume);

        Assert.True(closeReceived);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_backupPath))
            {
                File.Copy(_backupPath, _configPath, overwrite: true);
                File.Delete(_backupPath);
            }
            else if (File.Exists(_configPath))
            {
                File.Delete(_configPath);
            }
        }
        catch
        {
        }
    }

    private sealed class AlertState
    {
        public bool Received { get; set; }
        public double Value { get; set; }
    }

    private sealed class VoiceState
    {
        public bool Received { get; set; }
        public VoiceSettingsDto? Value { get; set; }
    }

    private sealed class CloseState
    {
        public bool Received { get; set; }
    }
}