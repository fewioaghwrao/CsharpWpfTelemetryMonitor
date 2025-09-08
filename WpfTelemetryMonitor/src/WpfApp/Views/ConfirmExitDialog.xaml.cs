using System.Windows;

namespace WpfApp.Views
{
    public partial class ConfirmExitDialog : Window
    {
        public string Message { get; set; } = "アプリケーションを終了します。よろしいですか？";

        public ConfirmExitDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            DialogResult = true; // 閉じる許可
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // キャンセル
        }
    }
}

