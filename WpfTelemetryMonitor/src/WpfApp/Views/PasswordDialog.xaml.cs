using System.Windows;

namespace WpfApp.Views
{
    public partial class PasswordDialog : Window
    {
        public string EnteredPassword { get; private set; } = string.Empty;

        public PasswordDialog()
        {
            InitializeComponent();
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            EnteredPassword = Pwd.Password ?? string.Empty;
            DialogResult = true;
        }
    }
}

