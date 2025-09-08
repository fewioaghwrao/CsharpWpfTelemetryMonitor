// Services/DialogService.cs
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp.Services
{
    public class DialogService : IDialogService
    {
        public Task<string?> ShowSaveFilePickerAsync(SaveFilePickerOptions options)
        {
            var sfd = new SaveFileDialog
            {
                Title = options.Title,
                FileName = options.DefaultFileName,
                DefaultExt = options.DefaultExt,
                Filter = options.Filter,
                AddExtension = true,
                OverwritePrompt = true,
                CheckPathExists = true
            };
            string? path = sfd.ShowDialog(Application.Current.MainWindow) == true ? sfd.FileName : null;
            return Task.FromResult(path);
        }

        public void ShowInfo(string message, string title = "情報")
            => MessageBox.Show(Application.Current.MainWindow!, message, title, MessageBoxButton.OK, MessageBoxImage.Information);

        public void ShowError(string message, string title = "エラー")
            => MessageBox.Show(Application.Current.MainWindow!, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

