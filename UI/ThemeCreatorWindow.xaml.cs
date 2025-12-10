using Automatization.Services;
using Automatization.Types;
using Automatization.ViewModels;
using System.Windows;
using Wpf.Ui.Controls;
using Button = System.Windows.Controls.Button;
using Color = System.Drawing.Color;
using Media = System.Windows.Media;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using TextBox = System.Windows.Controls.TextBox;

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

        private void Randomize_Click(object sender, RoutedEventArgs e)
        {
            Random rnd = new();

            bool isDarkTheme = rnd.Next(2) == 0;

            Color bgPrimary;
            Color text;
            Color btnBg;
            Color btnHover;

            if (isDarkTheme)
            {
                bgPrimary = Color.FromArgb(255, rnd.Next(10, 40), rnd.Next(10, 40), rnd.Next(10, 45));
                text = Color.FromArgb(255, 220, 220, 220);
                btnBg = Color.FromArgb(40, 255, 255, 255);
                btnHover = Color.FromArgb(60, 255, 255, 255);
            }
            else
            {
                bgPrimary = Color.FromArgb(255, rnd.Next(230, 256), rnd.Next(230, 256), rnd.Next(235, 256));
                text = Color.FromArgb(255, 20, 20, 20);
                btnBg = Color.FromArgb(30, 0, 0, 0);
                btnHover = Color.FromArgb(50, 0, 0, 0);
            }

            Color accent = Color.FromArgb(255, rnd.Next(50, 255), rnd.Next(50, 255), rnd.Next(50, 255));

            bool useGradient = rnd.Next(100) < 30;
            Color bgSecondary = bgPrimary;

            if (useGradient)
            {
                RadioGradient.IsChecked = true;

                int shift = 30;
                int r = Math.Clamp(bgPrimary.R + rnd.Next(-shift, shift), 0, 255);
                int g = Math.Clamp(bgPrimary.G + rnd.Next(-shift, shift), 0, 255);
                int b = Math.Clamp(bgPrimary.B + rnd.Next(-shift, shift), 0, 255);

                bgSecondary = Color.FromArgb(255, r, g, b);
            }
            else
            {
                RadioSolid.IsChecked = true;
            }

            WindowBgHex.Text = ColorToHex(bgPrimary);
            GradientEndHex.Text = ColorToHex(bgSecondary);
            TextColorHex.Text = ColorToHex(text);
            BtnBgHex.Text = ColorToHex(btnBg);
            BtnHoverHex.Text = ColorToHex(btnHover);
            AccentHex.Text = ColorToHex(accent);

            Preview_Click(sender, e);
        }

        private static string ColorToHex(Color c)
        {
            return $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
        }

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CustomTheme theme = BuildTheme();

                ThemeService.ApplyTheme(theme, Resources);

                WindowBackdropType = WindowBackdropType.None;

                if (Resources["ApplicationBackgroundBrush"] is Media.Brush bgBrush)
                {
                    Background = bgBrush;
                }
            }
            catch
            {
                MessageBox uiMessageBox = new()
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
                MessageBox uiMessageBox = new()
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
                MessageBox uiMessageBox = new()
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
            if (sender is Button btn && btn.Tag is TextBox targetBox)
            {
                using ColorDialog colorDialog = new();

                try
                {
                    Media.Color wpfColor = (Media.Color)Media.ColorConverter.ConvertFromString(targetBox.Text);

                    colorDialog.Color = Color.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B);
                }
                catch
                {
                }

                if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Color c = colorDialog.Color;
                    targetBox.Text = $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

                    targetBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                }
            }
        }

        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new()
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