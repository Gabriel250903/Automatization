using Automatization.Secrets;
using System.Windows;
using Wpf.Ui.Controls;
using static Automatization.Types.PasswordDialogResultType;

namespace Automatization.UI
{
    public partial class AdminPasswordDialog : FluentWindow
    {
        public PasswordDialogResult Result { get; private set; }

        public string? Password { get; private set; }
        private readonly string? _adminPassword;

        public AdminPasswordDialog()
        {
            InitializeComponent();
            _adminPassword = AdminSecret.GetAdminPassword();

            Loaded += (s, e) => PasswordInput.Focus();

            Result = PasswordDialogResult.Cancelled;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string enteredPassword = PasswordInput.Password;

            if (_adminPassword != null && enteredPassword == _adminPassword)
            {
                Password = enteredPassword;
                Result = PasswordDialogResult.Correct;
                DialogResult = true;
                Close();
            }
            else
            {
                ErrorMessage.Text = "Incorrect password.";
                ErrorMessage.Visibility = Visibility.Visible;
                Result = PasswordDialogResult.Incorrect;

                PasswordInput.Clear();
                _ = PasswordInput.Focus();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Password = null;
            Result = PasswordDialogResult.Cancelled;
            DialogResult = false;
            Close();
        }
    }
}