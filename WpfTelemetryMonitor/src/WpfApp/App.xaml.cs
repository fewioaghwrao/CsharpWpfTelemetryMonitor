using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Hosting; // 追加
using System;
using System.IO;
using System.Windows;
using WpfApp.Bootstrapper;
using WpfApp.Models;
using WpfApp.Services;
using WpfApp.ViewModels;

namespace WpfTelemetryMonitor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost? HostApp { get; private set; }


        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);


            var builder = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile(Path.Combine("Config", "appsettings.json"), optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddAppServices(ctx.Configuration);
            })
            .UseSerilog((ctx, lc) =>
            {
                var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app-.log");
                lc.MinimumLevel.Information()
    .WriteTo.File(logPath, rollingInterval: RollingInterval.Day);
            });


            HostApp = builder.Build();
            HostApp.Start();

            // ★ DI から生成して表示
            var main = HostApp.Services.GetRequiredService<WpfApp.Views.MainWindow>();
            MainWindow = main;
            main.Show();
        }


        protected override async void OnExit(ExitEventArgs e)
        {
            await (HostApp?.StopAsync() ?? System.Threading.Tasks.Task.CompletedTask);
            HostApp?.Dispose();
            base.OnExit(e);
        }
    }

}
