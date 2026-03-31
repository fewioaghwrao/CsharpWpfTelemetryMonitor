using CommunityToolkit.Mvvm.Messaging;
using System;
using WpfApp.Models;
using WpfApp.Services;
using WpfApp.ViewModels;
using Xunit;

namespace WpfApp.Tests;

public class AlertServiceTests
{
    [Fact]
    public void Check_RaisesAlert_WhenValueIsGreaterThanOrEqualToInitialThreshold()
    {
        // Arrange
        var monitor = new TestOptionsMonitor<AppSettings>(new AppSettings
        {
            AlertThreshold = 80.0
        });

        IMessenger messenger = new WeakReferenceMessenger();
        using var service = new AlertService(monitor, messenger);

        string? actual = null;
        service.OnAlert += message => actual = message;

        var reading = new SensorReading
        {
            Timestamp = new DateTime(2026, 3, 31, 12, 0, 0),
            Value = 80.0,
            DeviceId = "dev-001"
        };

        // Act
        service.Check(reading);

        // Assert
        Assert.NotNull(actual);
        Assert.Contains("閾値超え", actual);
        Assert.Contains("80.0 >= 80.0", actual);
    }

    [Fact]
    public void Check_DoesNotRaiseAlert_WhenValueIsBelowThreshold()
    {
        // Arrange
        var monitor = new TestOptionsMonitor<AppSettings>(new AppSettings
        {
            AlertThreshold = 80.0
        });

        IMessenger messenger = new WeakReferenceMessenger();
        using var service = new AlertService(monitor, messenger);

        bool raised = false;
        service.OnAlert += _ => raised = true;

        var reading = new SensorReading
        {
            Timestamp = DateTime.Now,
            Value = 79.9,
            DeviceId = "dev-001"
        };

        // Act
        service.Check(reading);

        // Assert
        Assert.False(raised);
    }

    [Fact]
    public void Check_UsesUpdatedThreshold_AfterMessengerMessage()
    {
        // Arrange
        var monitor = new TestOptionsMonitor<AppSettings>(new AppSettings
        {
            AlertThreshold = 80.0
        });

        IMessenger messenger = new WeakReferenceMessenger();
        using var service = new AlertService(monitor, messenger);

        bool raised = false;
        service.OnAlert += _ => raised = true;

        messenger.Send(new AlertThresholdChangedMessage(90.0));

        var reading = new SensorReading
        {
            Timestamp = DateTime.Now,
            Value = 85.0,
            DeviceId = "dev-001"
        };

        // Act
        service.Check(reading);

        // Assert
        Assert.False(raised);
    }

    [Fact]
    public void Check_UsesUpdatedThreshold_AfterOptionsMonitorChange()
    {
        // Arrange
        var monitor = new TestOptionsMonitor<AppSettings>(new AppSettings
        {
            AlertThreshold = 80.0
        });

        IMessenger messenger = new WeakReferenceMessenger();
        using var service = new AlertService(monitor, messenger);

        bool raised = false;
        service.OnAlert += _ => raised = true;

        monitor.Set(new AppSettings
        {
            AlertThreshold = 60.0
        });

        var reading = new SensorReading
        {
            Timestamp = DateTime.Now,
            Value = 70.0,
            DeviceId = "dev-001"
        };

        // Act
        service.Check(reading);

        // Assert
        Assert.True(raised);
    }
}