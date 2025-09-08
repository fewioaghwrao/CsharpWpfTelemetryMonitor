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
            // パスワードダイアログ
            var dlg = new PasswordDialog { Owner = this };
            if (dlg.ShowDialog() != true) return;

            // 期待パスワード（ddHH）※月日（MMdd）にしたい場合は "MMdd" に変えるだけ
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
                  await vm.ClearDatabaseAsync();         // バックアップ＆クリア
                   vm.ReloadFromDatabese(showAll: false); // 画面の一覧を空に（必要に応じて再読込）
                MessageBox.Show(this, "ログをバックアップしてクリアしました。", "完了",
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