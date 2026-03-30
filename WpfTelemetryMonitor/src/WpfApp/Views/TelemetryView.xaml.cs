using System;
using System.Windows;
using WpfApp.ViewModels;

namespace WpfApp.Views
{
    public partial class TelemetryView : Window
    {
        public TelemetryView()
        {
            InitializeComponent();
            DataContext = new TelemetryViewModel();
        }

        private async void Cleardb_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new PasswordDialog { Owner = this };
            if (dlg.ShowDialog() != true) return;

            string expected = DateTime.Now.ToString("ddHH");
            if (!string.Equals(dlg.EnteredPassword, expected, StringComparison.Ordinal))
            {
                MessageBox.Show(this, "パスワードが違います。", "認証エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (DataContext is not TelemetryViewModel vm)
            {
                MessageBox.Show(this, "ViewModel が未設定です。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                await vm.ClearDatabaseAsync();
                vm.ReloadFromDatabese(showAll: false);

                MessageBox.Show(this, "計測ログをバックアップしてクリアしました。", "完了",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"処理中にエラーが発生しました。\n{ex.Message}", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}