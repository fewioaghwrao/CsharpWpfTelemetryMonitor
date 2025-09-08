using CommunityToolkit.Mvvm.Messaging;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using WpfApp.ViewModels;


namespace WpfApp.Views
{
    public partial class SettingsView : Window
    {
      
        public SettingsView()
        {
            InitializeComponent();

            // 受信登録：この View に Close メッセージが来たら閉じる
            WeakReferenceMessenger.Default.Register<CloseSettingsViewMessage>(this, (r, m) =>
            {
                MessageBox.Show(this, "設定を保存しました。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            });

            // 念のため後片付け（WeakReferenceMessenger なら大体OKだが明示的に外すと安心）
            this.Closed += (_, __) =>
            {
                WeakReferenceMessenger.Default.Unregister<CloseSettingsViewMessage>(this);
            };
        }

        private static readonly Regex _regex = new Regex("^[0-9]+$");

        private void AlertThresholdTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 入力が1～9の数字のみか判定
            e.Handled = !_regex.IsMatch(e.Text);
        }
    }
}
