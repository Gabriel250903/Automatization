using Automatization.Services;
using Automatization.Types;
using Automatization.ViewModels;
using System.Windows;
using Wpf.Ui.Controls;

namespace Automatization.UI
{
    public partial class ThemeCreatorWindow : FluentWindow
    {
        public ThemeCreatorWindow()
        {
            InitializeComponent();
        }

        private CustomTheme BuildTheme()
        {
            CustomTheme theme = new()
            {
                Name = string.IsNullOrWhiteSpace(ThemeNameBox.Text) ? "Custom Theme" : ThemeNameBox.Text,
                WindowBackgroundColor = WindowBgHex.Text,
                WindowGradientEndColor = GradientEndHex.Text,
                TextColor = TextColorHex.Text,
                ButtonBackgroundColor = BtnBgHex.Text,
                ButtonHoverColor = BtnHoverHex.Text,
                AccentColor = AccentHex.Text,
                BackgroundImagePath = ImagePathBox.Text,
                BackgroundMode = RadioImage.IsChecked == true
                    ? BackgroundType.Image
                    : RadioGradient.IsChecked == true ? BackgroundType.Gradient : BackgroundType.Solid
            };

            return theme;
        }

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CustomTheme theme = BuildTheme();

                ThemeService.ApplyTheme(theme, Resources);

                WindowBackdropType = WindowBackdropType.None;

                if (Resources["ApplicationBackgroundBrush"] is System.Windows.Media.Brush bgBrush)
                {
                    Background = bgBrush;
                }
            }
            catch
            {
                Wpf.Ui.Controls.MessageBox uiMessageBox = new()
                {
                    Title = "Error",
                    Content = "Invalid Color Code. Check your hex values.",
                    CloseButtonText = "OK"
                };
                _ = uiMessageBox.ShowDialogAsync();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ThemeNameBox.Text))
            {
                Wpf.Ui.Controls.MessageBox uiMessageBox = new()
                {
                    Title = "Missing Name",
                    Content = "Please enter a theme name.",
                    CloseButtonText = "OK"
                };

                _ = uiMessageBox.ShowDialogAsync();
                return;
            }

            try
            {
                ThemeService.SaveTheme(BuildTheme());
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Wpf.Ui.Controls.MessageBox uiMessageBox = new()
                {
                    Title = "Save Error",
                    Content = ex.Message,
                    CloseButtonText = "OK"
                };
                _ = uiMessageBox.ShowDialogAsync();
            }
        }

        private void PickColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is System.Windows.Controls.TextBox targetBox)
            {
                using ColorDialog colorDialog = new();

                try
                {
                    System.Windows.Media.Color wpfColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(targetBox.Text);

                    colorDialog.Color = System.Drawing.Color.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B);
                }
                catch
                {
                }

                if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    System.Drawing.Color c = colorDialog.Color;
                    targetBox.Text = $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

                    targetBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
                }
            }
        }

        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new()
            {
                Filter = "Images (*.jpg;*.png;*.bmp)|*.jpg;*.png;*.bmp|All files (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                ImagePathBox.Text = dlg.FileName;
            }
        }

        private void BgMode_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized)
            {
                return;
            }

            if (RadioSolid.IsChecked == true)
            {
                GradientEndContainer.Visibility = Visibility.Collapsed;
                ImageContainer.Visibility = Visibility.Collapsed;
                PrimaryColorLabel.Text = "Background Color";
            }
            else if (RadioGradient.IsChecked == true)
            {
                GradientEndContainer.Visibility = Visibility.Visible;
                ImageContainer.Visibility = Visibility.Collapsed;
                PrimaryColorLabel.Text = "Start Color";
            }
            else if (RadioImage.IsChecked == true)
            {
                GradientEndContainer.Visibility = Visibility.Collapsed;
                ImageContainer.Visibility = Visibility.Visible;
                PrimaryColorLabel.Text = "Fallback Color";
            }
        }
    }
}