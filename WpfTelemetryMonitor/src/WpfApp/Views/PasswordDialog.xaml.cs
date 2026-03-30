using System.Windows;
using System.Windows.Controls;

namespace WpfApp.Views
{
    public partial class PasswordDialog : Window
    {
        private bool _isSyncing;

        public string EnteredPassword { get; private set; } = string.Empty;

        public PasswordDialog()
        {
            InitializeComponent();
            Loaded += (_, _) => Pwd.Focus();
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            EnteredPassword = IsPasswordVisible()
                ? (PwdVisible.Text ?? string.Empty)
                : (Pwd.Password ?? string.Empty);

            DialogResult = true;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void TogglePasswordButton_Checked(object sender, RoutedEventArgs e)
        {
            PwdVisible.Text = Pwd.Password;
            Pwd.Visibility = Visibility.Collapsed;
            PwdVisible.Visibility = Visibility.Visible;
            TogglePasswordText.Text = "隠す";
            PwdVisible.Focus();
            PwdVisible.CaretIndex = PwdVisible.Text.Length;
        }

        private void TogglePasswordButton_Unchecked(object sender, RoutedEventArgs e)
        {
            Pwd.Password = PwdVisible.Text;
            PwdVisible.Visibility = Visibility.Collapsed;
            Pwd.Visibility = Visibility.Visible;
            TogglePasswordText.Text = "表示";
            Pwd.Focus();
        }

        private void Pwd_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncing) return;

            try
            {
                _isSyncing = true;
                if (PwdVisible.Visibility == Visibility.Visible)
                {
                    PwdVisible.Text = Pwd.Password;
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void PwdVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncing) return;

            try
            {
                _isSyncing = true;
                if (Pwd.Visibility == Visibility.Visible || PwdVisible.Visibility == Visibility.Visible)
                {
                    Pwd.Password = PwdVisible.Text;
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private bool IsPasswordVisible()
        {
            return PwdVisible.Visibility == Visibility.Visible;
        }
    }
}

