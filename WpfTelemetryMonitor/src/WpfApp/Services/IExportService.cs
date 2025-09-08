// Services/IExportService.cs
using System.Threading;
using System.Threading.Tasks;

namespace WpfApp.Services
{
    public interface IExportService
    {
        Task ExportTelemetryToCsvAsync(string dbPath, string csvPath, CancellationToken ct = default);
    }
}
