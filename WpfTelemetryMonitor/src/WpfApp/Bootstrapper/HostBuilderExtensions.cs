using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WpfApp.Models;
using WpfApp.Services;
using WpfApp.ViewModels;
using WpfApp.Views;
using WpfTelemetryMonitor;


namespace WpfApp.Bootstrapper
{
    public static class HostBuilderExtensions
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<AppSettings>(config.GetSection("AppSettings"));
            services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

            // Services
            services.AddSingleton<TelemetrySimulator>();
            services.AddSingleton<Repository>();
            services.AddSingleton<AlertService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IExportService, ExportService>();


            // ViewModels
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<LogsViewModel>();
            services.AddTransient<TelemetryViewModel>();


            // Views
            services.AddSingleton<MainWindow>();


            return services;
        }
    }
}
