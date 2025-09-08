// Services/IDialogService.cs
using System.Threading.Tasks;

namespace WpfApp.Services
{
    public record SaveFilePickerOptions(string Title, string DefaultFileName, string DefaultExt, string Filter);

    public interface IDialogService
    {
        Task<string?> ShowSaveFilePickerAsync(SaveFilePickerOptions options);
        void ShowInfo(string message, string title = "情報");
        void ShowError(string message, string title = "エラー");
    }
}